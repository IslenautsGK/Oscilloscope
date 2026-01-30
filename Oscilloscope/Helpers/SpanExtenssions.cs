namespace Oscilloscope.Helpers;

internal static class SpanExtenssions
{
    extension(ref ReadOnlySpan<byte> span)
    {
        public bool ReadBool()
        {
            var result = BitConverter.ToBoolean(span);
            span = span[sizeof(bool)..];
            return result;
        }

        public char ReadChar()
        {
            var result = BitConverter.ToChar(span);
            span = span[sizeof(char)..];
            return result;
        }

        public sbyte ReadSByte()
        {
            var result = (sbyte)span[0];
            span = span[sizeof(sbyte)..];
            return result;
        }

        public byte ReadByte()
        {
            var result = span[0];
            span = span[sizeof(byte)..];
            return result;
        }

        public short ReadInt16()
        {
            var result = BitConverter.ToInt16(span);
            span = span[sizeof(short)..];
            return result;
        }

        public ushort ReadUInt16()
        {
            var result = BitConverter.ToUInt16(span);
            span = span[sizeof(ushort)..];
            return result;
        }

        public int ReadInt32()
        {
            var result = BitConverter.ToInt32(span);
            span = span[sizeof(int)..];
            return result;
        }

        public uint ReadUInt32()
        {
            var result = BitConverter.ToUInt32(span);
            span = span[sizeof(uint)..];
            return result;
        }

        public long ReadInt64()
        {
            var result = BitConverter.ToInt64(span);
            span = span[sizeof(long)..];
            return result;
        }

        public ulong ReadUInt64()
        {
            var result = BitConverter.ToUInt64(span);
            span = span[sizeof(ulong)..];
            return result;
        }

        public Int128 ReadInt128()
        {
            var result = BitConverter.ToInt128(span);
            span = span[16..];
            return result;
        }

        public UInt128 ReadUInt128()
        {
            var result = BitConverter.ToUInt128(span);
            span = span[16..];
            return result;
        }

        public Half ReadFloat16()
        {
            var result = BitConverter.ToHalf(span);
            span = span[2..];
            return result;
        }

        public float ReadFloat32()
        {
            var result = BitConverter.ToSingle(span);
            span = span[sizeof(float)..];
            return result;
        }

        public double ReadFloat64()
        {
            var result = BitConverter.ToDouble(span);
            span = span[sizeof(double)..];
            return result;
        }
    }
}
