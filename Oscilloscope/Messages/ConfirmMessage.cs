using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Oscilloscope.Messages;

internal sealed class ConfirmMessage(string title, string message) : AsyncRequestMessage<bool>
{
    public string Title => title;

    public string Message => message;
}
