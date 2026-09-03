// Claude - Opus 4.6

using System.Reflection;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="TreeView{T}.DefaultKeyBindings"/> static property.
/// </summary>
public class TreeViewDefaultKeyBindingsTests
{
    [Fact]
    public void TreeView_DefaultKeyBindings_IsNotNull () => Assert.NotNull (TreeView<ITreeNode>.DefaultKeyBindings);

    [Fact]
    public void TreeView_DefaultKeyBindings_AllKeyStringsParseable ()
    {
        foreach ((Command command, PlatformKeyBinding platformBinding) in TreeView<ITreeNode>.DefaultKeyBindings!)
        {
            Key [] [] allKeyArrays = [platformBinding.All ?? [], platformBinding.Windows ?? [], platformBinding.Linux ?? [], platformBinding.Macos ?? []];

            foreach (Key [] keyArray in allKeyArrays)
            {
                foreach (Key key in keyArray)
                {
                    Assert.NotEqual (Key.Empty, key);
                }
            }
        }
    }

    [Fact]
    public void TreeView_DefaultKeyBindings_AllCommandNamesParseable ()
    {
        foreach (Command command in TreeView<ITreeNode>.DefaultKeyBindings!.Keys)
        {
            Assert.True (Enum.IsDefined (command), $"Command name '{command}' should parse to a Command enum value.");
        }
    }

    [Fact]
    public void TreeView_DefaultKeyBindings_DoesNotHaveConfigurationPropertyAttribute ()
    {
        PropertyInfo? property =
            typeof (TreeView<ITreeNode>).GetProperty (nameof (TreeView<ITreeNode>.DefaultKeyBindings), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        var attr = property.GetCustomAttributes ().FirstOrDefault (a => a.GetType ().Name == "ConfigurationPropertyAttribute");

        Assert.Null (attr);
    }

    [Fact]
    public void TreeView_DefaultKeyBindings_ContainsUniqueCommands ()
    {
        Dictionary<Command, PlatformKeyBinding> bindings = TreeView<ITreeNode>.DefaultKeyBindings!;

        // Tree-specific commands
        Assert.True (bindings.ContainsKey (Command.Expand), "DefaultKeyBindings should contain Expand.");
        Assert.True (bindings.ContainsKey (Command.ExpandAll), "DefaultKeyBindings should contain ExpandAll.");
        Assert.True (bindings.ContainsKey (Command.Collapse), "DefaultKeyBindings should contain Collapse.");
        Assert.True (bindings.ContainsKey (Command.CollapseAll), "DefaultKeyBindings should contain CollapseAll.");
        Assert.True (bindings.ContainsKey (Command.LineUpToFirstBranch), "DefaultKeyBindings should contain LineUpToFirstBranch.");
        Assert.True (bindings.ContainsKey (Command.LineDownToLastBranch), "DefaultKeyBindings should contain LineDownToLastBranch.");

        // TreeView overrides Start/End to use Home/End instead of Ctrl+Home/Ctrl+End
        Assert.True (bindings.ContainsKey (Command.Start), "DefaultKeyBindings should contain Start.");
        Assert.True (bindings.ContainsKey (Command.End), "DefaultKeyBindings should contain End.");
    }
}
