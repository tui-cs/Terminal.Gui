#define DRAW_CONTENT
//#define BASE_DRAW_CONTENT

using Microsoft.VisualBasic;
using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminal.Gui;
using Terminal.Gui.Resources;
using Rune = System.Rune;

namespace UICatalog.Scenarios {
	/// <summary>
	/// This Scenario demonstrates building a custom control (a class deriving from View) that:
	///   - Provides a "Character Map" application (like Windows' charmap.exe).
	///   - Helps test unicode character rendering in Terminal.Gui
	///   - Illustrates how to use ScrollView to do infinite scrolling
	/// </summary>
	[ScenarioMetadata (Name: "Character Map",
		Description: "A Unicode character set viewier built as a custom control using the ScrollView control.")]
	[ScenarioCategory ("Text and Formatting")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("ScrollView")]
	public class CharacterMap : Scenario {
		CharMap _charMap;
		public override void Setup ()
		{
			_charMap = new CharMap () {
				X = 0,
				Y = 0,
				Height = Dim.Fill (),
			};

			var jumpLabel = new Label ("Jump To Glyph:") { X = Pos.Right (_charMap) + 1, Y = Pos.Y (_charMap) };
			Win.Add (jumpLabel);
			var jumpEdit = new TextField () { X = Pos.Right (jumpLabel) + 1, Y = Pos.Y (_charMap), Width = 10, };
			Win.Add (jumpEdit);
			var unicodeLabel = new Label ("") { X = Pos.Right (jumpEdit) + 1, Y = Pos.Y (_charMap) };
			Win.Add (unicodeLabel);
			jumpEdit.TextChanged += (s, e) => {
				uint result = 0;
				if (jumpEdit.Text.Length == 0) return;
				try {
					result = Convert.ToUInt32 (jumpEdit.Text.ToString (), 10);
				} catch (OverflowException) {
					unicodeLabel.Text = $"Invalid (overflow)";
					return;
				} catch (FormatException) {
					try {
						result = Convert.ToUInt32 (jumpEdit.Text.ToString (), 16);
					} catch (OverflowException) {
						unicodeLabel.Text = $"Invalid (overflow)";
						return;
					} catch (FormatException) {
						unicodeLabel.Text = $"Invalid (can't parse)";
						return;
					}
				}
				unicodeLabel.Text = $"U+{result:x4}";
				_charMap.SelectedGlyph = result;
			};

			var radioItems = new (ustring radioLabel, uint start, uint end) [UnicodeRange.Ranges.Count];

			for (var i = 0; i < UnicodeRange.Ranges.Count; i++) {
				var range = UnicodeRange.Ranges [i];
				radioItems [i] = CreateRadio (range.Category, range.Start, range.End);
			}
			(ustring radioLabel, uint start, uint end) CreateRadio (ustring title, uint start, uint end)
			{
				return ($"{title} (U+{start:x5}-{end:x5})", start, end);
			}

			Win.Add (_charMap);
			var label = new Label ("Jump To Unicode Block:") { X = Pos.Right (_charMap) + 1, Y = Pos.Bottom (jumpLabel) + 1 };
			Win.Add (label);

			var jumpList = new ListView (radioItems.Select (t => t.radioLabel).ToArray ()) {
				X = Pos.X (label) + 1,
				Y = Pos.Bottom (label),
				Width = radioItems.Max (r => r.radioLabel.Length) + 2,
				Height = Dim.Fill(1),
				SelectedItem = 0
			};
			jumpList.SelectedItemChanged += (s, args) => {
				_charMap.StartGlyph = radioItems [jumpList.SelectedItem].start;
			};

			Win.Add (jumpList);

			//jumpList.Refresh ();
			_charMap.SetFocus ();

			_charMap.Width = Dim.Fill () - jumpList.Width;
		}
	}

	class CharMap : ScrollView {

