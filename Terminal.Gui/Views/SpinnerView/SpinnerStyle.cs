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

using System;

namespace Terminal.Gui {
	/// <summary>
	/// SpinnerStyles used in a <see cref="SpinnerView"/>.
	/// </summary>
	public abstract class SpinnerStyle {
		const int DEFAULT_DELAY = 80;
		const bool DEFAULT_BOUNCE = false;
		const bool DEFAULT_SPECIAL = false;

		/// <summary>
		/// Gets or sets the number of milliseconds to wait between characters
		/// in the spin.  Defaults to the SpinnerStyle's Interval value.
		/// </summary>
		/// <remarks>This is the maximum speed the spinner will rotate at.  You still need to
		/// call <see cref="View.SetNeedsDisplay()"/> or <see cref="SpinnerView.AutoSpin"/> to
		/// advance/start animation.</remarks>
		public abstract int SpinDelay { get; }

		/// <summary>
		/// Gets or sets whether spinner should go back and forth through the Sequence rather than
		/// going to the end and starting again at the beginning.
		/// </summary>
		public abstract bool SpinBounce { get; }

		/// <summary>
		/// Gets whether the current spinner style contains emoji or other special characters.
		/// </summary>
		public abstract bool HasSpecialCharacters { get; }

		/// <summary>
		/// Gets or sets the frames used to animate the spinner.
		/// </summary>
		public abstract string [] Sequence { get; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

		// Placeholder when user has specified Delay and Sequence manually
		public class Custom : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => Array.Empty<string> ();
		}
		public class Dots : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"⠋",
			"⠙",
			"⠹",
			"⠸",
			"⠼",
			"⠴",
			"⠦",
			"⠧",
			"⠇",
			"⠏",
			};
		}
		public class Dots2 : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"⣾",
			"⣽",
			"⣻",
			"⢿",
			"⡿",
			"⣟",
			"⣯",
			"⣷",
			};
		}
		public class Dots3 : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"⠋",
			"⠙",
			"⠚",
			"⠞",
			"⠖",
			"⠦",
			"⠴",
			"⠲",
			"⠳",
			"⠓",
			};
		}
		public class Dots4 : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => true;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"⠄",
			"⠆",
			"⠇",
			"⠋",
			"⠙",
			"⠸",
			"⠰",
			"⠠",
			};
		}
		public class Dots5 : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"⠋",
			"⠙",
			"⠚",
			"⠒",
			"⠂",
			"⠂",
			"⠒",
			"⠲",
			"⠴",
			"⠦",
			"⠖",
			"⠒",
			"⠐",
			"⠐",
			"⠒",
			"⠓",
			"⠋",
			};
		}
		public class Dots6 : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => true;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
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
			};
		}
		public class Dots7 : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => true;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"⠈",
			"⠈",
			"⠉",
			"⠋",
			"⠓",
			"⠒",
			"⠐",
			"⠐",
			"⠒",
			"⠖",
			"⠦",
			"⠤",
			"⠠",
			"⠠",
			};
		}
		public class Dots8 : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
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
			"⠈",
			};
		}
		public class Dots9 : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"⢹",
			"⢺",
			"⢼",
			"⣸",
			"⣇",
			"⡧",
			"⡗",
			"⡏",
			};
		}
		public class Dots10 : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"⢄",
			"⢂",
			"⢁",
			"⡁",
			"⡈",
			"⡐",
			"⡠",
			};
		}
		public class Dots11 : SpinnerStyle {
			public override int SpinDelay => 100;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"⠁",
			"⠂",
			"⠄",
			"⡀",
			"⢀",
			"⠠",
			"⠐",
			"⠈",
			};
		}
		public class Dots12 : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
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
			"⠀⡀",
			};
		}
		public class Dots8Bit : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
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
			"⣿",
			};
		}
		public class Line : SpinnerStyle {
			public override int SpinDelay => 130;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			 "-",
			@"\",
			 "|",
			 "/",
			};
		}
		public class Line2 : SpinnerStyle {
			public override int SpinDelay => 100;
			public override bool SpinBounce => true;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"⠂",
			"-",
			"–",
			"—",
			};
		}
		public class Pipe : SpinnerStyle {
			public override int SpinDelay => 100;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"┤",
			"┘",
			"┴",
			"└",
			"├",
			"┌",
			"┬",
			"┐",
			};
		}
		public class SimpleDots : SpinnerStyle {
			public override int SpinDelay => 400;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			".  ",
			".. ",
			"...",
			"   ",
			};
		}
		public class SimpleDotsScrolling : SpinnerStyle {
			public override int SpinDelay => 200;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			".  ",
			".. ",
			"...",
			" ..",
			"  .",
			"   ",
			};
		}
		public class Star : SpinnerStyle {
			public override int SpinDelay => 70;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"✶",
			"✸",
			"✹",
			"✺",
			"✹",
			"✷",
			};
		}
		public class Star2 : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"+",
			"x",
			"*",
			};
		}
		public class Flip : SpinnerStyle {
			public override int SpinDelay => 70;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"_",
			"_",
			"_",
			"-",
			"`",
			"`",
			"'",
			"´",
			"-",
			"_",
			"_",
			"_",
			};
		}
		public class Hamburger : SpinnerStyle {
			public override int SpinDelay => 100;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"☱",
			"☲",
			"☴",
			};
		}
		public class GrowVertical : SpinnerStyle {
			public override int SpinDelay => 120;
			public override bool SpinBounce => true;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"▁",
			"▃",
			"▄",
			"▅",
			"▆",
			"▇",
			};
		}
		public class GrowHorizontal : SpinnerStyle {
			public override int SpinDelay => 120;
			public override bool SpinBounce => true;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"▏",
			"▎",
			"▍",
			"▌",
			"▋",
			"▊",
			"▉",
			};
		}
		public class Balloon : SpinnerStyle {
			public override int SpinDelay => 140;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			" ",
			".",
			"o",
			"O",
			"@",
			"*",
			" ",
			};
		}
		public class Balloon2 : SpinnerStyle {
			public override int SpinDelay => 120;
			public override bool SpinBounce => true;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			".",
			".",
			"o",
			"O",
			"°",
			};
		}
		public class Noise : SpinnerStyle {
			public override int SpinDelay => 100;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"▓",
			"▒",
			"░",
			};
		}
		public class Bounce : SpinnerStyle {
			public override int SpinDelay => 120;
			public override bool SpinBounce => true;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"⠁",
			"⠂",
			"⠄",
			};
		}
		public class BoxBounce : SpinnerStyle {
			public override int SpinDelay => 120;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"▖",
			"▘",
			"▝",
			"▗",
			};
		}
		public class BoxBounce2 : SpinnerStyle {
			public override int SpinDelay => 100;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"▌",
			"▀",
			"▐",
			"▄",
			};
		}
		public class Triangle : SpinnerStyle {
			public override int SpinDelay => 50;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"◢",
			"◣",
			"◤",
			"◥",
			};
		}
		public class Arc : SpinnerStyle {
			public override int SpinDelay => 100;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"◜",
			"◠",
			"◝",
			"◞",
			"◡",
			"◟",
			};
		}
		public class Circle : SpinnerStyle {
			public override int SpinDelay => 120;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"◡",
			"⊙",
			"◠",
			};
		}
		public class SquareCorners : SpinnerStyle {
			public override int SpinDelay => 180;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"◰",
			"◳",
			"◲",
			"◱",
			};
		}
		public class CircleQuarters : SpinnerStyle {
			public override int SpinDelay => 120;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"◴",
			"◷",
			"◶",
			"◵",
			};
		}
		public class CircleHalves : SpinnerStyle {
			public override int SpinDelay => 50;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"◐",
			"◓",
			"◑",
			"◒",
			};
		}
		public class Squish : SpinnerStyle {
			public override int SpinDelay => 100;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"╫",
			"╪",
			};
		}
		public class Toggle : SpinnerStyle {
			public override int SpinDelay => 250;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"⊶",
			"⊷",
			};
		}
		public class Toggle2 : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"▫",
			"▪",
			};
		}
		public class Toggle3 : SpinnerStyle {
			public override int SpinDelay => 120;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"□",
			"■",
			};
		}
		public class Toggle4 : SpinnerStyle {
			public override int SpinDelay => 100;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"■",
			"□",
			"▪",
			"▫",
			};
		}
		public class Toggle5 : SpinnerStyle {
			public override int SpinDelay => 100;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"▮",
			"▯",
			};
		}
		public class Toggle6 : SpinnerStyle {
			public override int SpinDelay => 300;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"ဝ",
			"၀",
			};
		}
		public class Toggle7 : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"⦾",
			"⦿",
			};
		}
		public class Toggle8 : SpinnerStyle {
			public override int SpinDelay => 100;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"◍",
			"◌",
			};
		}
		public class Toggle9 : SpinnerStyle {
			public override int SpinDelay => 100;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"◉",
			"◎",
			};
		}
		public class Toggle10 : SpinnerStyle {
			public override int SpinDelay => 100;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"㊂",
			"㊀",
			"㊁",
			};
		}
		public class Toggle11 : SpinnerStyle {
			public override int SpinDelay => 50;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"⧇",
			"⧆",
			};
		}
		public class Toggle12 : SpinnerStyle {
			public override int SpinDelay => 120;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"☗",
			"☖",
			};
		}
		public class Toggle13 : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"=",
			"*",
			"-",
			};
		}
		public class Arrow : SpinnerStyle {
			public override int SpinDelay => 100;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"←",
			"↖",
			"↑",
			"↗",
			"→",
			"↘",
			"↓",
			"↙",
			};
		}
		public class Arrow2 : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => true;
			public override string [] Sequence => new string []
			{
			"⬆️ ",
			"↗️ ",
			"➡️ ",
			"↘️ ",
			"⬇️ ",
			"↙️ ",
			"⬅️ ",
			"↖️ ",
			};
		}
		public class Arrow3 : SpinnerStyle {
			public override int SpinDelay => 120;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"▹▹▹▹▹",
			"▸▹▹▹▹",
			"▹▸▹▹▹",
			"▹▹▸▹▹",
			"▹▹▹▸▹",
			"▹▹▹▹▸",
			};
		}
		public class BouncingBar : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => true;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"[    ]",
			"[=   ]",
			"[==  ]",
			"[=== ]",
			"[ ===]",
			"[  ==]",
			"[   =]",
			"[    ]",
			};
		}
		public class BouncingBall : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => true;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"(●     )",
			"( ●    )",
			"(  ●   )",
			"(   ●  )",
			"(    ● )",
			"(     ●)",
			};
		}
		public class Smiley : SpinnerStyle {
			public override int SpinDelay => 200;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => true;
			public override string [] Sequence => new string []
			{
			"😄 ",
			"😝 ",
			};
		}
		public class Monkey : SpinnerStyle {
			public override int SpinDelay => 300;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => true;
			public override string [] Sequence => new string []
			{
			"🙈 ",
			"🙈 ",
			"🙉 ",
			"🙊 ",
			};
		}
		public class Hearts : SpinnerStyle {
			public override int SpinDelay => 100;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => true;
			public override string [] Sequence => new string []
			{
			"💛 ",
			"💙 ",
			"💜 ",
			"💚 ",
			"❤️ ",
			};
		}
		public class Clock : SpinnerStyle {
			public override int SpinDelay => 100;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => true;
			public override string [] Sequence => new string []
			{
			"🕛 ",
			"🕐 ",
			"🕑 ",
			"🕒 ",
			"🕓 ",
			"🕔 ",
			"🕕 ",
			"🕖 ",
			"🕗 ",
			"🕘 ",
			"🕙 ",
			"🕚 ",
			};
		}
		public class Earth : SpinnerStyle {
			public override int SpinDelay => 180;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => true;
			public override string [] Sequence => new string []
			{
			"🌍 ",
			"🌎 ",
			"🌏 ",
			};
		}
		public class Material : SpinnerStyle {
			public override int SpinDelay => 17;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
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
			"▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁",
			};
		}
		public class Moon : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => true;
			public override string [] Sequence => new string []
			{
			"🌑 ",
			"🌒 ",
			"🌓 ",
			"🌔 ",
			"🌕 ",
			"🌖 ",
			"🌗 ",
			"🌘 ",
			};
		}
		public class Runner : SpinnerStyle {
			public override int SpinDelay => 140;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => true;
			public override string [] Sequence => new string []
			{
			"🚶 ",
			"🏃 ",
			};
		}
		public class Pong : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => true;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
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
			"▐⠠       ▌",
			};
		}
		public class Shark : SpinnerStyle {
			public override int SpinDelay => 120;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
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
			 "▐/|____________▌",
			};
		}
		public class Dqpb : SpinnerStyle {
			public override int SpinDelay => 100;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"d",
			"q",
			"p",
			"b",
			};
		}
		public class Weather : SpinnerStyle {
			public override int SpinDelay => 100;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => true;
			public override string [] Sequence => new string []
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
			"☀️ ",
			};
		}
		public class Christmas : SpinnerStyle {
			public override int SpinDelay => 400;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => true;
			public override string [] Sequence => new string []
			{
			"🌲",
			"🎄",
			};
		}
		public class Grenade : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => true;
			public override string [] Sequence => new string []
			{
			"،   ",
			"′   ",
			" ´ ",
			" ‾ ",
			"  ⸌",
			"  ⸊",
			"  |",
			"  ⁎",
			"  ⁕",
			" ෴ ",
			"  ⁓",
			"   ",
			"   ",
			"   ",
			};
		}
		public class Points : SpinnerStyle {
			public override int SpinDelay => 125;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"∙∙∙",
			"●∙∙",
			"∙●∙",
			"∙∙●",
			"∙∙∙",
			};
		}
		public class Layer : SpinnerStyle {
			public override int SpinDelay => 150;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"-",
			"=",
			"≡",
			};
		}
		public class BetaWave : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"ρββββββ",
			"βρβββββ",
			"ββρββββ",
			"βββρβββ",
			"ββββρββ",
			"βββββρβ",
			"ββββββρ",
			};
		}
		public class FingerDance : SpinnerStyle {
			public override int SpinDelay => 160;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => true;
			public override string [] Sequence => new string []
			{
			"🤘 ",
			"🤟 ",
			"🖖 ",
			"✋ ",
			"🤚 ",
			"👆 "
			};
		}
		public class FistBump : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => true;
			public override string [] Sequence => new string []
			{
			"🤜\u3000\u3000\u3000\u3000🤛 ",
			"🤜\u3000\u3000\u3000\u3000🤛 ",
			"🤜\u3000\u3000\u3000\u3000🤛 ",
			"\u3000🤜\u3000\u3000🤛\u3000 ",
			"\u3000\u3000🤜🤛\u3000\u3000 ",
			"\u3000🤜✨🤛\u3000\u3000 ",
			"🤜\u3000✨\u3000🤛\u3000 "
			};
		}
		public class SoccerHeader : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => true;
			public override bool HasSpecialCharacters => true;
			public override string [] Sequence => new string []
			{
			" 🧑⚽️       🧑 ",
			"🧑  ⚽️      🧑 ",
			"🧑   ⚽️     🧑 ",
			"🧑    ⚽️    🧑 ",
			"🧑     ⚽️   🧑 ",
			"🧑      ⚽️  🧑 ",
			"🧑       ⚽️🧑  ",
			};
		}
		public class MindBlown : SpinnerStyle {
			public override int SpinDelay => 160;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => true;
			public override string [] Sequence => new string []
			{
			"😐 ",
			"😐 ",
			"😮 ",
			"😮 ",
			"😦 ",
			"😦 ",
			"😧 ",
			"😧 ",
			"🤯 ",
			"💥 ",
			"✨ ",
			"\u3000 ",
			"\u3000 ",
			"\u3000 "
			};
		}
		public class Speaker : SpinnerStyle {
			public override int SpinDelay => 160;
			public override bool SpinBounce => true;
			public override bool HasSpecialCharacters => true;
			public override string [] Sequence => new string []
			{
			"🔈 ",
			"🔉 ",
			"🔊 ",
			};
		}
		public class OrangePulse : SpinnerStyle {
			public override int SpinDelay => 100;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => true;
			public override string [] Sequence => new string []
			{
			"🔸 ",
			"🔶 ",
			"🟠 ",
			"🟠 ",
			"🔶 "
			};
		}
		public class BluePulse : SpinnerStyle {
			public override int SpinDelay => 100;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => true;
			public override string [] Sequence => new string []
			{
			"🔹 ",
			"🔷 ",
			"🔵 ",
			"🔵 ",
			"🔷 "
			};
		}
		public class OrangeBluePulse : SpinnerStyle {
			public override int SpinDelay => 100;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => true;
			public override string [] Sequence => new string []
			{
			"🔸 ",
			"🔶 ",
			"🟠 ",
			"🟠 ",
			"🔶 ",
			"🔹 ",
			"🔷 ",
			"🔵 ",
			"🔵 ",
			"🔷 "
			};
		}
		public class TimeTravelClock : SpinnerStyle {
			public override int SpinDelay => 100;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => true;
			public override string [] Sequence => new string []
			{
			"🕛 ",
			"🕚 ",
			"🕙 ",
			"🕘 ",
			"🕗 ",
			"🕖 ",
			"🕕 ",
			"🕔 ",
			"🕓 ",
			"🕒 ",
			"🕑 ",
			"🕐 "
			};
		}
		public class Aesthetic : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
			{
			"▰▱▱▱▱▱▱",
			"▰▰▱▱▱▱▱",
			"▰▰▰▱▱▱▱",
			"▰▰▰▰▱▱▱",
			"▰▰▰▰▰▱▱",
			"▰▰▰▰▰▰▱",
			"▰▰▰▰▰▰▰",
			"▰▱▱▱▱▱▱",
			};
		}
		public class Aesthetic2 : SpinnerStyle {
			public override int SpinDelay => DEFAULT_DELAY;
			public override bool SpinBounce => DEFAULT_BOUNCE;
			public override bool HasSpecialCharacters => DEFAULT_SPECIAL;
			public override string [] Sequence => new string []
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
			"▱▱▱▱▱▱▱",
			};
		}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}