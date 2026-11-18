namespace Terminal.Gui.Drawing;

/// <summary>
///     Represents standard color names with their RGB values. Derived from the W3C color names, but includes other common
///     names.
/// </summary>
/// <remarks>
///     Based on https://www.w3schools.com/colors/color_tryit.asp page.
/// </remarks>
public enum StandardColor
{
    /// <summary>
    ///     Alice blue RGB(240, 248, 255).
    ///     <para>
    ///         A very pale shade of blue, almost white.
    ///     </para>
    /// </summary>
    AliceBlue = 0xF0F8FF,

    /// <summary>
    ///     Antique white RGB(250, 235, 215).
    ///     <para>
    ///         A light beige color with a hint of orange.c
    ///     </para>
    /// </summary>
    AntiqueWhite = 0xFAEBD7,

    /// <summary>
    ///     Amber Phosphor RGB(255, 191, 0).
    /// <para>
    ///     Matches the Amber Phosphor color used on classic terminals.
    /// </para>
    /// </summary>
    AmberPhosphor = 0xFFBF00,

    /// <summary>
    ///     Aqua RGB(0, 255, 255).
    ///     <para>
    ///         A bright cyan color, often associated with water.
    ///     </para>
    /// </summary>
    Aqua = 0x00FFFF,

    /// <summary>
    ///     Aquamarine RGB(127, 255, 212).
    ///     <para>
    ///         A light greenish-blue color, resembling the gemstone aquamarine.
    ///     </para>
    /// </summary>
    Aquamarine = 0x7FFFD4,

    /// <summary>
    ///     Azure RGB(240, 255, 255).
    ///     <para>
    ///         A pale cyan color, resembling the color of the sky on a clear day.
    ///     </para>
    /// </summary>
    Azure = 0xF0FFFF,

    /// <summary>
    ///     Beige RGB(245, 245, 220).
    ///     <para>
    ///         A pale sandy yellow color, often used in interior design.
    ///     </para>
    /// </summary>
    Beige = 0xF5F5DC,

    /// <summary>
    ///     Bisque RGB(255, 228, 196).
    ///     <para>
    ///         A pale orange color, resembling the color of bisque pottery.
    ///     </para>
    /// </summary>
    Bisque = 0xFFE4C4,

    /// <summary>
    ///     Black RGB(0, 0, 0).
    ///     <para>
    ///         The darkest color, representing the absence of light.
    ///     </para>
    /// </summary>
    Black = 0x000000,

    /// <summary>
    ///     Blanched almond RGB(255, 235, 205).
    ///     <para>
    ///         A pale yellowish-orange color, resembling blanched almonds.
    ///     </para>
    /// </summary>
    BlanchedAlmond = 0xFFEBCD,

    /// <summary>
    ///     Blue RGB(0, 0, 255).
    ///     <para>
    ///         A primary color, often associated with the sky and the sea.
    ///     </para>
    /// </summary>
    Blue = 0x0000FF,

    /// <summary>
    ///     Blue violet RGB(138, 43, 226).
    ///     <para>
    ///         A deep purple color with a hint of blue.
    ///     </para>
    /// </summary>
    BlueViolet = 0x8A2BE2,

    /// <summary>
    ///     Brown RGB(165, 42, 42).
    ///     <para>
    ///         A dark reddish-brown color, resembling the color of wood or soil.
    ///     </para>
    /// </summary>
    Brown = 0xA52A2A,

    /// <summary>
    ///     Burly wood RGB(222, 184, 135).
    ///     <para>
    ///         A light brown color, resembling the color of burly wood.
    ///     </para>
    /// </summary>
    BurlyWood = 0xDEB887,

    /// <summary>
    ///     Cadet blue RGB(95, 158, 160).
    ///     <para>
    ///         A light bluish-green color, first used as a color name in English in 1892.
    ///     </para>
    /// </summary>
    CadetBlue = 0x5F9EA0,

    /// <summary>
    ///     Charcoal RGB(54, 69, 79).
    ///     <para>
    ///         A dark grayish-black color, resembling burnt wood.
    ///     </para>
    /// </summary>
    Charcoal = 0x36454F,

    /// <summary>
    ///     Cornflower blue RGB(100, 149, 237).
    ///     <para>
    ///         A medium blue color, resembling the cornflower plant.
    ///     </para>
    /// </summary>
    CornflowerBlue = 0x6495ED,

    /// <summary>
    ///     Cornsilk RGB(255, 248, 220).
    ///     <para>
    ///         A pale yellow color, resembling the silky threads of corn.
    ///     </para>
    /// </summary>
    Cornsilk = 0xFFF8DC,

