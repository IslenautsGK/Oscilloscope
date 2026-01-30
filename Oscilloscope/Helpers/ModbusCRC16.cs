using Oscilloscope;

namespace Oscilloscope.Helpers;

internal static class ModbusCRC16
{
    private static readonly ushort[] crc16Table = new ushort[256];

    static ModbusCRC16()
    {
        const ushort polynomial = 0xA001;
        for (ushort i = 0; i < 256; i++)
        {
            ushort value = 0;
            var temp = i;
            for (byte j = 0; j < 8; j++)
            {
                if (((value ^ temp) & 0x0001) != 0)
                    value = (ushort)((value >> 1) ^ polynomial);
                else
                    value >>= 1;
                temp >>= 1;
            }
            crc16Table[i] = value;
        }
    }

    public static ushort CalculateCRC(ReadOnlySpan<byte> data)
    {
        ushort crc = 0xFFFF;
        foreach (var b in data)
            crc = (ushort)(crc >> 8 ^ crc16Table[(crc ^ b) & 0xFF]);
        return crc;
    }

    public static bool VerifyCRC(ReadOnlySpan<byte> data)
    {
        if (data.Length < 2)
            return false;
        return CalculateCRC(data[..^2]) == BitConverter.ToUInt16(data[^2..]);
    }
}
