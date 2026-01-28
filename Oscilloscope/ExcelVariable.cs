namespace Oscilloscope;

internal sealed class ExcelVariable
{
    public string? Name { get; set; }

    public string? Info { get; set; }

    public ulong Address { get; set; }

    public TypeCode TypeCode { get; set; }

    public int BitOffset { get; set; }

    public int BitSize { get; set; }

    public string Color { get; set; } = "#000000";
}