    /// <summary>
    ///     Crimson RGB(220, 20, 60).
    ///     <para>
    ///         A deep red color, resembling the color of blood.
    ///     </para>
    /// </summary>
    Crimson = 0xDC143C,

    /// <summary>
    ///     Cyan RGB(0, 255, 255).
    ///     <para>
    ///         Same as <see cref="Aqua"/>.
    ///     </para>
    /// </summary>
    Cyan = Aqua,

    /// <summary>
    ///     Dark blue RGB(0, 0, 139).
    ///     <para>
    ///         A very dark shade of blue.
    ///     </para>
    /// </summary>
    DarkBlue = 0x00008B,

    /// <summary>
    ///     Dark cyan RGB(0, 139, 139).
    ///     <para>
    ///         A dark shade of cyan.
    ///     </para>
    /// </summary>
    DarkCyan = 0x008B8B,

    /// <summary>
    ///     Dark goldenrod RGB(184, 134, 11).
    ///     <para>
    ///         A dark yellowish-brown color.
    ///     </para>
    /// </summary>
    DarkGoldenrod = 0xB8860B,

    /// <summary>
    ///     Dark gray RGB(169, 169, 169).
    ///     <para>
    ///         A medium-dark shade of gray.
    ///     </para>
    /// </summary>
    DarkGray = 0xA9A9A9,

    /// <summary>
    ///     Dark green RGB(0, 100, 0).
    ///     <para>
    ///         A dark shade of green.
    ///     </para>
    /// </summary>
    DarkGreen = 0x006400,

    /// <summary>
    ///     Dark grey RGB(169, 169, 169).
    ///     <para>
    ///         Same as <see cref="DarkGray"/>.
    ///     </para>
    /// </summary>
    DarkGrey = DarkGray,

    /// <summary>
    ///     Dark khaki RGB(189, 183, 107).
    ///     <para>
    ///         A dark yellowish-brown color.
    ///     </para>
    /// </summary>
    DarkKhaki = 0xBDB76B,

    /// <summary>
    ///     Dark magenta RGB(139, 0, 139).
    ///     <para>
    ///         A dark shade of magenta.
    ///     </para>
    /// </summary>
    DarkMagenta = 0x8B008B,

    /// <summary>
    ///     Dark olive green RGB(85, 107, 47).
    ///     <para>
    ///         A dark yellowish-green color.
    ///     </para>
    /// </summary>
    DarkOliveGreen = 0x556B2F,

    /// <summary>
    ///     Dark orange RGB(255, 140, 0).
    ///     <para>
    ///         A dark shade of orange.
    ///     </para>
    /// </summary>
    DarkOrange = 0xFF8C00,

    /// <summary>
    ///     Dark orchid RGB(153, 50, 204).
    ///     <para>
    ///         A dark purple color with a hint of blue.
    ///     </para>
    /// </summary>
    DarkOrchid = 0x9932CC,

    /// <summary>
    ///     Dark red RGB(139, 0, 0).
    ///     <para>
    ///         A dark shade of red.
    ///     </para>
    /// </summary>
    DarkRed = 0x8B0000,

    /// <summary>
    ///     Dark salmon RGB(233, 150, 122).
    ///     <para>
    ///         A dark pinkish-orange color.
    ///     </para>
    /// </summary>
    DarkSalmon = 0xE9967A,

    /// <summary>
    ///     Dark sea green RGB(143, 188, 143).
    ///     <para>
    ///         A dark shade of sea green.
    ///     </para>
    /// </summary>
    DarkSeaGreen = 0x8FBC8F,

    /// <summary>
    ///     Dark slate blue RGB(72, 61, 139).
    ///     <para>
    ///         A dark blue color with a hint of gray.
    ///     </para>
    /// </summary>
    DarkSlateBlue = 0x483D8B,

    /// <summary>
    ///     Dark slate gray RGB(47, 79, 79).
    ///     <para>
    ///         A very dark gray color with a hint of green.
    ///     </para>
    /// </summary>
    DarkSlateGray = 0x2F4F4F,

    /// <summary>
    ///     Dark slate grey RGB(47, 79, 79).
    ///     <para>
    ///         Same as <see cref="DarkSlateGray"/>.
    ///     </para>
    /// </summary>
    DarkSlateGrey = DarkSlateGray,

    /// <summary>
    ///     Dark turquoise RGB(0, 206, 209).
    ///     <para>
    ///         A dark shade of turquoise.
    ///     </para>
    /// </summary>
    DarkTurquoise = 0x00CED1,

    /// <summary>
    ///     Dark violet RGB(148, 0, 211).
    ///     <para>
    ///         A dark shade of violet.
    ///     </para>
    /// </summary>
    DarkViolet = 0x9400D3,

