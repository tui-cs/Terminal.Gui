//------------------------------------------------------------------------------
// SpinnerStyles below are derived from
// <https://github.com/sindresorhus/cli-spinners/blob/master/spinners.json>
// MIT License
// Copyright (c) Sindre Sorhus <sindresorhus@gmail.com>
// (https://sindresorhus.com)
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//------------------------------------------------------------------------------
// Windows Terminal supports Unicode and Emoji characters, but by default
// conhost shells (e.g., PowerShell and cmd.exe) do not. See
// <https://spectreconsole.net/best-practices>.
//------------------------------------------------------------------------------

#pragma warning disable CA1034 // Nested types should not be visible

namespace Terminal.Gui.Views;

/// <summary>SpinnerStyles used in a <see cref="SpinnerView"/>.</summary>
public abstract class SpinnerStyle
{
    private const bool DEFAULT_BOUNCE = false;
    private const int DEFAULT_DELAY = 80;
    private const bool DEFAULT_SPECIAL = false;

    /// <summary>Gets whether the current spinner style contains emoji or other special characters.</summary>
    public abstract bool HasSpecialCharacters { get; }

    /// <summary>Gets or sets the frames used to animate the spinner.</summary>
    public abstract string [] Sequence { get; }

    /// <summary>
    ///     Gets or sets whether spinner should go back and forth through the Sequence rather than going to the end and
    ///     starting again at the beginning.
    /// </summary>
    public abstract bool SpinBounce { get; }

    /// <summary>
    ///     Gets or sets the number of milliseconds to wait between characters in the spin.  Defaults to the
    ///     SpinnerStyle's Interval value.
    /// </summary>
    /// <remarks>
    ///     This is the maximum speed the spinner will rotate at.  You still need to call
    ///     <see cref="View.SetNeedsDraw()"/> or <see cref="SpinnerView.AutoSpin"/> to advance/start animation.
    /// </remarks>
    public abstract int SpinDelay { get; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    // Placeholder when user has specified Delay and Sequence manually
    public class Custom : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => Array.Empty<string> ();
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class Dots : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class Dots2 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "⣾", "⣽", "⣻", "⢿", "⡿", "⣟", "⣯", "⣷" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class Dots3 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "⠋", "⠙", "⠚", "⠞", "⠖", "⠦", "⠴", "⠲", "⠳", "⠓" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class Dots4 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "⠄", "⠆", "⠇", "⠋", "⠙", "⠸", "⠰", "⠠" };
        public override bool SpinBounce => true;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class Dots5 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;

        public override string [] Sequence => new []
        {
            "⠋", "⠙", "⠚", "⠒", "⠂", "⠂", "⠒", "⠲", "⠴", "⠦", "⠖", "⠒", "⠐", "⠐", "⠒", "⠓", "⠋"
        };

        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class Dots6 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;

        public override string [] Sequence => new []
        {
            "⠁", "⠁", "⠉", "⠙", "⠚", "⠒", "⠂", "⠂", "⠒", "⠲", "⠴", "⠤", "⠄", "⠄"
        };

        public override bool SpinBounce => true;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class Dots7 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;

        public override string [] Sequence => new []
        {
            "⠈", "⠈", "⠉", "⠋", "⠓", "⠒", "⠐", "⠐", "⠒", "⠖", "⠦", "⠤", "⠠", "⠠"
        };

        public override bool SpinBounce => true;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class Dots8 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;

