using Riok.Mapperly.Abstractions;

namespace Oscilloscope;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
internal static partial class AppStatusRecordMap
{
    public static partial AppStatusRecord ToRecord(this MainViewModel viewModel);

    public static partial void ToViewModel(this AppStatusRecord record, MainViewModel viewModel);
}
