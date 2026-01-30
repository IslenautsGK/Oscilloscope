using Oscilloscope.Models;

namespace Oscilloscope.Helpers;

internal static class TreeFilterExtensions
{
    private static readonly List<VariableTreeNode> Empty = [];

    extension(List<VariableTreeNode> nodes)
    {
        public List<VariableTreeNode> Filter(string name)
        {
            if (nodes.Count == 0)
                return Empty;
            var result = new List<VariableTreeNode>();
            foreach (var node in nodes)
            {
                var children = node.Children.Filter(name);
                if (node.Variable.Name.Contains(name) || children.Count > 0)
                    result.Add(new(node.Variable, children));
            }
            return result;
        }
    }
}