    /// <summary>
    ///     Deep pink RGB(255, 20, 147).
    ///     <para>
    ///         A bright and intense pink color.
    ///     </para>
    /// </summary>
    DeepPink = 0xFF1493,

    /// <summary>
    ///     Deep sky blue RGB(0, 191, 255).
    ///     <para>
    ///         A bright and intense sky blue color.
    ///     </para>
    /// </summary>
    DeepSkyBlue = 0x00BFFF,

    /// <summary>
    ///     Dim gray RGB(105, 105, 105).
    ///     <para>
    ///         A medium-dark shade of gray.
    ///     </para>
    /// </summary>
    DimGray = 0x696969,

    /// <summary>
    ///     Dim grey RGB(105, 105, 105).
    ///     <para>
    ///         Same as <see cref="DimGray"/>.
    ///     </para>
    /// </summary>
    DimGrey = DimGray,

    /// <summary>
    ///     Dodger blue RGB(30, 144, 255).
    ///     <para>
    ///         A bright and vibrant blue color.
    ///     </para>
    /// </summary>
    DodgerBlue = 0x1E90FF,

    /// <summary>
    ///     Ebony RGB(85, 93, 80).
    ///     <para>
    ///         A very dark gray color with a slight greenish tint.
    ///     </para>
    /// </summary>
    Ebony = 0x555D50,

    /// <summary>
    ///     Fire brick RGB(178, 34, 34).
    ///     <para>
    ///         A dark reddish-brown color, resembling bricks.
    ///     </para>
    /// </summary>
    FireBrick = 0xB22222,

    /// <summary>
    ///     Floral white RGB(255, 250, 240).
    ///     <para>
    ///         A very pale shade of white with a hint of orange.
    ///     </para>
    /// </summary>
    FloralWhite = 0xFFFAF0,


    /// <summary>
    ///     Fluorescent Orange - Same as Amber Phosphor RGB(255, 191, 0).
    /// <para>
    ///     Matches the Amber Phosphor color used on classic terminals.
    /// </para>
    /// </summary>
    FluorescentOrange = AmberPhosphor,

    /// <summary>
    ///     Forest green RGB(34, 139, 34).
    ///     <para>
    ///         A dark green color, resembling the color of a forest.
    ///     </para>
    /// </summary>
    ForestGreen = 0x228B22,

    /// <summary>
    ///     Fuchsia RGB(255, 0, 255).
    ///     <para>
    ///         A bright and vibrant magenta color.
    ///     </para>
    /// </summary>
    Fuchsia = 0xFF00FF,

    /// <summary>
    ///     Gainsboro RGB(220, 220, 220).
    ///     <para>
    ///         A very light gray color.
    ///     </para>
    /// </summary>
    Gainsboro = 0xDCDCDC,

    /// <summary>
    ///     Ghost white RGB(248, 248, 255).
    ///     <para>
    ///         A very pale shade of white with a hint of blue.
    ///     </para>
    /// </summary>
    GhostWhite = 0xF8F8FF,

    /// <summary>
    ///     Gold RGB(255, 215, 0).
    ///     <para>
    ///         A bright yellow color, resembling gold.
    ///     </para>
    /// </summary>
    Gold = 0xFFD700,

    /// <summary>
    ///     Goldenrod RGB(218, 165, 32).
    ///     <para>
    ///         A dark yellow color with a hint of brown.
    ///     </para>
    /// </summary>
    Goldenrod = 0xDAA520,

    /// <summary>
    ///     Gray RGB(128, 128, 128).
    ///     <para>
    ///         A medium shade of gray.
    ///     </para>
    /// </summary>
    Gray = 0x808080,

    /// <summary>
    ///     Green RGB(0, 128, 0).
    ///     <para>
    ///         A medium shade of green.
    ///     </para>
    /// </summary>
    Green = 0x008000,

    /// <summary>
    ///     Green Phosphor RGB(0, 255, 102).
    /// <para>
    ///     Matches the Green Phosphor color used on classic terminals.
    /// </para>
    /// </summary>
    GreenPhosphor = 0x00FF66,

    /// <summary>
    ///     Green yellow RGB(173, 255, 47).
    ///     <para>
    ///         A bright yellowish-green color.
    ///     </para>
    /// </summary>
    GreenYellow = 0xADFF2F,

    /// <summary>
    ///     Grey RGB(128, 128, 128).
    ///     <para>
    ///         Same as <see cref="Gray"/>.
    ///     </para>
    /// </summary>
    Grey = Gray,

