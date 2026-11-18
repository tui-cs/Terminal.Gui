namespace Terminal.Gui.Drawing;

/// <summary>The type of Rune that we will use before considering double width, curved borders etc</summary>
internal enum IntersectionRuneType
{
    None,
    Dot,
    ULCorner,
    URCorner,
    LLCorner,
    LRCorner,
    TopTee,
    BottomTee,
    RightTee,
    LeftTee,
    Cross,
    HLine,
    VLine
}
