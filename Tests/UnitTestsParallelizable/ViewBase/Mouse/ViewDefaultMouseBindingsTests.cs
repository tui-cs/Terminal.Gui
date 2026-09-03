// Copilot

using System.Reflection;

namespace ViewBaseTests.MouseTests;

/// <summary>
///     Tests for <see cref="View.DefaultMouseBindings"/> and <see cref="View.ViewMouseBindings"/> static properties.
/// </summary>
public class ViewDefaultMouseBindingsTests
{
    [Fact]
    public void View_DefaultMouseBindings_IsNotNull () => Assert.NotNull (View.DefaultMouseBindings);

    [Theory]
    [InlineData (Command.Activate)]
    [InlineData (Command.Context)]
    [InlineData (Command.StartSelection)]
    [InlineData (Command.StartRectangleSelection)]
    public void View_DefaultMouseBindings_ContainsExpectedCommands (Command command) => Assert.True (View.DefaultMouseBindings!.ContainsKey (command));

    [Fact]
    public void View_DefaultMouseBindings_AllMouseFlagsParseable ()
    {
        foreach ((Command _, PlatformMouseBinding platformBinding) in View.DefaultMouseBindings!)
        {
            MouseFlags [] [] allMouseFlagArrays =
            [
                platformBinding.All ?? [],
                platformBinding.Windows ?? [],
                platformBinding.Linux ?? [],
                platformBinding.Macos ?? []
            ];

            foreach (MouseFlags [] mouseFlagsArray in allMouseFlagArrays)
            {
                foreach (MouseFlags mouseFlags in mouseFlagsArray)
                {
                    Assert.NotEqual (MouseFlags.None, mouseFlags);
                }
            }
        }
    }

    [Fact]
    public void View_DefaultMouseBindings_HasConfigurationPropertyAttribute ()
    {
        PropertyInfo? property = typeof (View).GetProperty (nameof (View.DefaultMouseBindings), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        System.Attribute? attr = property.GetCustomAttributes ().FirstOrDefault (a => a.GetType ().Name == "ConfigurationPropertyAttribute");

        Assert.Null (attr);
    }

    [Fact]
    public void View_ViewMouseBindings_IsNull_ByDefault () => Assert.Null (View.ViewMouseBindings);

    [Fact]
    public void View_ViewMouseBindings_HasConfigurationPropertyAttribute ()
    {
        PropertyInfo? property = typeof (View).GetProperty (nameof (View.ViewMouseBindings), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        System.Attribute? attr = property.GetCustomAttributes ().FirstOrDefault (a => a.GetType ().Name == "ConfigurationPropertyAttribute");

        Assert.Null (attr);
    }
}
