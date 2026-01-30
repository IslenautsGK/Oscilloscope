using CommunityToolkit.Mvvm.Messaging.Messages;
using Oscilloscope.Models;

namespace Oscilloscope.Messages;

internal sealed class SelectVariableMessage(List<VariableTreeNode> variableTree)
    : AsyncRequestMessage<SelectVariableResult?>
{
    public List<VariableTreeNode> VariableTree => variableTree;
}
