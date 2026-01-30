namespace Oscilloscope.Models;

internal sealed record AppStatusRecord(string? SerialPortName, int BaudRate, int Cycle);
