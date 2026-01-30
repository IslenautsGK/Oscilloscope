namespace Oscilloscope.Models;

internal sealed class VariableTreeNode(VariableInfo variable)
{
    public VariableInfo Variable => variable;

    public List<VariableTreeNode> Children { get; } = [];
}