		/// <summary>
		/// Specifies the starting offset for the character map. The default is 0x2500 
		/// which is the Box Drawing characters.
		/// </summary>
		public uint StartGlyph {
			get => _start;
			set {
				_start = value;
				_selected = value;
				ContentOffset = new Point (0, (int)(_start / 16));
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Specifies the starting offset for the character map. The default is 0x2500 
		/// which is the Box Drawing characters.
		/// </summary>
		public uint SelectedGlyph {
			get => _selected;
			set {
				_selected = value;
				int row = (int)_selected / 16;
				int height = (Bounds.Height / ROW_HEIGHT) - 1;
				if (row + ContentOffset.Y < 0) {
					// Moving up.
					ContentOffset = new Point (0, row);
				} else if (row + ContentOffset.Y >= height) {
					// Moving down.
					ContentOffset = new Point (0, Math.Min (row, (row - height) + 1));

				} else {
					//ContentOffset = new Point (0, Math.Min (row, (row - height) - 1));
				}

				SetNeedsDisplay ();
			}
		}

		uint _start = 0;
		uint _selected = 0;

		public const int COLUMN_WIDTH = 3;
		public const int ROW_HEIGHT = 1;

		public static uint MaxCodePointVal => 0x10FFFF;

		public static int RowLabelWidth => $"U+{MaxCodePointVal:x5}".Length + 1; 
		public static int RowWidth => RowLabelWidth + (COLUMN_WIDTH * 16);

		public CharMap ()
		{
			ColorScheme = Colors.Dialog;
			CanFocus = true;

			ContentSize = new Size (CharMap.RowWidth, (int)(MaxCodePointVal / 16 + 1));
			ShowVerticalScrollIndicator = true;
			ShowHorizontalScrollIndicator = false;
			LayoutComplete += (s, args) => {
				if (Bounds.Width < RowWidth) {
					ShowHorizontalScrollIndicator = true;
				} else {
					ShowHorizontalScrollIndicator = false;
					// Snap 1st column into view if it's been scrolled horizontally 
					ContentOffset = new Point (0, ContentOffset.Y);
					SetNeedsDisplay ();
				}
			};
			DrawContent += CharMap_DrawContent;

			AddCommand (Command.ScrollUp, () => {
				if (SelectedGlyph >= 16) {
					SelectedGlyph = SelectedGlyph - 16;
				}
				return true;
			});
			AddCommand (Command.ScrollDown, () => {
				if (SelectedGlyph < MaxCodePointVal - 16) {
					SelectedGlyph = SelectedGlyph + 16;
				}
				return true;
			});
			AddCommand (Command.ScrollLeft, () => {
				if (SelectedGlyph > 0) {
					SelectedGlyph--;
				}
				return true;
			});
			AddCommand (Command.ScrollRight, () => {
				if (SelectedGlyph < MaxCodePointVal - 1) {
					SelectedGlyph++;
				}
				return true;
			});
			AddCommand (Command.PageUp, () => {
				var page = (uint)(Bounds.Height / ROW_HEIGHT - 1) * 16;
				SelectedGlyph -= Math.Min(page, SelectedGlyph);
				return true;
			});
			AddCommand (Command.PageDown, () => {
				var page = (uint)(Bounds.Height / ROW_HEIGHT - 1) * 16;
				SelectedGlyph += Math.Min(page, MaxCodePointVal -SelectedGlyph);
				return true;
			});
			AddCommand (Command.TopHome, () => {
				SelectedGlyph = 0;
				return true;
			});
			AddCommand (Command.BottomEnd, () => {
				SelectedGlyph = MaxCodePointVal;
				return true;
			});

			MouseClick += Handle_MouseClick;
			Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);
		}

		private void CopyValue ()
		{
			Clipboard.Contents = $"U+{SelectedGlyph:x5}";
		}

		private void CopyGlyph ()
		{
			Clipboard.Contents = $"{new Rune (SelectedGlyph)}";
		}

		private void CharMap_DrawContent (object sender, DrawEventArgs e)
		{
			Rect viewport = e.Rect;

			var oldClip = Driver.Clip;
			Driver.Clip = Frame;
			// Redraw doesn't know about the scroll indicators, so if off, add one to height
			if (!ShowHorizontalScrollIndicator) {
				Driver.Clip = new Rect (Frame.X, Frame.Y, Frame.Width, Frame.Height + 1);
			}
			Driver.SetAttribute (HasFocus ? ColorScheme.HotFocus : ColorScheme.Focus);
			Move (0, 0);
			Driver.AddStr (new string (' ', RowLabelWidth + 1));
			for (int hexDigit = 0; hexDigit < 16; hexDigit++) {
				var x = ContentOffset.X + RowLabelWidth + (hexDigit * COLUMN_WIDTH);
				if (x > RowLabelWidth - 2) {
					Move (x, 0);
					Driver.AddStr ($" {hexDigit:x} ");
				}
			}

			var firstColumnX = viewport.X + RowLabelWidth;
			for (int row = -ContentOffset.Y, y = 0; row <= (-ContentOffset.Y) + (Bounds.Height / ROW_HEIGHT); row++, y += ROW_HEIGHT) {
				int val = (row) * 16;
				Driver.SetAttribute (GetNormalColor ());
				Move (firstColumnX, y + 1);
				Driver.AddStr (new string (' ', 16 * COLUMN_WIDTH));
				if (val <= MaxCodePointVal) {
					Driver.SetAttribute (GetNormalColor ());
					for (int col = 0; col < 16; col++) {
						uint glyph = (uint)((uint)val + col);
						var rune = new Rune (glyph);
						//if (rune >= 0x00D800 && rune <= 0x00DFFF) {
						//	if (col == 0) {
						//		Driver.AddStr ("Reserved for surrogate pairs.");
						//	}
						//	continue;
						//}						
						Move (firstColumnX + (col * COLUMN_WIDTH) + 1, y + 1);
						if (glyph == SelectedGlyph) {
							Driver.SetAttribute (HasFocus ? ColorScheme.HotFocus : ColorScheme.HotNormal);
						} else {
							Driver.SetAttribute (GetNormalColor ());
						}
						Driver.AddRune (rune);
					}
					Move (0, y + 1);
					Driver.SetAttribute (HasFocus ? ColorScheme.HotFocus : ColorScheme.Focus);
					var rowLabel = $"U+{val / 16:x5}_ ";
					Driver.AddStr (rowLabel);
				}
			}
			Driver.Clip = oldClip;
		}

		ContextMenu _contextMenu = new ContextMenu ();
		void Handle_MouseClick (object sender, MouseEventEventArgs args)
		{
			var me = args.MouseEvent;
			if (me.Flags == MouseFlags.ReportMousePosition || (me.Flags != MouseFlags.Button1Clicked &&
				me.Flags != MouseFlags.Button1DoubleClicked &&
				me.Flags != _contextMenu.MouseFlags)) {
				return;
			}

			if (me.X < RowLabelWidth) {
				return;
			}

			if (me.Y < 1) {
				return;
			}

			var row = (me.Y - 1);
			var col = (me.X - RowLabelWidth - ContentOffset.X) / COLUMN_WIDTH;
			uint val = (uint)((((uint)row - (uint)ContentOffset.Y) * 16) + col);
			if (val > MaxCodePointVal) {
				return;
			}

			if (me.Flags == MouseFlags.Button1Clicked) {
				SelectedGlyph = (uint)val;
				return;
			}

			if (me.Flags == MouseFlags.Button1DoubleClicked) {
				SelectedGlyph = (uint)val;
				MessageBox.Query ("Glyph", $"{new Rune (val)} U+{SelectedGlyph:x4}", "Ok");
				return;
			}

			if (me.Flags == _contextMenu.MouseFlags) {
				SelectedGlyph = (uint)val;
				_contextMenu = new ContextMenu (me.X + 1, me.Y + 1,
					new MenuBarItem (new MenuItem [] {
					new MenuItem ("_Copy Glyph", "", () => CopyGlyph (), null, null, Key.C | Key.CtrlMask),
					new MenuItem ("Copy _Value", "", () => CopyValue (), null, null, Key.C | Key.ShiftMask | Key.CtrlMask),
					}) {

					}
				);
				_contextMenu.Show ();
			}
		}

		protected override void Dispose (bool disposing)
		{
			DrawContent -= CharMap_DrawContent;
			base.Dispose (disposing);
		}
	}

