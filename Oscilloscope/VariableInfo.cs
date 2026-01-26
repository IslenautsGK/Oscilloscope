namespace Oscilloscope;

internal readonly record struct VariableInfo(
    string? Name,
    string? Info,
    nuint Address,
    int BitField,
    int Size
);
