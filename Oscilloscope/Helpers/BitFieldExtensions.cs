using System.Numerics;

namespace Oscilloscope.Helpers;

internal static class BitFieldExtensions
{
    extension<T>(T number)
        where T : IBinaryInteger<T>
    {
        public T BitField(int offset, int size)
        {
            if (size == 0)
                return number;
            var mask = (T.One << size - 1) - T.One;
            mask = (mask << 1) + T.One;
            return number >> offset & mask;
        }
    }
}
