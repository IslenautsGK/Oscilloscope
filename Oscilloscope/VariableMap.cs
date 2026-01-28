using Riok.Mapperly.Abstractions;

namespace Oscilloscope;

[Mapper]
internal static partial class VariableMap
{
    [MapNestedProperties(nameof(VariableViewModel.Variable))]
    [MapperIgnoreSource(nameof(VariableViewModel.CurValue))]
    public static partial ExcelVariable ToExcel(this VariableViewModel viewModel);

    [MapperIgnoreTarget(nameof(VariableInfo.Size))]
    [MapperIgnoreSource(nameof(ExcelVariable.Color))]
    public static partial VariableInfo ToInfo(this ExcelVariable variable);

    public static VariableViewModel ToViewModel(this ExcelVariable variable) =>
        new() { Variable = variable.ToInfo(), Color = variable.Color };
}