        public override string [] Sequence => new []
        {
            "⠁",
            "⠁",
            "⠉",
            "⠙",
            "⠚",
            "⠒",
            "⠂",
            "⠂",
            "⠒",
            "⠲",
            "⠴",
            "⠤",
            "⠄",
            "⠄",
            "⠤",
            "⠠",
            "⠠",
            "⠤",
            "⠦",
            "⠖",
            "⠒",
            "⠐",
            "⠐",
            "⠒",
            "⠓",
            "⠋",
            "⠉",
            "⠈",
            "⠈"
        };

        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class Dots9 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "⢹", "⢺", "⢼", "⣸", "⣇", "⡧", "⡗", "⡏" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class Dots10 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "⢄", "⢂", "⢁", "⡁", "⡈", "⡐", "⡠" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class Dots11 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "⠁", "⠂", "⠄", "⡀", "⢀", "⠠", "⠐", "⠈" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 100;
    }

    public class Dots12 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;

        public override string [] Sequence => new []
        {
            "⢀⠀",
            "⡀⠀",
            "⠄⠀",
            "⢂⠀",
            "⡂⠀",
            "⠅⠀",
            "⢃⠀",
            "⡃⠀",
            "⠍⠀",
            "⢋⠀",
            "⡋⠀",
            "⠍⠁",
            "⢋⠁",
            "⡋⠁",
            "⠍⠉",
            "⠋⠉",
            "⠋⠉",
            "⠉⠙",
            "⠉⠙",
            "⠉⠩",
            "⠈⢙",
            "⠈⡙",
            "⢈⠩",
            "⡀⢙",
            "⠄⡙",
            "⢂⠩",
            "⡂⢘",
            "⠅⡘",
            "⢃⠨",
            "⡃⢐",
            "⠍⡐",
            "⢋⠠",
            "⡋⢀",
            "⠍⡁",
            "⢋⠁",
            "⡋⠁",
            "⠍⠉",
            "⠋⠉",
            "⠋⠉",
            "⠉⠙",
            "⠉⠙",
            "⠉⠩",
            "⠈⢙",
            "⠈⡙",
            "⠈⠩",
            "⠀⢙",
            "⠀⡙",
            "⠀⠩",
            "⠀⢘",
            "⠀⡘",
            "⠀⠨",
            "⠀⢐",
            "⠀⡐",
            "⠀⠠",
            "⠀⢀",
            "⠀⡀"
        };

        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class Dots8Bit : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;

        public override string [] Sequence => new []
        {
            "⠀",
            "⠁",
            "⠂",
            "⠃",
            "⠄",
            "⠅",
            "⠆",
            "⠇",
            "⡀",
            "⡁",
            "⡂",
            "⡃",
            "⡄",
            "⡅",
            "⡆",
            "⡇",
            "⠈",
            "⠉",
            "⠊",
            "⠋",
            "⠌",
            "⠍",
            "⠎",
            "⠏",
            "⡈",
            "⡉",
            "⡊",
            "⡋",
            "⡌",
            "⡍",
            "⡎",
            "⡏",
            "⠐",
            "⠑",
            "⠒",
            "⠓",
            "⠔",
            "⠕",
            "⠖",
            "⠗",
            "⡐",
            "⡑",
            "⡒",
            "⡓",
            "⡔",
            "⡕",
            "⡖",
            "⡗",
            "⠘",
            "⠙",
            "⠚",
            "⠛",
            "⠜",
            "⠝",
            "⠞",
            "⠟",
            "⡘",
            "⡙",
            "⡚",
            "⡛",
            "⡜",
            "⡝",
            "⡞",
            "⡟",
            "⠠",
            "⠡",
            "⠢",
            "⠣",
            "⠤",
            "⠥",
            "⠦",
            "⠧",
            "⡠",
            "⡡",
            "⡢",
            "⡣",
            "⡤",
            "⡥",
            "⡦",
            "⡧",
            "⠨",
            "⠩",
            "⠪",
            "⠫",
            "⠬",
            "⠭",
            "⠮",
            "⠯",
            "⡨",
            "⡩",
            "⡪",
            "⡫",
            "⡬",
            "⡭",
            "⡮",
            "⡯",
            "⠰",
            "⠱",
            "⠲",
            "⠳",
            "⠴",
            "⠵",
            "⠶",
            "⠷",
            "⡰",
            "⡱",
            "⡲",
            "⡳",
            "⡴",
            "⡵",
            "⡶",
            "⡷",
            "⠸",
            "⠹",
            "⠺",
            "⠻",
            "⠼",
            "⠽",
            "⠾",
            "⠿",
            "⡸",
            "⡹",
            "⡺",
            "⡻",
            "⡼",
            "⡽",
            "⡾",
            "⡿",
            "⢀",
            "⢁",
            "⢂",
            "⢃",
            "⢄",
            "⢅",
            "⢆",
            "⢇",
            "⣀",
            "⣁",
            "⣂",
            "⣃",
            "⣄",
            "⣅",
            "⣆",
            "⣇",
            "⢈",
            "⢉",
            "⢊",
            "⢋",
            "⢌",
            "⢍",
            "⢎",
            "⢏",
            "⣈",
            "⣉",
            "⣊",
            "⣋",
            "⣌",
            "⣍",
            "⣎",
            "⣏",
            "⢐",
            "⢑",
            "⢒",
            "⢓",
            "⢔",
            "⢕",
            "⢖",
            "⢗",
            "⣐",
            "⣑",
            "⣒",
            "⣓",
            "⣔",
            "⣕",
            "⣖",
            "⣗",
            "⢘",
            "⢙",
            "⢚",
            "⢛",
            "⢜",
            "⢝",
            "⢞",
            "⢟",
            "⣘",
            "⣙",
            "⣚",
            "⣛",
            "⣜",
            "⣝",
            "⣞",
            "⣟",
            "⢠",
            "⢡",
            "⢢",
            "⢣",
            "⢤",
            "⢥",
            "⢦",
            "⢧",
            "⣠",
            "⣡",
            "⣢",
            "⣣",
            "⣤",
            "⣥",
            "⣦",
            "⣧",
            "⢨",
            "⢩",
            "⢪",
            "⢫",
            "⢬",
            "⢭",
            "⢮",
            "⢯",
            "⣨",
            "⣩",
            "⣪",
            "⣫",
            "⣬",
            "⣭",
            "⣮",
            "⣯",
            "⢰",
            "⢱",
            "⢲",
            "⢳",
            "⢴",
            "⢵",
            "⢶",
            "⢷",
            "⣰",
            "⣱",
            "⣲",
            "⣳",
            "⣴",
            "⣵",
            "⣶",
            "⣷",
            "⢸",
            "⢹",
            "⢺",
            "⢻",
            "⢼",
            "⢽",
            "⢾",
            "⢿",
            "⣸",
            "⣹",
            "⣺",
            "⣻",
            "⣼",
            "⣽",
            "⣾",
            "⣿"
        };

        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class Line : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "-", @"\", "|", "/" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 130;
    }

    public class Line2 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "⠂", "-", "–", "—" };
        public override bool SpinBounce => true;
        public override int SpinDelay => 100;
    }

    public class Pipe : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "┤", "┘", "┴", "└", "├", "┌", "┬", "┐" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 100;
    }

    public class SimpleDots : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { ".  ", ".. ", "...", "   " };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 400;
    }

    public class SimpleDotsScrolling : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { ".  ", ".. ", "...", " ..", "  .", "   " };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 200;
    }

    public class Star : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "✶", "✸", "✹", "✺", "✹", "✷" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 70;
    }

    public class Star2 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "+", "x", "*" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class Flip : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "_", "_", "_", "-", "`", "`", "'", "´", "-", "_", "_", "_" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 70;
    }

    public class Hamburger : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "☱", "☲", "☴" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 100;
    }

    public class GrowVertical : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "▁", "▃", "▄", "▅", "▆", "▇" };
        public override bool SpinBounce => true;
        public override int SpinDelay => 120;
    }

    public class GrowHorizontal : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "▏", "▎", "▍", "▌", "▋", "▊", "▉" };
        public override bool SpinBounce => true;
        public override int SpinDelay => 120;
    }

    public class Balloon : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { " ", ".", "o", "O", "@", "*", " " };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 140;
    }

    public class Balloon2 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { ".", ".", "o", "O", "°" };
        public override bool SpinBounce => true;
        public override int SpinDelay => 120;
    }

    public class Noise : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "▓", "▒", "░" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 100;
    }

    public class Bounce : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "⠁", "⠂", "⠄" };
        public override bool SpinBounce => true;
        public override int SpinDelay => 120;
    }

    public class BoxBounce : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "▖", "▘", "▝", "▗" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 120;
    }

    public class BoxBounce2 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "▌", "▀", "▐", "▄" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 100;
    }

    public class Triangle : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "◢", "◣", "◤", "◥" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 50;
    }

    public class Arc : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "◜", "◠", "◝", "◞", "◡", "◟" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 100;
    }

    public class Circle : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "◡", "⊙", "◠" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 120;
    }

    public class SquareCorners : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "◰", "◳", "◲", "◱" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 180;
    }

    public class CircleQuarters : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "◴", "◷", "◶", "◵" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 120;
    }

    public class CircleHalves : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "◐", "◓", "◑", "◒" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 50;
    }

    public class Squish : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "╫", "╪" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 100;
    }

    public class Toggle : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "⊶", "⊷" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 250;
    }

    public class Toggle2 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "▫", "▪" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class Toggle3 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "□", "■" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 120;
    }

    public class Toggle4 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "■", "□", "▪", "▫" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 100;
    }

    public class Toggle5 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "▮", "▯" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 100;
    }

    public class Toggle6 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "ဝ", "၀" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 300;
    }

    public class Toggle7 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "⦾", "⦿" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class Toggle8 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "◍", "◌" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 100;
    }

    public class Toggle9 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "◉", "◎" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 100;
    }

    public class Toggle10 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "㊂", "㊀", "㊁" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 100;
    }

    public class Toggle11 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "⧇", "⧆" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 50;
    }

    public class Toggle12 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "☗", "☖" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 120;
    }

    public class Toggle13 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "=", "*", "-" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class Arrow : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "←", "↖", "↑", "↗", "→", "↘", "↓", "↙" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 100;
    }

    public class Arrow2 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => true;
        public override string [] Sequence => new [] { "⬆️ ", "↗️ ", "➡️ ", "↘️ ", "⬇️ ", "↙️ ", "⬅️ ", "↖️ " };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class Arrow3 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "▹▹▹▹▹", "▸▹▹▹▹", "▹▸▹▹▹", "▹▹▸▹▹", "▹▹▹▸▹", "▹▹▹▹▸" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 120;
    }

    public class BouncingBar : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;

        public override string [] Sequence => new []
        {
            "[    ]", "[=   ]", "[==  ]", "[=== ]", "[ ===]", "[  ==]", "[   =]", "[    ]"
        };

        public override bool SpinBounce => true;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class BouncingBall : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;

        public override string [] Sequence => new []
        {
            "(●     )", "( ●    )", "(  ●   )", "(   ●  )", "(    ● )", "(     ●)"
        };

        public override bool SpinBounce => true;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class Smiley : SpinnerStyle
    {
        public override bool HasSpecialCharacters => true;
        public override string [] Sequence => new [] { "😄 ", "😝 " };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 200;
    }

    public class Monkey : SpinnerStyle
    {
        public override bool HasSpecialCharacters => true;
        public override string [] Sequence => new [] { "🙈 ", "🙈 ", "🙉 ", "🙊 " };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 300;
    }

    public class Hearts : SpinnerStyle
    {
        public override bool HasSpecialCharacters => true;
        public override string [] Sequence => new [] { "💛 ", "💙 ", "💜 ", "💚 ", "❤️ " };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 100;
    }

    public class Clock : SpinnerStyle
    {
        public override bool HasSpecialCharacters => true;

        public override string [] Sequence => new []
        {
            "🕛 ", "🕐 ", "🕑 ", "🕒 ", "🕓 ", "🕔 ", "🕕 ", "🕖 ", "🕗 ", "🕘 ", "🕙 ", "🕚 "
        };

        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 100;
    }

    public class Earth : SpinnerStyle
    {
        public override bool HasSpecialCharacters => true;
        public override string [] Sequence => new [] { "🌍 ", "🌎 ", "🌏 " };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 180;
    }

    public class Material : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;

        public override string [] Sequence => new []
        {
            "█▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁",
            "██▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁",
            "███▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁",
            "████▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁",
            "██████▁▁▁▁▁▁▁▁▁▁▁▁▁▁",
            "██████▁▁▁▁▁▁▁▁▁▁▁▁▁▁",
            "███████▁▁▁▁▁▁▁▁▁▁▁▁▁",
            "████████▁▁▁▁▁▁▁▁▁▁▁▁",
            "█████████▁▁▁▁▁▁▁▁▁▁▁",
            "█████████▁▁▁▁▁▁▁▁▁▁▁",
            "██████████▁▁▁▁▁▁▁▁▁▁",
            "███████████▁▁▁▁▁▁▁▁▁",
            "█████████████▁▁▁▁▁▁▁",
            "██████████████▁▁▁▁▁▁",
            "██████████████▁▁▁▁▁▁",
            "▁██████████████▁▁▁▁▁",
            "▁██████████████▁▁▁▁▁",
            "▁██████████████▁▁▁▁▁",
            "▁▁██████████████▁▁▁▁",
            "▁▁▁██████████████▁▁▁",
            "▁▁▁▁█████████████▁▁▁",
            "▁▁▁▁██████████████▁▁",
            "▁▁▁▁██████████████▁▁",
            "▁▁▁▁▁██████████████▁",
            "▁▁▁▁▁██████████████▁",
            "▁▁▁▁▁██████████████▁",
            "▁▁▁▁▁▁██████████████",
            "▁▁▁▁▁▁██████████████",
            "▁▁▁▁▁▁▁█████████████",
            "▁▁▁▁▁▁▁█████████████",
            "▁▁▁▁▁▁▁▁████████████",
            "▁▁▁▁▁▁▁▁████████████",
            "▁▁▁▁▁▁▁▁▁███████████",
            "▁▁▁▁▁▁▁▁▁███████████",
            "▁▁▁▁▁▁▁▁▁▁██████████",
            "▁▁▁▁▁▁▁▁▁▁██████████",
            "▁▁▁▁▁▁▁▁▁▁▁▁████████",
            "▁▁▁▁▁▁▁▁▁▁▁▁▁███████",
            "▁▁▁▁▁▁▁▁▁▁▁▁▁▁██████",
            "▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁█████",
            "▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁█████",
            "█▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁████",
            "██▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁███",
            "██▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁███",
            "███▁▁▁▁▁▁▁▁▁▁▁▁▁▁███",
            "████▁▁▁▁▁▁▁▁▁▁▁▁▁▁██",
            "█████▁▁▁▁▁▁▁▁▁▁▁▁▁▁█",
            "█████▁▁▁▁▁▁▁▁▁▁▁▁▁▁█",
            "██████▁▁▁▁▁▁▁▁▁▁▁▁▁█",
            "████████▁▁▁▁▁▁▁▁▁▁▁▁",
            "█████████▁▁▁▁▁▁▁▁▁▁▁",
            "█████████▁▁▁▁▁▁▁▁▁▁▁",
            "█████████▁▁▁▁▁▁▁▁▁▁▁",
            "█████████▁▁▁▁▁▁▁▁▁▁▁",
            "███████████▁▁▁▁▁▁▁▁▁",
            "████████████▁▁▁▁▁▁▁▁",
            "████████████▁▁▁▁▁▁▁▁",
            "██████████████▁▁▁▁▁▁",
            "██████████████▁▁▁▁▁▁",
            "▁██████████████▁▁▁▁▁",
            "▁██████████████▁▁▁▁▁",
            "▁▁▁█████████████▁▁▁▁",
            "▁▁▁▁▁████████████▁▁▁",
            "▁▁▁▁▁████████████▁▁▁",
            "▁▁▁▁▁▁███████████▁▁▁",
            "▁▁▁▁▁▁▁▁█████████▁▁▁",
            "▁▁▁▁▁▁▁▁█████████▁▁▁",
            "▁▁▁▁▁▁▁▁▁█████████▁▁",
            "▁▁▁▁▁▁▁▁▁█████████▁▁",
            "▁▁▁▁▁▁▁▁▁▁█████████▁",
            "▁▁▁▁▁▁▁▁▁▁▁████████▁",
            "▁▁▁▁▁▁▁▁▁▁▁████████▁",
            "▁▁▁▁▁▁▁▁▁▁▁▁███████▁",
            "▁▁▁▁▁▁▁▁▁▁▁▁███████▁",
            "▁▁▁▁▁▁▁▁▁▁▁▁▁███████",
            "▁▁▁▁▁▁▁▁▁▁▁▁▁███████",
            "▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁█████",
            "▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁████",
            "▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁████",
            "▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁████",
            "▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁███",
            "▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁███",
            "▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁██",
            "▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁██",
            "▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁██",
            "▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁█",
            "▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁█",
            "▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁█",
            "▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁",
            "▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁",
            "▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁",
            "▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁"
        };

        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 17;
    }

    public class Moon : SpinnerStyle
    {
        public override bool HasSpecialCharacters => true;
        public override string [] Sequence => new [] { "🌑 ", "🌒 ", "🌓 ", "🌔 ", "🌕 ", "🌖 ", "🌗 ", "🌘 " };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class Runner : SpinnerStyle
    {
        public override bool HasSpecialCharacters => true;
        public override string [] Sequence => new [] { "🚶 ", "🏃 " };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 140;
    }

    public class Pong : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;

        public override string [] Sequence => new []
        {
            "▐⠂       ▌",
            "▐⠈       ▌",
            "▐ ⠂      ▌",
            "▐ ⠠      ▌",
            "▐  ⡀     ▌",
            "▐  ⠠     ▌",
            "▐   ⠂    ▌",
            "▐   ⠈    ▌",
            "▐    ⠂   ▌",
            "▐    ⠠   ▌",
            "▐     ⡀  ▌",
            "▐     ⠠  ▌",
            "▐      ⠂ ▌",
            "▐      ⠈ ▌",
            "▐       ⠂▌",
            "▐       ⠠▌",
            "▐       ⡀▌",
            "▐      ⠠ ▌",
            "▐      ⠂ ▌",
            "▐     ⠈  ▌",
            "▐     ⠂  ▌",
            "▐    ⠠   ▌",
            "▐    ⡀   ▌",
            "▐   ⠠    ▌",
            "▐   ⠂    ▌",
            "▐  ⠈     ▌",
            "▐  ⠂     ▌",
            "▐ ⠠      ▌",
            "▐ ⡀      ▌",
            "▐⠠       ▌"
        };

        public override bool SpinBounce => true;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class Shark : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;

        public override string [] Sequence => new []
        {
            @"▐|\____________▌",
            @"▐_|\___________▌",
            @"▐__|\__________▌",
            @"▐___|\_________▌",
            @"▐____|\________▌",
            @"▐_____|\_______▌",
            @"▐______|\______▌",
            @"▐_______|\_____▌",
            @"▐________|\____▌",
            @"▐_________|\___▌",
            @"▐__________|\__▌",
            @"▐___________|\_▌",
            @"▐____________|\▌",
            "▐____________/|▌",
            "▐___________/|_▌",
            "▐__________/|__▌",
            "▐_________/|___▌",
            "▐________/|____▌",
            "▐_______/|_____▌",
            "▐______/|______▌",
            "▐_____/|_______▌",
            "▐____/|________▌",
            "▐___/|_________▌",
            "▐__/|__________▌",
            "▐_/|___________▌",
            "▐/|____________▌"
        };

        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 120;
    }

    public class Dqpb : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "d", "q", "p", "b" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 100;
    }

    public class Weather : SpinnerStyle
    {
        public override bool HasSpecialCharacters => true;

        public override string [] Sequence => new []
        {
            "☀️ ",
            "☀️ ",
            "☀️ ",
            "🌤 ",
            "⛅️ ",
            "🌥 ",
            "☁️ ",
            "🌧 ",
            "🌨 ",
            "🌧 ",
            "🌨 ",
            "🌧 ",
            "🌨 ",
            "⛈ ",
            "🌨 ",
            "🌧 ",
            "🌨 ",
            "☁️ ",
            "🌥 ",
            "⛅️ ",
            "🌤 ",
            "☀️ ",
            "☀️ "
        };

        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 100;
    }

    public class Christmas : SpinnerStyle
    {
        public override bool HasSpecialCharacters => true;
        public override string [] Sequence => new [] { "🌲", "🎄" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 400;
    }

    public class Grenade : SpinnerStyle
    {
        public override bool HasSpecialCharacters => true;

        public override string [] Sequence => new []
        {
            "،   ", "′   ", " ´ ", " ‾ ", "  ⸌", "  ⸊", "  |", "  ⁎", "  ⁕", " ෴ ", "  ⁓", "   ", "   ", "   "
        };

        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class Points : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "∙∙∙", "●∙∙", "∙●∙", "∙∙●", "∙∙∙" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 125;
    }

    public class Layer : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
        public override string [] Sequence => new [] { "-", "=", "≡" };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 150;
    }

    public class BetaWave : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;

        public override string [] Sequence => new []
        {
            "ρββββββ", "βρβββββ", "ββρββββ", "βββρβββ", "ββββρββ", "βββββρβ", "ββββββρ"
        };

        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class FingerDance : SpinnerStyle
    {
        public override bool HasSpecialCharacters => true;
        public override string [] Sequence => new [] { "🤘 ", "🤟 ", "🖖 ", "✋ ", "🤚 ", "👆 " };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 160;
    }

    public class FistBump : SpinnerStyle
    {
        public override bool HasSpecialCharacters => true;

        public override string [] Sequence => new []
        {
            "🤜\u3000\u3000\u3000\u3000🤛 ",
            "🤜\u3000\u3000\u3000\u3000🤛 ",
            "🤜\u3000\u3000\u3000\u3000🤛 ",
            "\u3000🤜\u3000\u3000🤛\u3000 ",
            "\u3000\u3000🤜🤛\u3000\u3000 ",
            "\u3000🤜✨🤛\u3000\u3000 ",
            "🤜\u3000✨\u3000🤛\u3000 "
        };

        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class SoccerHeader : SpinnerStyle
    {
        public override bool HasSpecialCharacters => true;

        public override string [] Sequence => new []
        {
            " 🧑⚽️       🧑 ",
            "🧑  ⚽️      🧑 ",
            "🧑   ⚽️     🧑 ",
            "🧑    ⚽️    🧑 ",
            "🧑     ⚽️   🧑 ",
            "🧑      ⚽️  🧑 ",
            "🧑       ⚽️🧑  "
        };

        public override bool SpinBounce => true;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class MindBlown : SpinnerStyle
    {
        public override bool HasSpecialCharacters => true;

        public override string [] Sequence => new []
        {
            "😐 ", "😐 ", "😮 ", "😮 ", "😦 ", "😦 ", "😧 ", "😧 ", "🤯 ", "💥 ", "✨ ", "\u3000 ", "\u3000 ",
            "\u3000 "
        };

        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 160;
    }

    public class Speaker : SpinnerStyle
    {
        public override bool HasSpecialCharacters => true;
        public override string [] Sequence => new [] { "🔈 ", "🔉 ", "🔊 " };
        public override bool SpinBounce => true;
        public override int SpinDelay => 160;
    }

    public class OrangePulse : SpinnerStyle
    {
        public override bool HasSpecialCharacters => true;
        public override string [] Sequence => new [] { "🔸 ", "🔶 ", "🟠 ", "🟠 ", "🔶 " };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 100;
    }

    public class BluePulse : SpinnerStyle
    {
        public override bool HasSpecialCharacters => true;
        public override string [] Sequence => new [] { "🔹 ", "🔷 ", "🔵 ", "🔵 ", "🔷 " };
        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 100;
    }

    public class OrangeBluePulse : SpinnerStyle
    {
        public override bool HasSpecialCharacters => true;

        public override string [] Sequence => new []
        {
            "🔸 ", "🔶 ", "🟠 ", "🟠 ", "🔶 ", "🔹 ", "🔷 ", "🔵 ", "🔵 ", "🔷 "
        };

        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 100;
    }

    public class TimeTravelClock : SpinnerStyle
    {
        public override bool HasSpecialCharacters => true;

        public override string [] Sequence => new []
        {
            "🕛 ", "🕚 ", "🕙 ", "🕘 ", "🕗 ", "🕖 ", "🕕 ", "🕔 ", "🕓 ", "🕒 ", "🕑 ", "🕐 "
        };

        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => 100;
    }

    public class Aesthetic : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;

        public override string [] Sequence => new []
        {
            "▰▱▱▱▱▱▱", "▰▰▱▱▱▱▱", "▰▰▰▱▱▱▱", "▰▰▰▰▱▱▱", "▰▰▰▰▰▱▱", "▰▰▰▰▰▰▱", "▰▰▰▰▰▰▰", "▰▱▱▱▱▱▱"
        };

        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => DEFAULT_DELAY;
    }

    public class Aesthetic2 : SpinnerStyle
    {
        public override bool HasSpecialCharacters => DEFAULT_SPECIAL;

        public override string [] Sequence => new []
        {
            "▰▱▱▱▱▱▱",
            "▰▰▱▱▱▱▱",
            "▰▰▰▱▱▱▱",
            "▰▰▰▰▱▱▱",
            "▰▰▰▰▰▱▱",
            "▰▰▰▰▰▰▱",
            "▰▰▰▰▰▰▰",
            "▱▰▰▰▰▰▰",
            "▱▱▰▰▰▰▰",
            "▱▱▱▰▰▰▰",
            "▱▱▱▱▰▰▰",
            "▱▱▱▱▱▰▰",
            "▱▱▱▱▱▱▰",
            "▱▱▱▱▱▱▱"
        };

        public override bool SpinBounce => DEFAULT_BOUNCE;
        public override int SpinDelay => DEFAULT_DELAY;
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}

#pragma warning restore CA1034 // Nested types should not be visible
