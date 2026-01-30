namespace Oscilloscope.Messages;

internal sealed record LoadDataMessage(IEnumerable<dynamic> Datas);
