using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Oscilloscope;

internal sealed class RequestDataMessage : RequestMessage<IEnumerable<Dictionary<string, double>>>;
