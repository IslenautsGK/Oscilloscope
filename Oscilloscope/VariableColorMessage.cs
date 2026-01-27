using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Oscilloscope;

internal sealed class VariableColorMessage(string color) : AsyncRequestMessage<string>
{
    public string Color => color;
}
