using System.Buffers;
using System.Runtime.CompilerServices;
using Oscilloscope;

namespace Oscilloscope.Helpers;

internal readonly record struct ModbusProtocol(int Length)
{
    private readonly TaskCompletionSource<Memory<byte>> tcs = new();

    public bool HandleData(ReadOnlySequence<byte> data)
    {
        if (data.Length < Length)
            return false;
        var bytes = new byte[Length];
        data.Slice(0, Length).CopyTo(bytes);
        if (!ModbusCRC16.VerifyCRC(bytes))
        {
            tcs.SetException(new FormatException("CRC 校验失败"));
            return true;
        }
        tcs.SetResult(bytes);
        return true;
    }

    public TaskAwaiter<Memory<byte>> GetAwaiter() =>
        tcs.Task.WaitAsync(TimeSpan.FromSeconds(1)).GetAwaiter();
}
