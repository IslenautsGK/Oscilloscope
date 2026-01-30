using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Oscilloscope.Messages;

internal sealed class OpenFileMessage(string title, string filter) : RequestMessage<string?>
{
    public string Title => title;
    public string Filter => filter;
}
