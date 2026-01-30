namespace Oscilloscope.Models;

internal sealed class ExcelVariable
{
    public string Name { get; set; } = "";

    public uint Address { get; set; }

    public TypeCode TypeCode { get; set; }

    public int BitOffset { get; set; }

    public int BitSize { get; set; }

    public string? DisplayName { get; set; }

    public string Color { get; set; } = "#000000";
}
