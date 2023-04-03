using NStack;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Unicode", Description: "Tries to test Unicode in all controls (#204)")]
	[ScenarioCategory ("Text and Formatting")]
	[ScenarioCategory ("Controls")]
	public class UnicodeInMenu : Scenario {
		public override void Setup ()
		{
			const string IdenticalSign = "\u2261";
			const string ArrowUpSign = "\u2191";
			const string ArrowDownSign = "\u2193";
			const string EllipsesSign = "\u2026";
			const string StashSign = "\u205E";

			//string text = "Hello world, how are you today? Pretty neat!\nSecond line\n\nFourth Line.";
			string unicode = "Τὴ γλῶσσα μοῦ ἔδωσαν ἑλληνικὴ\nτὸ σπίτι φτωχικὸ στὶς ἀμμουδιὲς τοῦ Ὁμήρου.\nΜονάχη ἔγνοια ἡ γλῶσσα μου στὶς ἀμμουδιὲς τοῦ Ὁμήρου.";

			string gitString = $"gui.cs master {IdenticalSign} {ArrowDownSign}18 {ArrowUpSign}10 {StashSign}1 {EllipsesSign}";

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_Файл", new MenuItem [] {
					new MenuItem ("_Создать", "Creates new file", null),
					new MenuItem ("_Открыть", "", null),
					new MenuItem ("Со_хранить", "", null),
					new MenuItem ("_Выход", "", () => Application.RequestStop() )
				}),
				new MenuBarItem ("_Edit", new MenuItem [] {
					new MenuItem ("_Copy", "", null),
					new MenuItem ("C_ut", "", null),
					new MenuItem ("_Paste", "", null)
				})
			});
			Application.Top.Add (menu);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Application.QuitKey, $"{Application.QuitKey} Выход", () => Application.RequestStop()),
				new StatusItem (Key.Unknown, "~F2~ Создать", null),
				new StatusItem(Key.Unknown, "~F3~ Со_хранить", null),
			});
			Application.Top.Add (statusBar);

			var label = new Label ("Label:") { X = 0, Y = 1 };
			Win.Add (label);
			var testlabel = new Label (gitString) { X = 20, Y = Pos.Y (label), Width = Dim.Percent (50), };
			Win.Add (testlabel);

			label = new Label ("Label (CanFocus):") { X = Pos.X (label), Y = Pos.Bottom (label) + 1 };
			Win.Add (label);
			testlabel = new Label ("Стоял &он, дум великих полн") { X = 20, Y = Pos.Y (label), Width = Dim.Percent (50), CanFocus = true, HotKeySpecifier = new System.Rune ('&') };
			Win.Add (testlabel);

			label = new Label ("Button:") { X = Pos.X (label), Y = Pos.Bottom (label) + 1 };
			Win.Add (label);
			var button = new Button ("A123456789♥♦♣♠JQK") { X = 20, Y = Pos.Y (label) };
			Win.Add (button);

			label = new Label ("CheckBox:") { X = Pos.X (label), Y = Pos.Bottom (label) + 1 };
			Win.Add (label);
			var checkBox = new CheckBox (gitString) { X = 20, Y = Pos.Y (label), Width = Dim.Percent (50) };
			var ckbAllowNull = new CheckBox ("Allow null checked") { X = Pos.Right (checkBox) + 1, Y = Pos.Y (label) };
			ckbAllowNull.Toggled += (s,e) => checkBox.AllowNullChecked = (bool)!e.OldValue;
			Win.Add (checkBox, ckbAllowNull);

			label = new Label ("ComboBox:") { X = Pos.X (label), Y = Pos.Bottom (label) + 1 };
			Win.Add (label);
			var comboBox = new ComboBox () {
				X = 20,
				Y = Pos.Y (label),
				Width = Dim.Percent (50)
			};
			comboBox.SetSource (new List<string> () { gitString, "Со_хранить" });

			Win.Add (comboBox);
			comboBox.Text = gitString;

			label = new Label ("HexView:") { X = Pos.X (label), Y = Pos.Bottom (label) + 2 };
			Win.Add (label);
			var hexView = new HexView (new System.IO.MemoryStream (Encoding.ASCII.GetBytes (gitString + " Со_хранить"))) {
				X = 20,
				Y = Pos.Y (label),
				Width = Dim.Percent (60),
				Height = 5
			};
			Win.Add (hexView);

			label = new Label ("ListView:") { X = Pos.X (label), Y = Pos.Bottom (hexView) + 1 };
			Win.Add (label);
			var listView = new ListView (new List<string> () { "item #1", gitString, "Со_хранить", unicode }) {
				X = 20,
				Y = Pos.Y (label),
				Width = Dim.Percent (60),
				Height = 3,
			};
			Win.Add (listView);

			label = new Label ("RadioGroup:") { X = Pos.X (label), Y = Pos.Bottom (listView) + 1 };
			Win.Add (label);
			var radioGroup = new RadioGroup (new ustring [] { "item #1", gitString, "Со_хранить", "𝔽𝕆𝕆𝔹𝔸ℝ" }, selected: 0) {
				X = 20,
				Y = Pos.Y (label),
				Width = Dim.Percent (60),
			};
			Win.Add (radioGroup);

			label = new Label ("TextField:") { X = Pos.X (label), Y = Pos.Bottom (radioGroup) + 1 };
			Win.Add (label);
			var textField = new TextField (gitString + " = Со_хранить") { X = 20, Y = Pos.Y (label), Width = Dim.Percent (60) };
			Win.Add (textField);

			label = new Label ("TextView:") { X = Pos.X (label), Y = Pos.Bottom (textField) + 1 };
			Win.Add (label);
			var textView = new TextView () {
				X = 20,
				Y = Pos.Y (label),
				Width = Dim.Percent (60),
				Height = 5,
				Text = unicode,
			};
			Win.Add (textView);
		}
	}
}
