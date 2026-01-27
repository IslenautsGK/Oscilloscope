using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Oscilloscope;

internal sealed class OpenFileMessage(string title, string filter) : RequestMessage<string?>
{
    public string Title => title;
    public string Filter => filter;
}