    /// <summary>
    ///     Guppie Green - Same as Green Phosphor RGB(0, 255, 102).
    /// <para>
    ///     Matches the Green Phosphor color used on classic terminals.
    /// </para>
    /// </summary>
    GuppieGreen = GreenYellow,

    /// <summary>
    ///     Honey dew RGB(240, 255, 240).
    ///     <para>
    ///         A very pale greenish-white color.
    ///     </para>
    /// </summary>
    HoneyDew = 0xF0FFF0,

    /// <summary>
    ///     Hot pink RGB(255, 105, 180).
    ///     <para>
    ///         A bright and vibrant pink color.
    ///     </para>
    /// </summary>
    HotPink = 0xFF69B4,

    /// <summary>
    ///     Indian red RGB(205, 92, 92).
    ///     <para>
    ///         A dark reddish-brown color.
    ///     </para>
    /// </summary>
    IndianRed = 0xCD5C5C,

    /// <summary>
    ///     Indigo RGB(75, 0, 130).
    ///     <para>
    ///         A dark blue-purple color.
    ///     </para>
    /// </summary>
    Indigo = 0x4B0082,

    /// <summary>
    ///     Ivory RGB(255, 255, 240).
    ///     <para>
    ///         A very pale yellowish-white color.
    ///     </para>
    /// </summary>
    Ivory = 0xFFFFF0,

    /// <summary>
    ///     Jet RGB(52, 52, 52).
    ///     <para>
    ///         A very dark gray, often used as a near-black.
    ///     </para>
    /// </summary>
    Jet = 0x343434,

    /// <summary>
    ///     Khaki RGB(240, 230, 140).
    ///     <para>
    ///         A pale yellowish-brown color.
    ///     </para>
    /// </summary>
    Khaki = 0xF0E68C,

    /// <summary>
    ///     Lavender RGB(230, 230, 250).
    ///     <para>
    ///         A pale purple color, resembling the lavender flower.
    ///     </para>
    /// </summary>
    Lavender = 0xE6E6FA,

    /// <summary>
    ///     Lavender blush RGB(255, 240, 245).
    ///     <para>
    ///         A very pale pinkish-white color.
    ///     </para>
    /// </summary>
    LavenderBlush = 0xFFF0F5,

    /// <summary>
    ///     Lawn green RGB(124, 252, 0).
    ///     <para>
    ///         A bright green color, resembling freshly cut grass.
    ///     </para>
    /// </summary>
    LawnGreen = 0x7CFC00,

    /// <summary>
    ///     Lemon chiffon RGB(255, 250, 205).
    ///     <para>
    ///         A pale yellow color, resembling lemon chiffon cake.
    ///     </para>
    /// </summary>
    LemonChiffon = 0xFFFACD,

    /// <summary>
    ///     Light blue RGB(173, 216, 230).
    ///     <para>
    ///         A pale shade of blue.
    ///     </para>
    /// </summary>
    LightBlue = 0xADD8E6,

    /// <summary>
    ///     Light coral RGB(240, 128, 128).
    ///     <para>
    ///         A pale pinkish-orange color.
    ///     </para>
    /// </summary>
    LightCoral = 0xF08080,

    /// <summary>
    ///     Light cyan RGB(224, 255, 255).
    ///     <para>
    ///         A pale shade of cyan.
    ///     </para>
    /// </summary>
    LightCyan = 0xE0FFFF,

    /// <summary>
    ///     Light goldenrod yellow RGB(250, 250, 210).
    ///     <para>
    ///         A pale yellow color with a hint of gold.
    ///     </para>
    /// </summary>
    LightGoldenrodYellow = 0xFAFAD2,

    /// <summary>
    ///     Light gray RGB(211, 211, 211).
    ///     <para>
    ///         A pale shade of gray.
    ///     </para>
    /// </summary>
    LightGray = 0xD3D3D3,

    /// <summary>
    ///     Light green RGB(144, 238, 144).
    ///     <para>
    ///         A pale shade of green.
    ///     </para>
    /// </summary>
    LightGreen = 0x90EE90,

    /// <summary>
    ///     Light grey RGB(211, 211, 211).
    ///     <para>
    ///         Same as <see cref="LightGray"/>.
    ///     </para>
    /// </summary>
    LightGrey = LightGray,

    /// <summary>
    ///     Light pink RGB(255, 182, 193).
    ///     <para>
    ///         A pale shade of pink.
    ///     </para>
    /// </summary>
    LightPink = 0xFFB6C1,

    /// <summary>
    ///     Light salmon RGB(255, 160, 122).
    ///     <para>
    ///         A pale pinkish-orange color.
    ///     </para>
    /// </summary>
    LightSalmon = 0xFFA07A,

