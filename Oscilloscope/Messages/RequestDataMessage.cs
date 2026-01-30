using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Oscilloscope.Messages;

internal sealed class RequestDataMessage : RequestMessage<IEnumerable<Dictionary<string, double>>>;