	class UnicodeRange {
		public uint Start;
		public uint End;
		public string Category;
		public UnicodeRange (uint start, uint end, string category)
		{
			this.Start = start;
			this.End = end;
			this.Category = category;
		}
			
		public static List<UnicodeRange> Ranges = new List<UnicodeRange> {
			new UnicodeRange (0x0000, 0x001F, "ASCII Control Characters"),
			new UnicodeRange (0x0080, 0x009F, "C0 Control Characters"),
			new UnicodeRange(0x1100, 0x11ff,"Hangul Jamo"),	// This is where wide chars tend to start
			new UnicodeRange(0x20A0, 0x20CF,"Currency Symbols"),
			new UnicodeRange(0x2100, 0x214F,"Letterlike Symbols"),
			new UnicodeRange(0x2160, 0x218F, "Roman Numerals"),
			new UnicodeRange(0x2190, 0x21ff,"Arrows" ),
			new UnicodeRange(0x2200, 0x22ff,"Mathematical symbols"),
			new UnicodeRange(0x2300, 0x23ff,"Miscellaneous Technical"),
			new UnicodeRange(0x24B6, 0x24e9,"Circled Latin Capital Letters"), 
			new UnicodeRange(0x1F130, 0x1F149,"Squared Latin Capital Letters"), 
			new UnicodeRange(0x2500, 0x25ff,"Box Drawing & Geometric Shapes"),
			new UnicodeRange(0x2600, 0x26ff,"Miscellaneous Symbols"),
			new UnicodeRange(0x2700, 0x27ff,"Dingbats"),
			new UnicodeRange(0x2800, 0x28ff,"Braille"),
			new UnicodeRange(0x2b00, 0x2bff,"Miscellaneous Symbols and Arrows"),
			new UnicodeRange(0xFB00, 0xFb4f,"Alphabetic Presentation Forms"),
			new UnicodeRange(0x12400, 0x1240f,"Cuneiform Numbers and Punctuation"),
			new UnicodeRange(0x1FA00, 0x1FA0f,"Chess Symbols"),

			new UnicodeRange (0x0020 ,0x007F        ,"Basic Latin"),
			new UnicodeRange (0x00A0 ,0x00FF        ,"Latin-1 Supplement"),
			new UnicodeRange (0x0100 ,0x017F        ,"Latin Extended-A"),
			new UnicodeRange (0x0180 ,0x024F        ,"Latin Extended-B"),
			new UnicodeRange (0x0250 ,0x02AF        ,"IPA Extensions"),
			new UnicodeRange (0x02B0 ,0x02FF        ,"Spacing Modifier Letters"),
			new UnicodeRange (0x0300 ,0x036F        ,"Combining Diacritical Marks"),
			new UnicodeRange (0x0370 ,0x03FF        ,"Greek and Coptic"),
			new UnicodeRange (0x0400 ,0x04FF        ,"Cyrillic"),
			new UnicodeRange (0x0500 ,0x052F        ,"Cyrillic Supplementary"),
			new UnicodeRange (0x0530 ,0x058F        ,"Armenian"),
			new UnicodeRange (0x0590 ,0x05FF        ,"Hebrew"),
			new UnicodeRange (0x0600 ,0x06FF        ,"Arabic"),
			new UnicodeRange (0x0700 ,0x074F        ,"Syriac"),
			new UnicodeRange (0x0780 ,0x07BF        ,"Thaana"),
			new UnicodeRange (0x0900 ,0x097F        ,"Devanagari"),
			new UnicodeRange (0x0980 ,0x09FF        ,"Bengali"),
			new UnicodeRange (0x0A00 ,0x0A7F        ,"Gurmukhi"),
			new UnicodeRange (0x0A80 ,0x0AFF        ,"Gujarati"),
			new UnicodeRange (0x0B00 ,0x0B7F        ,"Oriya"),
			new UnicodeRange (0x0B80 ,0x0BFF        ,"Tamil"),
			new UnicodeRange (0x0C00 ,0x0C7F        ,"Telugu"),
			new UnicodeRange (0x0C80 ,0x0CFF        ,"Kannada"),
			new UnicodeRange (0x0D00 ,0x0D7F        ,"Malayalam"),
			new UnicodeRange (0x0D80 ,0x0DFF        ,"Sinhala"),
			new UnicodeRange (0x0E00 ,0x0E7F        ,"Thai"),
			new UnicodeRange (0x0E80 ,0x0EFF        ,"Lao"),
			new UnicodeRange (0x0F00 ,0x0FFF        ,"Tibetan"),
			new UnicodeRange (0x1000 ,0x109F        ,"Myanmar"),
			new UnicodeRange (0x10A0 ,0x10FF        ,"Georgian"),
			new UnicodeRange (0x1100 ,0x11FF        ,"Hangul Jamo"),
			new UnicodeRange (0x1200 ,0x137F        ,"Ethiopic"),
			new UnicodeRange (0x13A0 ,0x13FF        ,"Cherokee"),
			new UnicodeRange (0x1400 ,0x167F        ,"Unified Canadian Aboriginal Syllabics"),
			new UnicodeRange (0x1680 ,0x169F        ,"Ogham"),
			new UnicodeRange (0x16A0 ,0x16FF        ,"Runic"),
			new UnicodeRange (0x1700 ,0x171F        ,"Tagalog"),
			new UnicodeRange (0x1720 ,0x173F        ,"Hanunoo"),
			new UnicodeRange (0x1740 ,0x175F        ,"Buhid"),
			new UnicodeRange (0x1760 ,0x177F        ,"Tagbanwa"),
			new UnicodeRange (0x1780 ,0x17FF        ,"Khmer"),
			new UnicodeRange (0x1800 ,0x18AF        ,"Mongolian"),
			new UnicodeRange (0x1900 ,0x194F        ,"Limbu"),
			new UnicodeRange (0x1950 ,0x197F        ,"Tai Le"),
			new UnicodeRange (0x19E0 ,0x19FF        ,"Khmer Symbols"),
			new UnicodeRange (0x1D00 ,0x1D7F        ,"Phonetic Extensions"),
			new UnicodeRange (0x1E00 ,0x1EFF        ,"Latin Extended Additional"),
			new UnicodeRange (0x1F00 ,0x1FFF        ,"Greek Extended"),
			new UnicodeRange (0x2000 ,0x206F        ,"General Punctuation"),
			new UnicodeRange (0x2070 ,0x209F        ,"Superscripts and Subscripts"),
			new UnicodeRange (0x20A0 ,0x20CF        ,"Currency Symbols"),
			new UnicodeRange (0x20D0 ,0x20FF        ,"Combining Diacritical Marks for Symbols"),
			new UnicodeRange (0x2100 ,0x214F        ,"Letterlike Symbols"),
			new UnicodeRange (0x2150 ,0x218F        ,"Number Forms"),
			new UnicodeRange (0x2190 ,0x21FF        ,"Arrows"),
			new UnicodeRange (0x2200 ,0x22FF        ,"Mathematical Operators"),
			new UnicodeRange (0x2300 ,0x23FF        ,"Miscellaneous Technical"),
			new UnicodeRange (0x2400 ,0x243F        ,"Control Pictures"),
			new UnicodeRange (0x2440 ,0x245F        ,"Optical Character Recognition"),
			new UnicodeRange (0x2460 ,0x24FF        ,"Enclosed Alphanumerics"),
			new UnicodeRange (0x2500 ,0x257F        ,"Box Drawing"),
			new UnicodeRange (0x2580 ,0x259F        ,"Block Elements"),
			new UnicodeRange (0x25A0 ,0x25FF        ,"Geometric Shapes"),
			new UnicodeRange (0x2600 ,0x26FF        ,"Miscellaneous Symbols"),
			new UnicodeRange (0x2700 ,0x27BF        ,"Dingbats"),
			new UnicodeRange (0x27C0 ,0x27EF        ,"Miscellaneous Mathematical Symbols-A"),
			new UnicodeRange (0x27F0 ,0x27FF        ,"Supplemental Arrows-A"),
			new UnicodeRange (0x2800 ,0x28FF        ,"Braille Patterns"),
			new UnicodeRange (0x2900 ,0x297F        ,"Supplemental Arrows-B"),
			new UnicodeRange (0x2980 ,0x29FF        ,"Miscellaneous Mathematical Symbols-B"),
			new UnicodeRange (0x2A00 ,0x2AFF        ,"Supplemental Mathematical Operators"),
			new UnicodeRange (0x2B00 ,0x2BFF        ,"Miscellaneous Symbols and Arrows"),
			new UnicodeRange (0x2E80 ,0x2EFF        ,"CJK Radicals Supplement"),
			new UnicodeRange (0x2F00 ,0x2FDF        ,"Kangxi Radicals"),
			new UnicodeRange (0x2FF0 ,0x2FFF        ,"Ideographic Description Characters"),
			new UnicodeRange (0x3000 ,0x303F        ,"CJK Symbols and Punctuation"),
			new UnicodeRange (0x3040 ,0x309F        ,"Hiragana"),
			new UnicodeRange (0x30A0 ,0x30FF        ,"Katakana"),
			new UnicodeRange (0x3100 ,0x312F        ,"Bopomofo"),
			new UnicodeRange (0x3130 ,0x318F        ,"Hangul Compatibility Jamo"),
			new UnicodeRange (0x3190 ,0x319F        ,"Kanbun"),
			new UnicodeRange (0x31A0 ,0x31BF        ,"Bopomofo Extended"),
			new UnicodeRange (0x31F0 ,0x31FF        ,"Katakana Phonetic Extensions"),
			new UnicodeRange (0x3200 ,0x32FF        ,"Enclosed CJK Letters and Months"),
			new UnicodeRange (0x3300 ,0x33FF        ,"CJK Compatibility"),
			new UnicodeRange (0x3400 ,0x4DBF        ,"CJK Unified Ideographs Extension A"),
			new UnicodeRange (0x4DC0 ,0x4DFF        ,"Yijing Hexagram Symbols"),
			new UnicodeRange (0x4E00 ,0x9FFF        ,"CJK Unified Ideographs"),
			new UnicodeRange (0xA000 ,0xA48F        ,"Yi Syllables"),
			new UnicodeRange (0xA490 ,0xA4CF        ,"Yi Radicals"),
			new UnicodeRange (0xAC00 ,0xD7AF        ,"Hangul Syllables"),
			new UnicodeRange (0xD800 ,0xDB7F        ,"High Surrogates"),
			new UnicodeRange (0xDB80 ,0xDBFF        ,"High Private Use Surrogates"),
			new UnicodeRange (0xDC00 ,0xDFFF        ,"Low Surrogates"),
			new UnicodeRange (0xE000 ,0xF8FF        ,"Private Use Area"),
			new UnicodeRange (0xF900 ,0xFAFF        ,"CJK Compatibility Ideographs"),
			new UnicodeRange (0xFB00 ,0xFB4F        ,"Alphabetic Presentation Forms"),
			new UnicodeRange (0xFB50 ,0xFDFF        ,"Arabic Presentation Forms-A"),
			new UnicodeRange (0xFE00 ,0xFE0F        ,"Variation Selectors"),
			new UnicodeRange (0xFE20 ,0xFE2F        ,"Combining Half Marks"),
			new UnicodeRange (0xFE30 ,0xFE4F        ,"CJK Compatibility Forms"),
			new UnicodeRange (0xFE50 ,0xFE6F        ,"Small Form Variants"),
			new UnicodeRange (0xFE70 ,0xFEFF        ,"Arabic Presentation Forms-B"),
			new UnicodeRange (0xFF00 ,0xFFEF        ,"Halfwidth and Fullwidth Forms"),
			new UnicodeRange (0xFFF0 ,0xFFFF        ,"Specials"),
			new UnicodeRange (0x10000, 0x1007F   ,"Linear B Syllabary"),
			new UnicodeRange (0x10080, 0x100FF   ,"Linear B Ideograms"),
			new UnicodeRange (0x10100, 0x1013F   ,"Aegean Numbers"),
			new UnicodeRange (0x10300, 0x1032F   ,"Old Italic"),
			new UnicodeRange (0x10330, 0x1034F   ,"Gothic"),
			new UnicodeRange (0x10380, 0x1039F   ,"Ugaritic"),
			new UnicodeRange (0x10400, 0x1044F   ,"Deseret"),
			new UnicodeRange (0x10450, 0x1047F   ,"Shavian"),
			new UnicodeRange (0x10480, 0x104AF   ,"Osmanya"),
			new UnicodeRange (0x10800, 0x1083F   ,"Cypriot Syllabary"),
			new UnicodeRange (0x1D000, 0x1D0FF   ,"Byzantine Musical Symbols"),
			new UnicodeRange (0x1D100, 0x1D1FF   ,"Musical Symbols"),
			new UnicodeRange (0x1D300, 0x1D35F   ,"Tai Xuan Jing Symbols"),
			new UnicodeRange (0x1D400, 0x1D7FF   ,"Mathematical Alphanumeric Symbols"),
			new UnicodeRange (0x1F600, 0x1F532   ,"Emojis Symbols"),
			new UnicodeRange (0x20000, 0x2A6DF   ,"CJK Unified Ideographs Extension B"),
			new UnicodeRange (0x2F800, 0x2FA1F   ,"CJK Compatibility Ideographs Supplement"),
			new UnicodeRange (0xE0000, 0xE007F   ,"Tags"),
			new UnicodeRange((uint)(CharMap.MaxCodePointVal - 16), (uint)CharMap.MaxCodePointVal,"End"),
		};
	}
	
}