    /// <summary>
    ///     Light sea green RGB(32, 178, 170).
    ///     <para>
    ///         A pale shade of sea green.
    ///     </para>
    /// </summary>
    LightSeaGreen = 0x20B2AA,

    /// <summary>
    ///     Light sky blue RGB(135, 206, 250).
    ///     <para>
    ///         A pale shade of sky blue.
    ///     </para>
    /// </summary>
    LightSkyBlue = 0x87CEFA,

    /// <summary>
    ///     Light slate gray RGB(119, 136, 153).
    ///     <para>
    ///         A pale shade of slate gray.
    ///     </para>
    /// </summary>
    LightSlateGray = 0x778899,

    /// <summary>
    ///     Light slate grey RGB(119, 136, 153).
    ///     <para>
    ///         Same as <see cref="LightSlateGray"/>.
    ///     </para>
    /// </summary>
    LightSlateGrey = LightSlateGray,

    /// <summary>
    ///     Light steel blue RGB(176, 196, 222).
    ///     <para>
    ///         A pale shade of steel blue.
    ///     </para>
    /// </summary>
    LightSteelBlue = 0xB0C4DE,

    /// <summary>
    ///     Light yellow RGB(255, 255, 224).
    ///     <para>
    ///         A pale shade of yellow.
    ///     </para>
    /// </summary>
    LightYellow = 0xFFFFE0,

    /// <summary>
    ///     Lime RGB(0, 255, 0).
    ///     <para>
    ///         A bright green color.
    ///     </para>
    /// </summary>
    Lime = 0x00FF00,

    /// <summary>
    ///     Lime green RGB(50, 205, 50).
    ///     <para>
    ///         A bright green color with a hint of yellow.
    ///     </para>
    /// </summary>
    LimeGreen = 0x32CD32,

    /// <summary>
    ///     Linen RGB(250, 240, 230).
    ///     <para>
    ///         A pale beige color, resembling linen fabric.
    ///     </para>
    /// </summary>
    Linen = 0xFAF0E6,

    /// <summary>
    ///     Magenta RGB(255, 0, 255).
    ///     <para>
    ///         A bright and vibrant pinkish-purple color.
    ///     </para>
    /// </summary>
    Magenta = 0xFF00FF,

    /// <summary>
    ///     Maroon RGB(128, 0, 0).
    ///     <para>
    ///         A dark reddish-brown color.
    ///     </para>
    /// </summary>
    Maroon = 0x800000,

    /// <summary>
    ///     Medium aqua marine RGB(102, 205, 170).
    ///     <para>
    ///         A medium shade of greenish-blue.
    ///     </para>
    /// </summary>
    MediumAquaMarine = 0x66CDAA,

    /// <summary>
    ///     Medium blue RGB(0, 0, 205).
    ///     <para>
    ///         A medium shade of blue.
    ///     </para>
    /// </summary>
    MediumBlue = 0x0000CD,

    /// <summary>
    ///     Medium orchid RGB(186, 85, 211).
    ///     <para>
    ///         A medium shade of purple with a hint of pink.
    ///     </para>
    /// </summary>
    MediumOrchid = 0xBA55D3,

    /// <summary>
    ///     Medium purple RGB(147, 112, 219).
    ///     <para>
    ///         A medium shade of purple.
    ///     </para>
    /// </summary>
    MediumPurple = 0x9370DB,

    /// <summary>
    ///     Medium sea green RGB(60, 179, 113).
    ///     <para>
    ///         A medium shade of sea green.
    ///     </para>
    /// </summary>
    MediumSeaGreen = 0x3CB371,

    /// <summary>
    ///     Medium slate blue RGB(123, 104, 238).
    ///     <para>
    ///         A medium shade of slate blue.
    ///     </para>
    /// </summary>
    MediumSlateBlue = 0x7B68EE,

    /// <summary>
    ///     Medium spring green RGB(0, 250, 154).
    ///     <para>
    ///         A medium shade of spring green.
    ///     </para>
    /// </summary>
    MediumSpringGreen = 0x00FA9A,

    /// <summary>
    ///     Medium turquoise RGB(72, 209, 204).
    ///     <para>
    ///         A medium shade of turquoise.
    ///     </para>
    /// </summary>
    MediumTurquoise = 0x48D1CC,

    /// <summary>
    ///     Medium violet red RGB(199, 21, 133).
    ///     <para>
    ///         A medium shade of violet-red.
    ///     </para>
    /// </summary>
    MediumVioletRed = 0xC71585,

