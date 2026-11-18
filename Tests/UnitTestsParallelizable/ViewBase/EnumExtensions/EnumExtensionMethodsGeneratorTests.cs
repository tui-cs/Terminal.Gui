namespace EnumExtensionsTests;

/// <summary>
///     Tests for the source-generated enum extension methods produced by
///     <c>Terminal.Gui.Analyzers.Internal.EnumExtensionMethodsGenerator</c>.
/// </summary>
public class EnumExtensionMethodsGeneratorTests
{
    // ──── AsInt32 ────

    [Fact]
    public void AsInt32_Returns_Correct_Value_For_Regular_Enum ()
    {
        Alignment value = Alignment.Center;
        int result = value.AsInt32 ();

        Assert.Equal (2, result);
    }

    [Fact]
    public void AsInt32_Returns_Zero_For_Default ()
    {
        Alignment value = default;
        int result = value.AsInt32 ();

        Assert.Equal (0, result);
    }

    [Fact]
    public void AsInt32_Returns_Correct_Value_For_Flags_Enum ()
    {
        AlignmentModes value = AlignmentModes.EndToStart | AlignmentModes.AddSpaceBetweenItems;
        int result = value.AsInt32 ();

        Assert.Equal (3, result);
    }

    // ──── AsUInt32 ────

    [Fact]
    public void AsUInt32_Returns_Correct_Value_For_Regular_Enum ()
    {
        Alignment value = Alignment.End;
        uint result = value.AsUInt32 ();

        Assert.Equal (1u, result);
    }

    [Fact]
    public void AsUInt32_Returns_Correct_Value_For_Flags_Enum ()
    {
        AlignmentModes value = AlignmentModes.AddSpaceBetweenItems;
        uint result = value.AsUInt32 ();

        Assert.Equal (2u, result);
    }

    // ──── FastIsDefined ────

    [Theory]
    [InlineData (0, true)]
    [InlineData (1, true)]
    [InlineData (2, true)]
    [InlineData (3, true)]
    [InlineData (4, false)]
    [InlineData (-1, false)]
    [InlineData (99, false)]
    public void FastIsDefined_Returns_Expected_For_Regular_Enum (int value, bool expected)
    {
        bool result = default (Alignment).FastIsDefined (value);

        Assert.Equal (expected, result);
    }

    [Theory]
    [InlineData (0, true)]
    [InlineData (1, true)]
    [InlineData (2, true)]
    [InlineData (4, true)]
    [InlineData (3, false)]  // combined flags value, not explicitly defined
    [InlineData (8, false)]
    public void FastIsDefined_Returns_Expected_For_Flags_Enum (int value, bool expected)
    {
        bool result = default (AlignmentModes).FastIsDefined (value);

        Assert.Equal (expected, result);
    }

    // ──── FastHasFlags ────

    [Fact]
    public void FastHasFlags_Returns_True_When_Flag_Is_Set ()
    {
        AlignmentModes value = AlignmentModes.EndToStart | AlignmentModes.AddSpaceBetweenItems;
        bool result = value.FastHasFlags (AlignmentModes.EndToStart);

        Assert.True (result);
    }

    [Fact]
    public void FastHasFlags_Returns_False_When_Flag_Is_Not_Set ()
    {
        AlignmentModes value = AlignmentModes.EndToStart;
        bool result = value.FastHasFlags (AlignmentModes.AddSpaceBetweenItems);

        Assert.False (result);
    }

    [Fact]
    public void FastHasFlags_Returns_True_For_Zero_Check ()
    {
        AlignmentModes value = AlignmentModes.EndToStart;
        bool result = value.FastHasFlags (default (AlignmentModes));

        Assert.True (result);
    }

    [Fact]
    public void FastHasFlags_Int_Overload_Returns_True_When_Mask_Matches ()
    {
        AlignmentModes value = AlignmentModes.EndToStart | AlignmentModes.AddSpaceBetweenItems;
        bool result = value.FastHasFlags (1); // EndToStart = 1

        Assert.True (result);
    }

    [Fact]
    public void FastHasFlags_Int_Overload_Returns_False_When_Mask_Does_Not_Match ()
    {
        AlignmentModes value = AlignmentModes.StartToEnd;
        bool result = value.FastHasFlags (4); // IgnoreFirstOrLast = 4

        Assert.False (result);
    }

    // ──── ViewDiagnosticFlags (uint-backed enum) ────

    [Fact]
    public void AsInt32_Works_For_UInt_Backed_Enum ()
    {
        ViewDiagnosticFlags value = ViewDiagnosticFlags.Ruler;
        int result = value.AsInt32 ();

        Assert.Equal (1, result);
    }

    [Fact]
    public void AsUInt32_Works_For_UInt_Backed_Enum ()
    {
        ViewDiagnosticFlags value = ViewDiagnosticFlags.Ruler;
        uint result = value.AsUInt32 ();

        Assert.Equal (1u, result);
    }

    [Fact]
    public void FastHasFlags_Works_For_UInt_Backed_Flags_Enum ()
    {
        ViewDiagnosticFlags value = ViewDiagnosticFlags.Ruler | ViewDiagnosticFlags.Thickness;
        bool result = value.FastHasFlags (ViewDiagnosticFlags.Thickness);

        Assert.True (result);
    }

    // ──── BorderSettings (Flags enum) ────

    [Fact]
    public void FastHasFlags_Works_For_BorderSettings ()
    {
        BorderSettings value = BorderSettings.Title | BorderSettings.Gradient;
        bool result = value.FastHasFlags (BorderSettings.Title);

        Assert.True (result);
    }

    [Fact]
    public void FastIsDefined_BorderSettings_Returns_Expected ()
    {
        Assert.True (default (BorderSettings).FastIsDefined (0));
        Assert.True (default (BorderSettings).FastIsDefined (1));
        Assert.True (default (BorderSettings).FastIsDefined (2));
        Assert.True (default (BorderSettings).FastIsDefined (4));
        Assert.True (default (BorderSettings).FastIsDefined (8));
    }
}
