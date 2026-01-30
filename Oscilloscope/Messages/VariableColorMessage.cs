using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Oscilloscope.Messages;

internal sealed class VariableColorMessage(string color) : AsyncRequestMessage<string>
{
    public string Color => color;
}