    /// <summary>
    ///     Midnight blue RGB(25, 25, 112).
    ///     <para>
    ///         A very dark shade of blue.
    ///     </para>
    /// </summary>
    MidnightBlue = 0x191970,

    /// <summary>
    ///     Mint cream RGB(245, 255, 250).
    ///     <para>
    ///         A very pale greenish-white color.
    ///     </para>
    /// </summary>
    MintCream = 0xF5FFFA,

    /// <summary>
    ///     Misty rose RGB(255, 228, 225).
    ///     <para>
    ///         A very pale pinkish-white color.
    ///     </para>
    /// </summary>
    MistyRose = 0xFFE4E1,

    /// <summary>
    ///     Moccasin RGB(255, 228, 181).
    ///     <para>
    ///         A pale orange color, resembling moccasin leather.
    ///     </para>
    /// </summary>
    Moccasin = 0xFFE4B5,

    /// <summary>
    ///     Navajo white RGB(255, 222, 173).
    ///     <para>
    ///         A pale orange color, resembling Navajo pottery.
    ///     </para>
    /// </summary>
    NavajoWhite = 0xFFDEAD,

    /// <summary>
    ///     Navy RGB(0, 0, 128).
    ///     <para>
    ///         A very dark shade of blue.
    ///     </para>
    /// </summary>
    Navy = 0x000080,

    /// <summary>
    ///     Old lace RGB(253, 245, 230).
    ///     <para>
    ///         A very pale beige color, resembling old lace fabric.
    ///     </para>
    /// </summary>
    OldLace = 0xFDF5E6,

    /// <summary>
    ///     Olive RGB(128, 128, 0).
    ///     <para>
    ///         A dark yellowish-green color.
    ///     </para>
    /// </summary>
    Olive = 0x808000,

    /// <summary>
    ///     Olive drab RGB(107, 142, 35).
    ///     <para>
    ///         A dark yellowish-green color, resembling olive drab fabric.
    ///     </para>
    /// </summary>
    OliveDrab = 0x6B8E23,

    /// <summary>
    ///     Onyx RGB(53, 56, 57).
    ///     <para>
    ///         A dark grayish-black color, resembling the onyx gemstone.
    ///     </para>
    /// </summary>
    Onyx = 0x353839,

    /// <summary>
    ///     Orange RGB(255, 165, 0).
    ///     <para>
    ///         A bright orange color.
    ///     </para>
    /// </summary>
    Orange = 0xFFA500,

    /// <summary>
    ///     Orange red RGB(255, 69, 0).
    ///     <para>
    ///         A bright reddish-orange color.
    ///     </para>
    /// </summary>
    OrangeRed = 0xFF4500,

    /// <summary>
    ///     Orchid RGB(218, 112, 214).
    ///     <para>
    ///         A pale purple color with a hint of pink.
    ///     </para>
    /// </summary>
    Orchid = 0xDA70D6,

    /// <summary>
    ///     Outer space RGB(65, 74, 76).
    ///     <para>
    ///         A dark gray color with a bluish tint, resembling the color of outer space.
    ///     </para>
    /// </summary>
    OuterSpace = 0x414A4C,

    /// <summary>
    ///     Pale goldenrod RGB(238, 232, 170).
    ///     <para>
    ///         A pale yellow color with a hint of gold.
    ///     </para>
    /// </summary>
    PaleGoldenrod = 0xEEE8AA,

    /// <summary>
    ///     Pale green RGB(152, 251, 152).
    ///     <para>
    ///         A pale shade of green.
    ///     </para>
    /// </summary>
    PaleGreen = 0x98FB98,

    /// <summary>
    ///     Pale turquoise RGB(175, 238, 238).
    ///     <para>
    ///         A pale shade of turquoise.
    ///     </para>
    /// </summary>
    PaleTurquoise = 0xAFEEEE,

    /// <summary>
    ///     Pale violet red RGB(219, 112, 147).
    ///     <para>
    ///         A pale shade of violet-red.
    ///     </para>
    /// </summary>
    PaleVioletRed = 0xDB7093,

    /// <summary>
    ///     Papaya whip RGB(255, 239, 213).
    ///     <para>
    ///         A pale orange color, resembling papaya fruit.
    ///     </para>
    /// </summary>
    PapayaWhip = 0xFFEFD5,

    /// <summary>
    ///     Peach puff RGB(255, 218, 185).
    ///     <para>
    ///         A pale orange color, resembling peach skin.
    ///     </para>
    /// </summary>
    PeachPuff = 0xFFDAB9,

    /// <summary>
    ///     Peru RGB(205, 133, 63).
    ///     <para>
    ///         A dark orange-brown color.
    ///     </para>
    /// </summary>
    Peru = 0xCD853F,

