using Oscilloscope.Models;
using Oscilloscope.ViewModels;
using Riok.Mapperly.Abstractions;

namespace Oscilloscope.Maps;

[Mapper]
internal static partial class VariableMap
{
    [MapNestedProperties(nameof(VariableViewModel.Variable))]
    [MapperIgnoreSource(nameof(VariableViewModel.CurValue))]
    public static partial ExcelVariable ToExcel(this VariableViewModel viewModel);

    [MapperIgnoreTarget(nameof(VariableInfo.Size))]
    [MapperIgnoreSource(nameof(ExcelVariable.DisplayName))]
    [MapperIgnoreSource(nameof(ExcelVariable.Color))]
    public static partial VariableInfo ToInfo(this ExcelVariable variable);

    public static VariableViewModel ToViewModel(this ExcelVariable variable) =>
        new()
        {
            Variable = variable.ToInfo(),
            DisplayName = variable.DisplayName,
            Color = variable.Color,
        };
}
