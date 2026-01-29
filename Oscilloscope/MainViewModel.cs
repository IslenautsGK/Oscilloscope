using System.Buffers;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Pipelines;
using System.IO.Ports;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MiniExcelLibs;
using MiniExcelLibs.OpenXml;

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

    private static readonly OpenXmlConfiguration ExcelConfig = new()
    {
        TableStyles = TableStyles.None,
        AutoFilter = false,
        FastMode = true,
    };

    private static readonly byte[] stopBytes = [1, 24, 0, 42];

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
        nameof(StopCommand),
        nameof(RunUserCommandCommand)
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
        nameof(RunUserCommandCommand),
        nameof(SaveVariableCommand),
        nameof(OpenVariableCommand),
        nameof(AddVariableCommand),
        nameof(DeleteVariableCommand),
        nameof(SelectVariableCommand),
        nameof(SelectVariableColorCommand),
        nameof(SaveToExcelCommand),
        nameof(LoadFromExcelCommand)
    )]
    public partial OscilloscopeStatus Status { get; set; }

    [ObservableProperty]
    public partial double CurTime { get; set; }

    [ObservableProperty]
    public partial IEnumerable<UserCommand> UserCommands { get; set; }

    public ObservableCollection<VariableViewModel> Variables { get; } =
    [new() { Color = "#FF1F77B4" }];

    public event Action<double[]>? ReceiveData;

    private AppStatusRecord? appStatusRecord;

    private SerialPort? port;

    private readonly Pipe pipe = new();

    private Task? copyTask;

    private Task? readTask;

    private ModbusProtocol? protocol;

    private Memory<byte> memory = Memory<byte>.Empty;

    partial void OnStatusChanged(OscilloscopeStatus value) =>
        WeakReferenceMessenger.Default.Send(new OscilloscopeMessage(value));

    [RelayCommand]
    private async Task LoadAppStatusAsync()
    {
        if (File.Exists("app_status.json"))
        {
            await using var stream = File.OpenRead("app_status.json");
            appStatusRecord = await JsonSerializer.DeserializeAsync<AppStatusRecord>(stream);
            appStatusRecord?.ToViewModel(this);
        }
        if (File.Exists("user_commands.json"))
        {
            await using var stream = File.OpenRead("user_commands.json");
            UserCommands =
                await JsonSerializer.DeserializeAsync<IEnumerable<UserCommand>>(stream) ?? [];
        }
    }

    public async Task SaveAppStatusAsync()
    {
        var record = this.ToRecord();
        if (record == appStatusRecord)
            return;
        File.Delete("app_status.json");
        await using var stream = File.OpenWrite("app_status.json");
        await JsonSerializer.SerializeAsync(stream, this.ToRecord());
    }

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
            catch
            {
                port.Dispose();
                Connected = false;
                throw;
            }
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
            if (port is { IsOpen: true })
            {
                try
                {
                    port.Close();
                }
                catch
                {
                    Connected = port.IsOpen;
                    throw;
                }
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
            var buffer = result.Buffer;
            if (Status != OscilloscopeStatus.Play)
            {
                if (this.protocol is { } protocol)
                {
                    if (protocol.HandleData(buffer))
                        reader.AdvanceTo(buffer.GetPosition(protocol.Length));
                    else
                        reader.AdvanceTo(buffer.Start, buffer.End);
                }
                else
                {
                    reader.AdvanceTo(buffer.End);
                }
            }
            else if (memory.Length == 0)
            {
                reader.AdvanceTo(buffer.End);
            }
            else
            {
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
    }

    private double[] ReadSpan(ReadOnlySpan<byte> span)
    {
        var results = new double[Variables.Count];
        if (!ModbusCRC16.VerifyCRC(span))
        {
            Array.Fill(results, double.NaN);
            return results;
        }
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
        memory = new Memory<byte>(new byte[Variables.Sum(v => v.Variable.Size) + 2]);
        var bytes = new byte[Variables.Count * 5 + 4];
        bytes[0] = 1;
        bytes[1] = 23;
        var span = bytes.AsSpan();
        for (var i = 0; i < Variables.Count; i++)
        {
            BitConverter.TryWriteBytes(span[(i * 5 + 2)..], Variables[i].Variable.Address);
            span[i * 5 + 6] = Variables[i].Variable.Size;
        }
        BitConverter.TryWriteBytes(span[^2..], ModbusCRC16.CalculateCRC(span[..^2]));
        protocol = null;
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
        await port.BaseStream.WriteAsync(stopBytes);
        pipe.Reader.CancelPendingRead();
    }

    private bool PauseCanExecute() => Connected && Status == OscilloscopeStatus.Play;

    [RelayCommand(CanExecute = nameof(StopCanExecute))]
    private async Task StopAsync()
    {
        if (port is null)
            return;
        Status = OscilloscopeStatus.Stop;
        await port.BaseStream.WriteAsync(stopBytes);
        pipe.Reader.CancelPendingRead();
    }

    private bool StopCanExecute() => Connected && Status != OscilloscopeStatus.Stop;

    [RelayCommand(CanExecute = nameof(RunUserCommandCanExecute))]
    private async Task RunUserCommandAsync(UserCommand command)
    {
        if (port is null)
            return;
        protocol = new(command.Length);
        try
        {
            await port.BaseStream.WriteAsync(command.Send);
            await protocol.Value;
        }
        finally
        {
            protocol = null;
        }
    }

    private bool RunUserCommandCanExecute() => Connected && Status != OscilloscopeStatus.Play;

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

    [RelayCommand(CanExecute = nameof(SaveToExcelCanExecute))]
    private async Task SaveToExcelAsync()
    {
        var fileName = WeakReferenceMessenger.Default.Send(
            new SaveFileMessage("保存数据", "data.xlsx", "Excel|*.xlsx")
        );
        if (fileName.Response is null)
            return;
        File.Delete(fileName.Response);
        var data = WeakReferenceMessenger.Default.Send(new RequestDataMessage());
        await MiniExcel.SaveAsAsync(
            fileName.Response,
            data.Response,
            sheetName: "数据",
            configuration: ExcelConfig
        );
        await MiniExcel.InsertAsync(
            fileName.Response,
            Variables.Select(VariableMap.ToExcel),
            sheetName: "变量",
            configuration: ExcelConfig
        );
    }

    private bool SaveToExcelCanExecute() => Status != OscilloscopeStatus.Play;

    [RelayCommand(CanExecute = nameof(VariableCanExecute))]
    private async Task LoadFromExcelAsync()
    {
        var message = WeakReferenceMessenger.Default.Send(
            new OpenFileMessage("加载数据", "Excel|*.xlsx")
        );
        if (message.Response is null || !File.Exists(message.Response))
            return;
        var variables = await MiniExcel.QueryAsync<ExcelVariable>(message.Response, "变量");
        Variables.Clear();
        foreach (var variable in variables)
            Variables.Add(variable.ToViewModel());
        var datas = await MiniExcel.QueryAsync(message.Response, true, "数据");
        WeakReferenceMessenger.Default.Send(new LoadDataMessage(datas));
    }
}