    /// <summary>
    ///     Pink RGB(255, 192, 203).
    ///     <para>
    ///         A pale shade of pink.
    ///     </para>
    /// </summary>
    Pink = 0xFFC0CB,

    /// <summary>
    ///     Plum RGB(221, 160, 221).
    ///     <para>
    ///         A pale purple color.
    ///     </para>
    /// </summary>
    Plum = 0xDDA0DD,

    /// <summary>
    ///     Powder blue RGB(176, 224, 230).
    ///     <para>
    ///         A pale shade of blue.
    ///     </para>
    /// </summary>
    PowderBlue = 0xB0E0E6,

    /// <summary>
    ///     Purple RGB(128, 0, 128).
    ///     <para>
    ///         A dark shade of purple.
    ///     </para>
    /// </summary>
    Purple = 0x800080,

    /// <summary>
    ///     Raisin black RGB(36, 33, 36).
    ///     <para>
    ///         A very dark grayish-black color, resembling the color of raisins.
    ///     </para>
    /// </summary>
    RaisinBlack = 0x242124,

    /// <summary>
    ///     Rebecca purple RGB(102, 51, 153).
    ///     <para>
    ///         A medium-dark shade of purple.
    ///     </para>
    /// </summary>
    RebeccaPurple = 0x663399,

    /// <summary>
    ///     Red RGB(255, 0, 0).
    ///     <para>
    ///         A bright red color.
    ///     </para>
    /// </summary>
    Red = 0xFF0000,

    /// <summary>
    ///     Rosy brown RGB(188, 143, 143).
    ///     <para>
    ///         A pale reddish-brown color.
    ///     </para>
    /// </summary>
    RosyBrown = 0xBC8F8F,

    /// <summary>
    ///     Royal blue RGB(65, 105, 225).
    ///     <para>
    ///         A medium shade of blue.
    ///     </para>
    /// </summary>
    RoyalBlue = 0x4169E1,

    /// <summary>
    ///     Saddle brown RGB(139, 69, 19).
    ///     <para>
    ///         A dark reddish-brown color.
    ///     </para>
    /// </summary>
    SaddleBrown = 0x8B4513,

    /// <summary>
    ///     Salmon RGB(250, 128, 114).
    ///     <para>
    ///         A pale pinkish-orange color.
    ///     </para>
    /// </summary>
    Salmon = 0xFA8072,

    /// <summary>
    ///     Sandy brown RGB(244, 164, 96).
    ///     <para>
    ///         A pale orange-brown color.
    ///     </para>
    /// </summary>
    SandyBrown = 0xF4A460,

    /// <summary>
    ///     Sea green RGB(46, 139, 87).
    ///     <para>
    ///         A medium-dark shade of green.
    ///     </para>
    /// </summary>
    SeaGreen = 0x2E8B57,

    /// <summary>
    ///     Sea shell RGB(255, 245, 238).
    ///     <para>
    ///         A very pale orange color, resembling seashells.
    ///     </para>
    /// </summary>
    SeaShell = 0xFFF5EE,

    /// <summary>
    ///     Sienna RGB(160, 82, 45).
    ///     <para>
    ///         A dark orange-brown color.
    ///     </para>
    /// </summary>
    Sienna = 0xA0522D,

    /// <summary>
    ///     Silver RGB(192, 192, 192).
    ///     <para>
    ///         A pale gray color, resembling silver.
    ///     </para>
    /// </summary>
    Silver = 0xC0C0C0,

    /// <summary>
    ///     Sky blue RGB(135, 206, 235).
    ///     <para>
    ///         A pale shade of blue, resembling the sky.
    ///     </para>
    /// </summary>
    SkyBlue = 0x87CEEB,

    /// <summary>
    ///     Slate blue RGB(106, 90, 205).
    ///     <para>
    ///         A medium-dark shade of blue.
    ///     </para>
    /// </summary>
    SlateBlue = 0x6A5ACD,

    /// <summary>
    ///     Slate gray RGB(112, 128, 144).
    ///     <para>
    ///         A medium-dark shade of gray.
    ///     </para>
    /// </summary>
    SlateGray = 0x708090,

    /// <summary>
    ///     Slate grey RGB(112, 128, 144).
    ///     <para>
    ///         Same as <see cref="SlateGray"/>.
    ///     </para>
    /// </summary>
    SlateGrey = SlateGray,

    /// <summary>
    ///     Snow RGB(255, 250, 250).
    ///     <para>
    ///         A very pale shade of white with a hint of pink.
    ///     </para>
    /// </summary>
    Snow = 0xFFFAFA,

