using System.Text.Json.Serialization;

namespace Oscilloscope;

internal readonly record struct VariableInfo(
    string? Name,
    string? Info,
    ulong Address,
    TypeCode TypeCode,
    int BitOffset,
    int BitSize
)
{
    [JsonIgnore]
    public int Size =>
        TypeCode switch
        {
            TypeCode.Boolean or TypeCode.SByte or TypeCode.Byte => 1,
            TypeCode.Char or TypeCode.Int16 or TypeCode.UInt16 => 2,
            TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Single => 4,
            TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Double => 8,
            _ => 4,
        };
}
