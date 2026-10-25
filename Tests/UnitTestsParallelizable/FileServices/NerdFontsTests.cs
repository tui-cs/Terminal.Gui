// CoPilot - GPT-6.1
using System.IO.Abstractions.TestingHelpers;
using System.Text;

namespace FileServicesTests;

public class NerdFontTests
{
    [Fact]
    public void TestAllExtensionsMapToKnownGlyphs ()
    {
        NerdFonts f = new ();

        foreach (KeyValuePair<string, string> k in f.ExtensionToIcon)
        {
            Assert.Contains (k.Value, f.Glyphs.Keys);
        }
    }

    [Fact]
    public void TestAllFilenamesMapToKnownGlyphs ()
    {
        NerdFonts f = new ();

        foreach (KeyValuePair<string, string> k in f.FilenameToIcon)
        {
            Assert.Contains (k.Value, f.Glyphs.Keys);
        }
    }

    [Theory]
    [InlineData ("Program.cs", 0xF031B)]
    [InlineData ("app.exe", 0xF08C6)]
    [InlineData ("script.ps1", 0xF07B7)]
    [InlineData ("report.pdf", 0xF0226)]
    [InlineData ("data.xml", 0xF05C0)]
    [InlineData ("README.md", 0xF00BB)]
    [InlineData ("archive.zip", 0xF410)]
    [InlineData ("image.png", 0xF1C5)]
    [InlineData ("unknown.extension", 0xEA7B)]
    public void GetNerdIcon_UsesV3Codepoints (string filename, int codepoint)
    {
        MockFileSystem fileSystem = new ();
        NerdFonts fonts = new ();

        Assert.Equal (new Rune (codepoint), fonts.GetNerdIcon (fileSystem.FileInfo.New (filename), false));
    }

    [Theory]
    [InlineData ("downloads", 0xF024D)]
    [InlineData ("onedrive", 0xF03CA)]
    [InlineData (".azure", 0xF0805)]
    public void GetNerdIcon_UsesV3DirectoryCodepoints (string directory, int codepoint)
    {
        MockFileSystem fileSystem = new ();
        NerdFonts fonts = new ();

        Assert.Equal (new Rune (codepoint), fonts.GetNerdIcon (fileSystem.DirectoryInfo.New (directory), false));
    }

    [Theory]
    [InlineData (false, 0xEA83)]
    [InlineData (true, 0xEAF7)]
    public void GetNerdIcon_UsesDirectoryOpenState (bool isOpen, int codepoint)
    {
        MockFileSystem fileSystem = new ();
        NerdFonts fonts = new ();

        Assert.Equal (new Rune (codepoint), fonts.GetNerdIcon (fileSystem.DirectoryInfo.New ("ordinary-folder"), isOpen));
    }

    [Theory]
    [InlineData ("Project.sln.dotsettings", "nf-fa-gear")]
    [InlineData ("Project.sln.dotsettings.user", "nf-fa-gear")]
    [InlineData ("main.tf.json", "nf-md-code_tags")]
    [InlineData ("main.tfvars.json", "nf-md-code_tags")]
    [InlineData ("main.auto.tfvars", "nf-md-code_tags")]
    [InlineData ("main.auto.tfvars.json", "nf-md-code_tags")]
    [InlineData ("MAIN.AUTO.TFVARS.JSON", "nf-md-code_tags")]
    public void GetNerdIcon_MatchesCompoundExtensions (string filename, string glyphName)
    {
        MockFileSystem fileSystem = new ();
        NerdFonts fonts = new ();

        Assert.Equal (fonts.Glyphs [glyphName], fonts.GetNerdIcon (fileSystem.FileInfo.New (filename), false));
    }

    [Theory]
    [InlineData ("Program.CS", "nf-md-language_csharp")]
    [InlineData ("license", "nf-md-certificate")]
    [InlineData ("readme.MD", "nf-md-book_multiple")]
    [InlineData ("package.APPXBUNDLE", "nf-md-package_variant")]
    public void GetNerdIcon_MatchesFileNamesAndExtensionsIgnoringCase (string filename, string glyphName)
    {
        MockFileSystem fileSystem = new ();
        NerdFonts fonts = new ();

        Assert.Equal (fonts.Glyphs [glyphName], fonts.GetNerdIcon (fileSystem.FileInfo.New (filename), false));
    }

    [Theory]
    [InlineData ("Downloads")]
    [InlineData ("DOWNLOADS")]
    public void GetNerdIcon_MatchesDirectoryNamesIgnoringCase (string directory)
    {
        MockFileSystem fileSystem = new ();
        NerdFonts fonts = new ();

        Assert.Equal (fonts.Glyphs ["nf-md-folder_download"], fonts.GetNerdIcon (fileSystem.DirectoryInfo.New (directory), false));
    }

    [Fact]
    public void GetNerdIcon_PrefersLongestMatchingExtension ()
    {
        MockFileSystem fileSystem = new ();
        NerdFonts fonts = new ();
        fonts.ExtensionToIcon.Clear ();
        fonts.ExtensionToIcon [".json"] = "nf-seti-json";
        fonts.ExtensionToIcon [".tfvars.json"] = "nf-md-code_tags";
        fonts.ExtensionToIcon [".auto.tfvars.json"] = "nf-fa-gear";

        Assert.Equal (fonts.Glyphs ["nf-fa-gear"], fonts.GetNerdIcon (fileSystem.FileInfo.New ("main.auto.tfvars.json"), false));
    }

    [Theory]
    [InlineData ("main.unrecognized.json")]
    [InlineData ("maintfvars.json")]
    public void GetNerdIcon_FallsBackToFinalExtension (string filename)
    {
        MockFileSystem fileSystem = new ();
        NerdFonts fonts = new ();

        Assert.Equal (fonts.Glyphs ["nf-seti-json"], fonts.GetNerdIcon (fileSystem.FileInfo.New (filename), false));
    }
}
