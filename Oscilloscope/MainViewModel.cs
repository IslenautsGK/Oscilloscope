using System.Buffers;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Pipelines;
using System.IO.Ports;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace Oscilloscope;

internal sealed partial class MainViewModel : ObservableObject
{
    private static readonly string[] Colors =
    [
        "#FF1F77B4",
        "#FFFF7F0E",
        "#FF2CA02C",
        "#FFD62728",
        "#FF9467BD",
        "#FF8C564B",
        "#FFE377C2",
        "#FF7F7F7F",
        "#FFBCBD22",
        "#FF17BECF",
    ];

    [ObservableProperty]
    public partial string[] SerialPortNames { get; set; } = SerialPort.GetPortNames();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SwitchConnectCommand))]
    public partial string? SerialPortName { get; set; } =
        SerialPort.GetPortNames().FirstOrDefault();

    [ObservableProperty]
    public partial int BaudRate { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(
        nameof(RefreshSerialPortNamesCommand),
        nameof(PlayCommand),
        nameof(PauseCommand),
        nameof(StopCommand)
    )]
    public partial bool Connected { get; set; }

    [ObservableProperty]
    public partial int Cycle { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(
        nameof(SwitchConnectCommand),
        nameof(PlayCommand),
        nameof(PauseCommand),
        nameof(StopCommand),
        nameof(AddVariableCommand),
        nameof(DeleteVariableCommand),
        nameof(SelectVariableCommand),
        nameof(SelectVariableColorCommand)
    )]
    public partial OscilloscopeStatus Status { get; set; }

    public ObservableCollection<VariableViewModel> Variables { get; } =
    [new() { Color = "#FF1F77B4" }];

    public event Action<double[]>? ReceiveData;

    private SerialPort? port;

    private readonly Pipe pipe = new();

    private Task? copyTask;

    private Task? readTask;

    private Memory<byte> memory = Memory<byte>.Empty;

    partial void OnStatusChanged(OscilloscopeStatus value) =>
        WeakReferenceMessenger.Default.Send(new OscilloscopeMessage(value));

    [RelayCommand(CanExecute = nameof(RefreshSerialPortNamesCanExecute))]
    private void RefreshSerialPortNames() => SerialPortNames = SerialPort.GetPortNames();

    private bool RefreshSerialPortNamesCanExecute() => !Connected;

    [RelayCommand(CanExecute = nameof(SwitchConnectCanExecute))]
    private async Task SwitchConnectAsync()
    {
        if (Connected)
        {
            port = new SerialPort(SerialPortName, BaudRate);
            try
            {
                port.Open();
            }
            catch { }
            if (!port.IsOpen)
            {
                port.Dispose();
                Connected = false;
                return;
            }
            copyTask = port
                .BaseStream.CopyToAsync(pipe.Writer)
                .ContinueWith(_ => pipe.Writer.CompleteAsync());
            readTask = ReadAsync(pipe.Reader).ContinueWith(_ => pipe.Reader.CompleteAsync());
        }
        else
        {
            if (port is not null)
            {
                try
                {
                    port.Close();
                }
                catch { }
                port.Dispose();
            }
            if (copyTask is not null)
                await copyTask;
            if (readTask is not null)
                await readTask;
            pipe.Reset();
        }
    }

    private async Task ReadAsync(PipeReader reader)
    {
        var result = default(ReadResult);
        while (!result.IsCompleted)
        {
            result = await reader.ReadAsync();
            if (Status != OscilloscopeStatus.Play || memory.Length == 0)
            {
                reader.AdvanceTo(result.Buffer.End);
                continue;
            }
            var buffer = result.Buffer;
            if (buffer.Length >= memory.Length)
            {
                var frame = buffer.Slice(0, memory.Length);
                frame.CopyTo(memory.Span);
                ReceiveData?.Invoke(ReadSpan(memory.Span));
                reader.AdvanceTo(frame.End);
            }
            else
            {
                reader.AdvanceTo(buffer.Start, buffer.End);
            }
        }
    }

    private double[] ReadSpan(ReadOnlySpan<byte> span)
    {
        var results = new double[Variables.Count];
        for (var i = 0; i < results.Length; i++)
        {
            results[i] = Variables[i].Variable.TypeCode switch
            {
                TypeCode.Boolean => span.ReadBool() ? 1 : 0,
                TypeCode.Char => span.ReadChar(),
                TypeCode.SByte => span.ReadSByte(),
                TypeCode.Byte => span.ReadByte(),
                TypeCode.Int16 => span.ReadInt16(),
                TypeCode.UInt16 => span.ReadUInt16(),
                TypeCode.Int32 => span.ReadInt32(),
                TypeCode.UInt32 => span.ReadUInt32(),
                TypeCode.Int64 => span.ReadInt64(),
                TypeCode.UInt64 => span.ReadUInt64(),
                TypeCode.Single => span.ReadFloat32(),
                TypeCode.Double => span.ReadFloat64(),
                _ => span.ReadInt32(),
            };
        }
        return results;
    }

    private bool SwitchConnectCanExecute() =>
        SerialPortName is not null && Status == OscilloscopeStatus.Stop;

    [RelayCommand(CanExecute = nameof(PlayCanExecute))]
    private async Task PlayAsync()
    {
        if (port is null)
            return;
        memory = new Memory<byte>(new byte[Variables.Sum(v => v.Variable.Size)]);
        var bytes = new byte[10];
        await port.BaseStream.WriteAsync(bytes);
        Status = OscilloscopeStatus.Play;
    }

    private bool PlayCanExecute() => Connected && Status != OscilloscopeStatus.Play;

    [RelayCommand(CanExecute = nameof(PauseCanExecute))]
    private async Task PauseAsync()
    {
        if (port is null)
            return;
        Status = OscilloscopeStatus.Pause;
        var bytes = new byte[10];
        await port.BaseStream.WriteAsync(bytes);
        pipe.Reader.CancelPendingRead();
    }

    private bool PauseCanExecute() => Connected && Status == OscilloscopeStatus.Play;

    [RelayCommand(CanExecute = nameof(StopCanExecute))]
    private async Task StopAsync()
    {
        if (port is null)
            return;
        Status = OscilloscopeStatus.Stop;
        var bytes = new byte[10];
        await port.BaseStream.WriteAsync(bytes);
        pipe.Reader.CancelPendingRead();
    }

    private bool StopCanExecute() => Connected && Status != OscilloscopeStatus.Stop;

    [RelayCommand(CanExecute = nameof(VariableCanExecute))]
    private async Task SaveVariableAsync()
    {
        var message = WeakReferenceMessenger.Default.Send(
            new SaveFileMessage("保存变量", "variables.json", "json|*.json")
        );
        if (message.Response is null)
            return;
        if (File.Exists(message.Response))
            File.Delete(message.Response);
        await using var stream = File.OpenWrite(message.Response);
        await JsonSerializer.SerializeAsync(stream, Variables);
    }

    [RelayCommand(CanExecute = nameof(VariableCanExecute))]
    private async Task OpenVariableAsync()
    {
        var message = WeakReferenceMessenger.Default.Send(
            new OpenFileMessage("加载变量", "json|*.json")
        );
        if (message.Response is null || !File.Exists(message.Response))
            return;
        await using var stream = File.OpenRead(message.Response);
        var variables = await JsonSerializer.DeserializeAsync<List<VariableViewModel>>(stream);
        if (variables is null)
            return;
        Variables.Clear();
        foreach (var variable in variables)
            Variables.Add(variable);
    }

    [RelayCommand(CanExecute = nameof(VariableCanExecute))]
    private void AddVariable() =>
        Variables.Add(
            new()
            {
                Color = Colors.FirstOrDefault(c => Variables.All(v => v.Color != c)) ?? "#000000",
            }
        );

    [RelayCommand(CanExecute = nameof(VariableCanExecute))]
    private void DeleteVariable(VariableViewModel vm) => Variables.Remove(vm);

    [RelayCommand(CanExecute = nameof(VariableCanExecute))]
    private void SelectVariable(VariableViewModel vm) { }

    [RelayCommand(CanExecute = nameof(VariableCanExecute))]
    private async Task SelectVariableColorAsync(VariableViewModel vm) =>
        vm.Color = await WeakReferenceMessenger.Default.Send(new VariableColorMessage(vm.Color));

    private bool VariableCanExecute() => Status == OscilloscopeStatus.Stop;
}
