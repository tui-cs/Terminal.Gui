using Xunit;

namespace Terminal.Gui.TextTests;

#nullable enable

public class StringTests {
	[Fact]
	public void TestGetColumns_Empty ()
	{
		var str = string.Empty;
		Assert.Equal (0, str.GetColumns ());
	}

	[Fact]
	public void TestGetColumns_Null ()
	{
		string? str = null;
		Assert.Equal (0, str.GetColumns ());
	}

	[Fact]
	public void TestGetColumns_SingleRune ()
	{
		var str = "a";
		Assert.Equal (1, str.GetColumns ());
	}

	[Theory]
	[InlineData ("a", 1)]
	[InlineData ("á", 1)]
	[InlineData ("ab", 2)]
	[InlineData ("áé", 2)]
	[InlineData ("abc", 3)]
	[InlineData ("áéí", 3)]
	[InlineData ("abcd", 4)]
	public void TestGetColumns_MultiRune (string str, int expected)
	{
		Assert.Equal (expected, str.GetColumns ());
	}

	// Test known wide codepoints
	[Theory]
	[InlineData ("🙂", 2)]
	[InlineData ("a🙂", 3)]
	[InlineData ("🙂a", 3)]
	[InlineData ("👨‍👩‍👦‍👦", 8)]
	[InlineData ("👨‍👩‍👦‍👦🙂", 10)]
	[InlineData ("👨‍👩‍👦‍👦🙂a", 11)]
	[InlineData ("👨‍👩‍👦‍👦a🙂", 11)]
	[InlineData ("👨‍👩‍👦‍👦👨‍👩‍👦‍👦", 16)]
	[InlineData ("山", 2)] // The character for "mountain" in Chinese/Japanese/Korean (山), Unicode U+5C71
	[InlineData ("山🙂", 4)] // The character for "mountain" in Chinese/Japanese/Korean (山), Unicode U+5C71
	public void TestGetColumns_MultiRune_WideBMP (string str, int expected)
	{
		Assert.Equal (expected, str.GetColumns ());
	}

	// Test non-BMP codepoints 
	// Face with Tears of Joy Emoji (😂), Unicode U+1F602 is 2 columns wide
	[Theory]
	[InlineData ("😂", 2)]
	[InlineData ("😂😂", 4)]
	public void TestGetColumns_MultiRune_NonBMP (string str, int expected)
	{
		Assert.Equal (expected, str.GetColumns ());
	}

}