    /// <summary>
    ///     Spring green RGB(0, 255, 127).
    ///     <para>
    ///         A bright green color with a hint of yellow.
    ///     </para>
    /// </summary>
    SpringGreen = 0x00FF7F,

    /// <summary>
    ///     Steel blue RGB(70, 130, 180).
    ///     <para>
    ///         A medium-dark shade of blue.
    ///     </para>
    /// </summary>
    SteelBlue = 0x4682B4,

    /// <summary>
    ///     Tan RGB(210, 180, 140).
    ///     <para>
    ///         A pale orange-brown color.
    ///     </para>
    /// </summary>
    Tan = 0xD2B48C,

    /// <summary>
    ///     Teal RGB(0, 128, 128).
    ///     <para>
    ///         A medium-dark shade of greenish-blue.
    ///     </para>
    /// </summary>
    Teal = 0x008080,

    /// <summary>
    ///     Thistle RGB(216, 191, 216).
    ///     <para>
    ///         A pale purple color.
    ///     </para>
    /// </summary>
    Thistle = 0xD8BFD8,

    /// <summary>
    ///     Tomato RGB(255, 99, 71).
    ///     <para>
    ///         A bright reddish-orange color, resembling tomatoes.
    ///     </para>
    /// </summary>
    Tomato = 0xFF6347,

    /// <summary>
    ///     Turquoise RGB(64, 224, 208).
    ///     <para>
    ///         A bright greenish-blue color.
    ///     </para>
    /// </summary>
    Turquoise = 0x40E0D0,

    /// <summary>
    ///     Violet RGB(238, 130, 238).
    ///     <para>
    ///         A bright purple color.
    ///     </para>
    /// </summary>
    Violet = 0xEE82EE,

    /// <summary>
    ///     Wheat RGB(245, 222, 179).
    ///     <para>
    ///         A pale yellowish-brown color, resembling wheat.
    ///     </para>
    /// </summary>
    Wheat = 0xF5DEB3,

    /// <summary>
    ///     White RGB(255, 255, 255).
    ///     <para>
    ///         The lightest color, representing the presence of all colors of light.
    ///     </para>
    /// </summary>
    White = 0xFFFFFF,

    /// <summary>
    ///     White smoke RGB(245, 245, 245).
    ///     <para>
    ///         A very pale shade of gray, resembling smoke.
    ///     </para>
    /// </summary>
    WhiteSmoke = 0xF5F5F5,

    /// <summary>
    ///     Yellow RGB(255, 255, 0).
    ///     <para>
    ///         A bright yellow color.
    ///     </para>
    /// </summary>
    Yellow = 0xFFFF00,

    /// <summary>
    ///     Yellow green RGB(154, 205, 50).
    ///     <para>
    ///         A bright yellowish-green color.
    ///     </para>
    /// </summary>
    YellowGreen = 0x9ACD32,

    // Legacy 16-color names for backwards compatibility
    // These match the RGB values used in ColorName16 mapping

    /// <summary>
    ///     Bright Black RGB(118, 118, 118).
    ///     <para>
    ///         A bright black (dark gray) color from the legacy 16-color palette. This is the ColorName16.DarkGray color.
    ///     </para>
    /// </summary>
    BrightBlack = 0x767676,

    /// <summary>
    ///     Bright Blue RGB(59, 120, 255).
    ///     <para>
    ///         A bright blue color from the legacy 16-color palette.
    ///     </para>
    /// </summary>
    BrightBlue = 0x3B78FF,

    /// <summary>
    ///     Bright Cyan RGB(97, 214, 214).
    ///     <para>
    ///         A bright cyan color from the legacy 16-color palette.
    ///     </para>
    /// </summary>
    BrightCyan = 0x61D6D6,

    /// <summary>
    ///     Bright Green RGB(22, 198, 12).
    ///     <para>
    ///         A bright green color from the legacy 16-color palette.
    ///     </para>
    /// </summary>
    BrightGreen = 0x16C60C,

    /// <summary>
    ///     Bright Magenta RGB(180, 0, 158).
    ///     <para>
    ///         A bright magenta color from the legacy 16-color palette.
    ///     </para>
    /// </summary>
    BrightMagenta = 0xB4009E,

    /// <summary>
    ///     Bright Red RGB(231, 72, 86).
    ///     <para>
    ///         A bright red color from the legacy 16-color palette.
    ///     </para>
    /// </summary>
    BrightRed = 0xE74856,

    /// <summary>
    ///     Bright Yellow RGB(249, 241, 165).
    ///     <para>
    ///         A bright yellow color from the legacy 16-color palette.
    ///     </para>
    /// </summary>
    BrightYellow = 0xF9F1A5
}
