using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Oscilloscope;

internal sealed class SaveFileMessage(string title, string fileName, string filter)
    : RequestMessage<string?>
{
    public string Title => title;

    public string FileName => fileName;

    public string Filter => filter;
}
