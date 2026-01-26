using System.Buffers;
using System.Collections.ObjectModel;
using System.IO.Pipelines;
using System.IO.Ports;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Oscilloscope;

internal sealed partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string[] SerialPortNames { get; set; } = SerialPort.GetPortNames();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SwitchConnectCommand))]
    public partial string? SerialPortName { get; set; } =
        SerialPort.GetPortNames().FirstOrDefault();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(
        nameof(RefreshSerialPortNamesCommand),
        nameof(PlayCommand),
        nameof(PauseCommand),
        nameof(StopCommand)
    )]
    public partial bool Connected { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(
        nameof(SwitchConnectCommand),
        nameof(PlayCommand),
        nameof(PauseCommand),
        nameof(StopCommand),
        nameof(AddVariableCommand),
        nameof(DeleteVariableCommand)
    )]
    public partial OscilloscopeStatus Status { get; set; }

    public ObservableCollection<VariableViewModel> Variables { get; } = [new()];

    private SerialPort? port;

    private readonly Pipe pipe = new();

    private Task? copyTask;

    private Task? readTask;

    public PipeReader Reader => pipe.Reader;

    [RelayCommand(CanExecute = nameof(RefreshSerialPortNamesCanExecute))]
    private void RefreshSerialPortNames() => SerialPortNames = SerialPort.GetPortNames();

    private bool RefreshSerialPortNamesCanExecute() => !Connected;

    [RelayCommand(CanExecute = nameof(SwitchConnectCanExecute))]
    private async Task SwitchConnectAsync()
    {
        if (Connected)
        {
            port = new SerialPort(SerialPortName);
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
        var span = new byte[10];
        var result = default(ReadResult);
        while (!result.IsCompleted)
        {
            result = await reader.ReadAsync();
            var buffer = result.Buffer;
            if (buffer.Length >= 10)
            {
                var frame = buffer.Slice(0, 10);
                frame.CopyTo(span);
                foreach (var b in span)
                    Console.Write((char)b);
                reader.AdvanceTo(frame.End);
            }
            else
            {
                reader.AdvanceTo(buffer.Start, buffer.End);
            }
        }
    }

    private bool SwitchConnectCanExecute() =>
        SerialPortName is not null && Status == OscilloscopeStatus.Stop;

    [RelayCommand(CanExecute = nameof(PlayCanExecute))]
    private Task PlayAsync()
    {
        Status = OscilloscopeStatus.Play;
        return Task.CompletedTask;
    }

    private bool PlayCanExecute() => Connected && Status != OscilloscopeStatus.Play;

    [RelayCommand(CanExecute = nameof(PauseCanExecute))]
    private Task PauseAsync()
    {
        Status = OscilloscopeStatus.Pause;
        return Task.CompletedTask;
    }

    private bool PauseCanExecute() => Connected && Status == OscilloscopeStatus.Play;

    [RelayCommand(CanExecute = nameof(StopCanExecute))]
    private Task StopAsync()
    {
        Status = OscilloscopeStatus.Stop;
        return Task.CompletedTask;
    }

    private bool StopCanExecute() => Connected && Status != OscilloscopeStatus.Stop;

    [RelayCommand(CanExecute = nameof(VariableCanExecute))]
    private void AddVariable() => Variables.Add(new());

    [RelayCommand(CanExecute = nameof(VariableCanExecute))]
    private void DeleteVariable(VariableViewModel vm) => Variables.Remove(vm);

    private bool VariableCanExecute() => Status == OscilloscopeStatus.Stop;
}
