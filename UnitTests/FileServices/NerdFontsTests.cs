using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Terminal.Gui;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.FileServicesTests {

    public class NerdFontTests
    {
        [Fact]
        public void TestAllFilenamesMapToKnownGlyphs()
        {
            var f = new NerdFonts();
            foreach(var k in f.FilenameToIcon)
            {
                Assert.Contains(k.Value, f.Glyphs.Keys);
            }
        }


        [Fact]
        public void TestAllExtensionsMapToKnownGlyphs()
        {
            var f = new NerdFonts();
            foreach(var k in f.ExtensionToIcon)
            {
                Assert.Contains(k.Value, f.Glyphs.Keys);
            }
        }
    }

}