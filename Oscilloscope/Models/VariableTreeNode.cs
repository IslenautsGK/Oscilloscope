namespace Oscilloscope.Models;

internal sealed class VariableTreeNode(VariableInfo variable, List<VariableTreeNode> children)
{
    public VariableInfo Variable => variable;

    public List<VariableTreeNode> Children => children;
}
