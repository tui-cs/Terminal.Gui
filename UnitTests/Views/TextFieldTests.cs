using NStack;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests {
	public class TextFieldTests {
		readonly ITestOutputHelper output;

		public TextFieldTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		// This class enables test functions annotated with the [InitShutdown] attribute
		// to have a function called before the test function is called and after.
		// 
		// This is necessary because a) Application is a singleton and Init/Shutdown must be called
		// as a pair, and b) all unit test functions should be atomic.
		[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
		public class TextFieldTestsAutoInitShutdown : AutoInitShutdownAttribute {

			public override void Before (MethodInfo methodUnderTest)
			{
				base.Before (methodUnderTest);

				//                                                    1         2         3 
				//                                          01234567890123456789012345678901=32 (Length)
				TextFieldTests._textField = new TextField ("TAB to jump between text fields.");
			}

			public override void After (MethodInfo methodUnderTest)
			{
				TextFieldTests._textField = null;
				base.After (methodUnderTest);
			}
		}

		private static TextField _textField;

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void Changing_SelectedStart_Or_CursorPosition_Update_SelectedLength_And_SelectedText ()
		{
			_textField.SelectedStart = 2;
			Assert.Equal (32, _textField.CursorPosition);
			Assert.Equal (30, _textField.SelectedLength);
			Assert.Equal ("B to jump between text fields.", _textField.SelectedText);
			_textField.CursorPosition = 20;
			Assert.Equal (2, _textField.SelectedStart);
			Assert.Equal (18, _textField.SelectedLength);
			Assert.Equal ("B to jump between ", _textField.SelectedText);
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void SelectedStart_With_Value_Less_Than_Minus_One_Changes_To_Minus_One ()
		{
			_textField.SelectedStart = -2;
			Assert.Equal (-1, _textField.SelectedStart);
			Assert.Equal (0, _textField.SelectedLength);
			Assert.Null (_textField.SelectedText);
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void SelectedStart_With_Value_Greater_Than_Text_Length_Changes_To_Text_Length ()
		{
			_textField.CursorPosition = 2;
			_textField.SelectedStart = 33;
			Assert.Equal (32, _textField.SelectedStart);
			Assert.Equal (30, _textField.SelectedLength);
			Assert.Equal ("B to jump between text fields.", _textField.SelectedText);
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void SelectedStart_And_CursorPosition_With_Value_Greater_Than_Text_Length_Changes_Both_To_Text_Length ()
		{
			_textField.CursorPosition = 33;
			_textField.SelectedStart = 33;
			Assert.Equal (32, _textField.CursorPosition);
			Assert.Equal (32, _textField.SelectedStart);
			Assert.Equal (0, _textField.SelectedLength);
			Assert.Null (_textField.SelectedText);
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void SelectedStart_Greater_Than_CursorPosition_All_Selection_Is_Overwritten_On_Typing ()
		{
			_textField.SelectedStart = 19;
			_textField.CursorPosition = 12;
			Assert.Equal ("TAB to jump between text fields.", _textField.Text.ToString ());
			_textField.ProcessKey (new KeyEvent ((Key)0x75, new KeyModifiers ())); // u
			Assert.Equal ("TAB to jump u text fields.", _textField.Text.ToString ());
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void CursorPosition_With_Value_Less_Than_Zero_Changes_To_Zero ()
		{
			_textField.CursorPosition = -1;
			Assert.Equal (0, _textField.CursorPosition);
			Assert.Equal (0, _textField.SelectedLength);
			Assert.Null (_textField.SelectedText);
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void CursorPosition_With_Value_Greater_Than_Text_Length_Changes_To_Text_Length ()
		{
			_textField.CursorPosition = 33;
			Assert.Equal (32, _textField.CursorPosition);
			Assert.Equal (0, _textField.SelectedLength);
			Assert.Null (_textField.SelectedText);
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void WordForward_With_No_Selection ()
		{
			_textField.CursorPosition = 0;
			var iteration = 0;

			while (_textField.CursorPosition < _textField.Text.Length) {
				_textField.ProcessKey (new KeyEvent (Key.CursorRight | Key.CtrlMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (4, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 1:
					Assert.Equal (7, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 2:
					Assert.Equal (12, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 3:
					Assert.Equal (20, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 4:
					Assert.Equal (25, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 5:
					Assert.Equal (32, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void WordBackward_With_No_Selection ()
		{
			_textField.CursorPosition = _textField.Text.Length;
			var iteration = 0;

			while (_textField.CursorPosition > 0) {
				_textField.ProcessKey (new KeyEvent (Key.CursorLeft | Key.CtrlMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (31, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 1:
					Assert.Equal (25, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 2:
					Assert.Equal (20, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 3:
					Assert.Equal (12, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 4:
					Assert.Equal (7, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 5:
					Assert.Equal (4, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 6:
					Assert.Equal (0, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void WordForward_With_Selection ()
		{
			_textField.CursorPosition = 0;
			_textField.SelectedStart = 0;
			var iteration = 0;

			while (_textField.CursorPosition < _textField.Text.Length) {
				_textField.ProcessKey (new KeyEvent (Key.CursorRight | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (4, _textField.CursorPosition);
					Assert.Equal (0, _textField.SelectedStart);
					Assert.Equal (4, _textField.SelectedLength);
					Assert.Equal ("TAB ", _textField.SelectedText);
					break;
				case 1:
					Assert.Equal (7, _textField.CursorPosition);
					Assert.Equal (0, _textField.SelectedStart);
					Assert.Equal (7, _textField.SelectedLength);
					Assert.Equal ("TAB to ", _textField.SelectedText);
					break;
				case 2:
					Assert.Equal (12, _textField.CursorPosition);
					Assert.Equal (0, _textField.SelectedStart);
					Assert.Equal (12, _textField.SelectedLength);
					Assert.Equal ("TAB to jump ", _textField.SelectedText);
					break;
				case 3:
					Assert.Equal (20, _textField.CursorPosition);
					Assert.Equal (0, _textField.SelectedStart);
					Assert.Equal (20, _textField.SelectedLength);
					Assert.Equal ("TAB to jump between ", _textField.SelectedText);
					break;
				case 4:
					Assert.Equal (25, _textField.CursorPosition);
					Assert.Equal (0, _textField.SelectedStart);
					Assert.Equal (25, _textField.SelectedLength);
					Assert.Equal ("TAB to jump between text ", _textField.SelectedText);
					break;
				case 5:
					Assert.Equal (32, _textField.CursorPosition);
					Assert.Equal (0, _textField.SelectedStart);
					Assert.Equal (32, _textField.SelectedLength);
					Assert.Equal ("TAB to jump between text fields.", _textField.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void WordBackward_With_Selection ()
		{
			_textField.CursorPosition = _textField.Text.Length;
			_textField.SelectedStart = _textField.Text.Length;
			var iteration = 0;

			while (_textField.CursorPosition > 0) {
				_textField.ProcessKey (new KeyEvent (Key.CursorLeft | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (31, _textField.CursorPosition);
					Assert.Equal (32, _textField.SelectedStart);
					Assert.Equal (1, _textField.SelectedLength);
					Assert.Equal (".", _textField.SelectedText);
					break;
				case 1:
					Assert.Equal (25, _textField.CursorPosition);
					Assert.Equal (32, _textField.SelectedStart);
					Assert.Equal (7, _textField.SelectedLength);
					Assert.Equal ("fields.", _textField.SelectedText);
					break;
				case 2:
					Assert.Equal (20, _textField.CursorPosition);
					Assert.Equal (32, _textField.SelectedStart);
					Assert.Equal (12, _textField.SelectedLength);
					Assert.Equal ("text fields.", _textField.SelectedText);
					break;
				case 3:
					Assert.Equal (12, _textField.CursorPosition);
					Assert.Equal (32, _textField.SelectedStart);
					Assert.Equal (20, _textField.SelectedLength);
					Assert.Equal ("between text fields.", _textField.SelectedText);
					break;
				case 4:
					Assert.Equal (7, _textField.CursorPosition);
					Assert.Equal (32, _textField.SelectedStart);
					Assert.Equal (25, _textField.SelectedLength);
					Assert.Equal ("jump between text fields.", _textField.SelectedText);
					break;
				case 5:
					Assert.Equal (4, _textField.CursorPosition);
					Assert.Equal (32, _textField.SelectedStart);
					Assert.Equal (28, _textField.SelectedLength);
					Assert.Equal ("to jump between text fields.", _textField.SelectedText);
					break;
				case 6:
					Assert.Equal (0, _textField.CursorPosition);
					Assert.Equal (32, _textField.SelectedStart);
					Assert.Equal (32, _textField.SelectedLength);
					Assert.Equal ("TAB to jump between text fields.", _textField.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void WordForward_With_The_Same_Values_For_SelectedStart_And_CursorPosition_And_Not_Starting_At_Beginning_Of_The_Text ()
		{
			_textField.CursorPosition = 10;
			_textField.SelectedStart = 10;
			var iteration = 0;

			while (_textField.CursorPosition < _textField.Text.Length) {
				_textField.ProcessKey (new KeyEvent (Key.CursorRight | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (12, _textField.CursorPosition);
					Assert.Equal (10, _textField.SelectedStart);
					Assert.Equal (2, _textField.SelectedLength);
					Assert.Equal ("p ", _textField.SelectedText);
					break;
				case 1:
					Assert.Equal (20, _textField.CursorPosition);
					Assert.Equal (10, _textField.SelectedStart);
					Assert.Equal (10, _textField.SelectedLength);
					Assert.Equal ("p between ", _textField.SelectedText);
					break;
				case 2:
					Assert.Equal (25, _textField.CursorPosition);
					Assert.Equal (10, _textField.SelectedStart);
					Assert.Equal (15, _textField.SelectedLength);
					Assert.Equal ("p between text ", _textField.SelectedText);
					break;
				case 3:
					Assert.Equal (32, _textField.CursorPosition);
					Assert.Equal (10, _textField.SelectedStart);
					Assert.Equal (22, _textField.SelectedLength);
					Assert.Equal ("p between text fields.", _textField.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void WordBackward_With_The_Same_Values_For_SelectedStart_And_CursorPosition_And_Not_Starting_At_Beginning_Of_The_Text ()
		{
			_textField.CursorPosition = 10;
			_textField.SelectedStart = 10;
			var iteration = 0;

			while (_textField.CursorPosition > 0) {
				_textField.ProcessKey (new KeyEvent (Key.CursorLeft | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (7, _textField.CursorPosition);
					Assert.Equal (10, _textField.SelectedStart);
					Assert.Equal (3, _textField.SelectedLength);
					Assert.Equal ("jum", _textField.SelectedText);
					break;
				case 1:
					Assert.Equal (4, _textField.CursorPosition);
					Assert.Equal (10, _textField.SelectedStart);
					Assert.Equal (6, _textField.SelectedLength);
					Assert.Equal ("to jum", _textField.SelectedText);
					break;
				case 2:
					Assert.Equal (0, _textField.CursorPosition);
					Assert.Equal (10, _textField.SelectedStart);
					Assert.Equal (10, _textField.SelectedLength);
					Assert.Equal ("TAB to jum", _textField.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void WordForward_With_No_Selection_And_With_More_Than_Only_One_Whitespace_And_With_Only_One_Letter ()
		{
			//                           1         2         3         4         5    
			//                 0123456789012345678901234567890123456789012345678901234=55 (Length)
			_textField.Text = "TAB   t  o  jump         b  etween    t ext   f ields .";
			_textField.CursorPosition = 0;
			var iteration = 0;

			while (_textField.CursorPosition < _textField.Text.Length) {
				_textField.ProcessKey (new KeyEvent (Key.CursorRight | Key.CtrlMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (6, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 1:
					Assert.Equal (9, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 2:
					Assert.Equal (12, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 3:
					Assert.Equal (25, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 4:
					Assert.Equal (28, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 5:
					Assert.Equal (38, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 6:
					Assert.Equal (40, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 7:
					Assert.Equal (46, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 8:
					Assert.Equal (48, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 9:
					Assert.Equal (54, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 10:
					Assert.Equal (55, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void WordBackward_With_No_Selection_And_With_More_Than_Only_One_Whitespace_And_With_Only_One_Letter ()
		{
			//                           1         2         3         4         5    
			//                 0123456789012345678901234567890123456789012345678901234=55 (Length)
			_textField.Text = "TAB   t  o  jump         b  etween    t ext   f ields .";
			_textField.CursorPosition = _textField.Text.Length;
			var iteration = 0;

			while (_textField.CursorPosition > 0) {
				_textField.ProcessKey (new KeyEvent (Key.CursorLeft | Key.CtrlMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (54, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 1:
					Assert.Equal (48, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 2:
					Assert.Equal (46, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 3:
					Assert.Equal (40, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 4:
					Assert.Equal (38, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 5:
					Assert.Equal (28, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 6:
					Assert.Equal (25, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 7:
					Assert.Equal (12, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 8:
					Assert.Equal (9, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 9:
					Assert.Equal (6, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				case 10:
					Assert.Equal (0, _textField.CursorPosition);
					Assert.Equal (-1, _textField.SelectedStart);
					Assert.Equal (0, _textField.SelectedLength);
					Assert.Null (_textField.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void Copy_Or_Cut_Null_If_No_Selection ()
		{
			_textField.SelectedStart = -1;
			_textField.Copy ();
			Assert.Null (_textField.SelectedText);
			_textField.Cut ();
			Assert.Null (_textField.SelectedText);
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void Copy_Or_Cut_Not_Null_If_Has_Selection ()
		{
			_textField.SelectedStart = 20;
			_textField.CursorPosition = 24;
			_textField.Copy ();
			Assert.Equal ("text", _textField.SelectedText);
			_textField.Cut ();
			Assert.Null (_textField.SelectedText);
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void Copy_Or_Cut_And_Paste_With_Selection ()
		{
			_textField.SelectedStart = 20;
			_textField.CursorPosition = 24;
			_textField.Copy ();
			Assert.Equal ("text", _textField.SelectedText);
			Assert.Equal ("TAB to jump between text fields.", _textField.Text.ToString ());
			_textField.Paste ();
			Assert.Equal ("TAB to jump between text fields.", _textField.Text.ToString ());
			_textField.SelectedStart = 20;
			_textField.Cut ();
			_textField.Paste ();
			Assert.Equal ("TAB to jump between text fields.", _textField.Text.ToString ());
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void Copy_Or_Cut_And_Paste_With_No_Selection ()
		{
			_textField.SelectedStart = 20;
			_textField.CursorPosition = 24;
			_textField.Copy ();
			Assert.Equal ("text", _textField.SelectedText);
			Assert.Equal ("TAB to jump between text fields.", _textField.Text.ToString ());
			_textField.SelectedStart = -1;
			_textField.Paste ();
			Assert.Equal ("TAB to jump between texttext fields.", _textField.Text.ToString ());
			_textField.SelectedStart = 24;
			_textField.Cut ();
			Assert.Null (_textField.SelectedText);
			Assert.Equal ("TAB to jump between text fields.", _textField.Text.ToString ());
			_textField.SelectedStart = -1;
			_textField.Paste ();
			Assert.Equal ("TAB to jump between texttext fields.", _textField.Text.ToString ());
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void Copy_Or_Cut__Not_Allowed_If_Secret_Is_True ()
		{
			_textField.Secret = true;
			_textField.SelectedStart = 20;
			_textField.CursorPosition = 24;
			_textField.Copy ();
			Assert.Null (_textField.SelectedText);
			_textField.Cut ();
			Assert.Null (_textField.SelectedText);
			_textField.Secret = false;
			_textField.Copy ();
			Assert.Equal ("text", _textField.SelectedText);
			_textField.Cut ();
			Assert.Null (_textField.SelectedText);
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void Paste_Always_Clear_The_SelectedText ()
		{
			_textField.SelectedStart = 20;
			_textField.CursorPosition = 24;
			_textField.Copy ();
			Assert.Equal ("text", _textField.SelectedText);
			_textField.Paste ();
			Assert.Null (_textField.SelectedText);
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void TextChanging_Event ()
		{
			bool cancel = true;

			_textField.TextChanging += (s, e) => {
				Assert.Equal ("changing", e.NewText);
				if (cancel) {
					e.Cancel = true;
				}
			};

			_textField.Text = "changing";
			Assert.Equal ("TAB to jump between text fields.", _textField.Text.ToString ());
			cancel = false;
			_textField.Text = "changing";
			Assert.Equal ("changing", _textField.Text.ToString ());
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void TextChanged_Event ()
		{
			_textField.TextChanged += (s, e) => {
				Assert.Equal ("TAB to jump between text fields.", e.OldValue);
			};

			_textField.Text = "changed";
			Assert.Equal ("changed", _textField.Text.ToString ());
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void Used_Is_True_By_Default ()
		{
			_textField.CursorPosition = 10;
			Assert.Equal ("TAB to jump between text fields.", _textField.Text.ToString ());
			_textField.ProcessKey (new KeyEvent ((Key)0x75, new KeyModifiers ())); // u
			Assert.Equal ("TAB to jumup between text fields.", _textField.Text.ToString ());
			_textField.ProcessKey (new KeyEvent ((Key)0x73, new KeyModifiers ())); // s
			Assert.Equal ("TAB to jumusp between text fields.", _textField.Text.ToString ());
			_textField.ProcessKey (new KeyEvent ((Key)0x65, new KeyModifiers ())); // e
			Assert.Equal ("TAB to jumusep between text fields.", _textField.Text.ToString ());
			_textField.ProcessKey (new KeyEvent ((Key)0x64, new KeyModifiers ())); // d
			Assert.Equal ("TAB to jumusedp between text fields.", _textField.Text.ToString ());
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void Used_Is_False ()
		{
			_textField.Used = false;
			_textField.CursorPosition = 10;
			Assert.Equal ("TAB to jump between text fields.", _textField.Text.ToString ());
			_textField.ProcessKey (new KeyEvent ((Key)0x75, new KeyModifiers ())); // u
			Assert.Equal ("TAB to jumu between text fields.", _textField.Text.ToString ());
			_textField.ProcessKey (new KeyEvent ((Key)0x73, new KeyModifiers ())); // s
			Assert.Equal ("TAB to jumusbetween text fields.", _textField.Text.ToString ());
			_textField.ProcessKey (new KeyEvent ((Key)0x65, new KeyModifiers ())); // e
			Assert.Equal ("TAB to jumuseetween text fields.", _textField.Text.ToString ());
			_textField.ProcessKey (new KeyEvent ((Key)0x64, new KeyModifiers ())); // d
			Assert.Equal ("TAB to jumusedtween text fields.", _textField.Text.ToString ());
		}

		[Fact]
		public void ProcessKey_Backspace_From_End ()
		{
			var tf = new TextField ("ABC");
			tf.EnsureFocus ();
			Assert.Equal ("ABC", tf.Text.ToString ());
			Assert.Equal (3, tf.CursorPosition);

			// now delete the C
			tf.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ()));
			Assert.Equal ("AB", tf.Text.ToString ());
			Assert.Equal (2, tf.CursorPosition);

			// then delete the B
			tf.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ()));
			Assert.Equal ("A", tf.Text.ToString ());
			Assert.Equal (1, tf.CursorPosition);

			// then delete the A
			tf.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ()));
			Assert.Equal ("", tf.Text.ToString ());
			Assert.Equal (0, tf.CursorPosition);
		}

		[Fact]
		public void ProcessKey_Backspace_From_Middle ()
		{
			var tf = new TextField ("ABC");
			tf.EnsureFocus ();
			tf.CursorPosition = 2;
			Assert.Equal ("ABC", tf.Text.ToString ());

			// now delete the B
			tf.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ()));
			Assert.Equal ("AC", tf.Text.ToString ());

			// then delete the A
			tf.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ()));
			Assert.Equal ("C", tf.Text.ToString ());

			// then delete nothing
			tf.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ()));
			Assert.Equal ("C", tf.Text.ToString ());

			// now delete the C
			tf.CursorPosition = 1;
			tf.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ()));
			Assert.Equal ("", tf.Text.ToString ());
		}

		[Fact]
		public void Cancel_TextChanging_ThenBackspace ()
		{
			var tf = new TextField ();
			tf.EnsureFocus ();
			tf.ProcessKey (new KeyEvent (Key.A, new KeyModifiers ()));
			Assert.Equal ("A", tf.Text.ToString ());

			// cancel the next keystroke
			tf.TextChanging += (s, e) => e.Cancel = e.NewText == "AB";
			tf.ProcessKey (new KeyEvent (Key.B, new KeyModifiers ()));

			// B was canceled so should just be A
			Assert.Equal ("A", tf.Text.ToString ());

			// now delete the A
			tf.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ()));

			Assert.Equal ("", tf.Text.ToString ());
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void Text_Replaces_Tabs_With_Empty_String ()
		{
			_textField.Text = "\t\tTAB to jump between text fields.";
			Assert.Equal ("TAB to jump between text fields.", _textField.Text.ToString ());
			_textField.Text = "";
			Clipboard.Contents = "\t\tTAB to jump between text fields.";
			_textField.Paste ();
			Assert.Equal ("TAB to jump between text fields.", _textField.Text.ToString ());
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void TextField_SpaceHandling ()
		{
			var tf = new TextField () {
				Width = 10,
				Text = " "
			};

			MouseEvent ev = new MouseEvent () {
				X = 0,
				Y = 0,
				Flags = MouseFlags.Button1DoubleClicked,
			};

			tf.MouseEvent (ev);
			Assert.Equal (1, tf.SelectedLength);

			ev = new MouseEvent () {
				X = 1,
				Y = 0,
				Flags = MouseFlags.Button1DoubleClicked,
			};

			tf.MouseEvent (ev);
			Assert.Equal (1, tf.SelectedLength);
		}

		[Fact]
		[TextFieldTestsAutoInitShutdown]
		public void CanFocus_False_Wont_Focus_With_Mouse ()
		{
			var top = Application.Top;
			var tf = new TextField () {
				Width = Dim.Fill (),
				CanFocus = false,
				ReadOnly = true,
				Text = "some text"
			};
			var fv = new FrameView ("I shouldn't get focus") {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				CanFocus = false,
			};
			fv.Add (tf);
			top.Add (fv);

			Application.Begin (top);

			Assert.False (tf.CanFocus);
			Assert.False (tf.HasFocus);
			Assert.False (fv.CanFocus);
			Assert.False (fv.HasFocus);

			tf.MouseEvent (new MouseEvent () {
				X = 1,
				Y = 0,
				Flags = MouseFlags.Button1DoubleClicked
			});

			Assert.Null (tf.SelectedText);
			Assert.False (tf.CanFocus);
			Assert.False (tf.HasFocus);
			Assert.False (fv.CanFocus);
			Assert.False (fv.HasFocus);

			Assert.Throws<InvalidOperationException> (() => tf.CanFocus = true);
			fv.CanFocus = true;
			tf.CanFocus = true;
			tf.MouseEvent (new MouseEvent () {
				X = 1,
				Y = 0,
				Flags = MouseFlags.Button1DoubleClicked
			});

			Assert.Equal ("some ", tf.SelectedText);
			Assert.True (tf.CanFocus);
			Assert.True (tf.HasFocus);
			Assert.True (fv.CanFocus);
			Assert.True (fv.HasFocus);

			fv.CanFocus = false;
			tf.MouseEvent (new MouseEvent () {
				X = 1,
				Y = 0,
				Flags = MouseFlags.Button1DoubleClicked
			});

			Assert.Equal ("some ", tf.SelectedText); // Setting CanFocus to false don't change the SelectedText
			Assert.False (tf.CanFocus);
			Assert.False (tf.HasFocus);
			Assert.False (fv.CanFocus);
			Assert.False (fv.HasFocus);
		}

		[Fact]
		[AutoInitShutdown]
		public void KeyBindings_Command ()
		{
			var tf = new TextField ("This is a test.") { Width = 20 };
			Assert.Equal (15, tf.Text.Length);
			Assert.Equal (15, tf.CursorPosition);
			Assert.False (tf.ReadOnly);

			Assert.True (tf.ProcessKey (new KeyEvent (Key.DeleteChar, new KeyModifiers ())));
			Assert.Equal ("This is a test.", tf.Text.ToString ());
			tf.CursorPosition = 0;
			Assert.True (tf.ProcessKey (new KeyEvent (Key.DeleteChar, new KeyModifiers ())));
			Assert.Equal ("his is a test.", tf.Text.ToString ());
			tf.ReadOnly = true;
			Assert.True (tf.ProcessKey (new KeyEvent (Key.D | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("his is a test.", tf.Text.ToString ());
			Assert.True (tf.ProcessKey (new KeyEvent (Key.Delete, new KeyModifiers ())));
			Assert.Equal ("his is a test.", tf.Text.ToString ());
			tf.ReadOnly = false;
			tf.CursorPosition = 1;
			Assert.True (tf.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			tf.CursorPosition = 5;
			Assert.True (tf.ProcessKey (new KeyEvent (Key.Home | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.Equal ("is is", tf.SelectedText);
			tf.CursorPosition = 5;
			tf.SelectedStart = -1;
			Assert.Null (tf.SelectedText);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.Home | Key.ShiftMask | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.Equal ("is is", tf.SelectedText);
			tf.CursorPosition = 5;
			tf.SelectedStart = -1;
			Assert.Null (tf.SelectedText);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.A | Key.ShiftMask | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.Equal ("is is", tf.SelectedText);
			tf.CursorPosition = 5;
			tf.SelectedStart = -1;
			Assert.Null (tf.SelectedText);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.End | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.Equal (" a test.", tf.SelectedText);
			tf.CursorPosition = 5;
			tf.SelectedStart = -1;
			Assert.Null (tf.SelectedText);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.End | Key.ShiftMask | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.Equal (" a test.", tf.SelectedText);
			tf.CursorPosition = 5;
			tf.SelectedStart = -1;
			Assert.Null (tf.SelectedText);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.E | Key.ShiftMask | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.Equal (" a test.", tf.SelectedText);
			tf.CursorPosition = 5;
			tf.SelectedStart = -1;
			Assert.Null (tf.SelectedText);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.Equal (0, tf.CursorPosition);
			tf.CursorPosition = 5;
			Assert.Null (tf.SelectedText);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.Home | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.Equal (0, tf.CursorPosition);
			tf.CursorPosition = 5;
			Assert.Null (tf.SelectedText);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.A | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.Equal (0, tf.CursorPosition);
			tf.CursorPosition = 5;
			tf.SelectedStart = -1;
			Assert.Null (tf.SelectedText);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.CursorLeft | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.Equal ("s", tf.SelectedText);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.CursorUp | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.Equal ("is", tf.SelectedText);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.CursorRight | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.Equal ("s", tf.SelectedText);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.CursorDown | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.Null (tf.SelectedText);
			tf.CursorPosition = 7;
			tf.SelectedStart = -1;
			Assert.Null (tf.SelectedText);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.CursorLeft | Key.ShiftMask | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.Equal ("a", tf.SelectedText);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.CursorUp | Key.ShiftMask | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.Equal ("is a", tf.SelectedText);
			Assert.True (tf.ProcessKey (new KeyEvent ((Key)((int)'B' + Key.ShiftMask | Key.AltMask), new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.Equal ("is is a", tf.SelectedText);
			tf.CursorPosition = 3;
			tf.SelectedStart = -1;
			Assert.Null (tf.SelectedText);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.CursorRight | Key.ShiftMask | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.Equal ("is ", tf.SelectedText);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.CursorDown | Key.ShiftMask | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.Equal ("is a ", tf.SelectedText);
			Assert.True (tf.ProcessKey (new KeyEvent ((Key)((int)'F' + Key.ShiftMask | Key.AltMask), new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.Equal ("is a test.", tf.SelectedText);
			Assert.Equal (13, tf.CursorPosition);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.Null (tf.SelectedText);
			Assert.Equal (12, tf.CursorPosition);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.Equal (11, tf.CursorPosition);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.Equal (13, tf.CursorPosition);
			tf.CursorPosition = 0;
			Assert.True (tf.ProcessKey (new KeyEvent (Key.End | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.Equal (13, tf.CursorPosition);
			tf.CursorPosition = 0;
			Assert.True (tf.ProcessKey (new KeyEvent (Key.E | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.Equal (13, tf.CursorPosition);
			tf.CursorPosition = 0;
			Assert.True (tf.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.Equal (1, tf.CursorPosition);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.F | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.Equal (2, tf.CursorPosition);
			tf.CursorPosition = 9;
			tf.ReadOnly = true;
			Assert.True (tf.ProcessKey (new KeyEvent (Key.K | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			tf.ReadOnly = false;
			Assert.True (tf.ProcessKey (new KeyEvent (Key.K | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("is is a t", tf.Text.ToString ());
			Assert.Equal ("est.", Clipboard.Contents.ToString ());
			Assert.True (tf.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.True (tf.ProcessKey (new KeyEvent (Key.Y | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("is is a t", tf.Text.ToString ());
			Assert.True (tf.ProcessKey (new KeyEvent (Key.Backspace | Key.AltMask, new KeyModifiers ())));
			Assert.Equal ("is is a test.", tf.Text.ToString ());
			Assert.True (tf.ProcessKey (new KeyEvent (Key.Y | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("is is a t", tf.Text.ToString ());
			Assert.True (tf.ProcessKey (new KeyEvent (Key.CursorLeft | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("is is a t", tf.Text.ToString ());
			Assert.Equal (8, tf.CursorPosition);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.CursorUp | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("is is a t", tf.Text.ToString ());
			Assert.Equal (6, tf.CursorPosition);
			Assert.True (tf.ProcessKey (new KeyEvent ((Key)((int)'B' + Key.AltMask), new KeyModifiers ())));
			Assert.Equal ("is is a t", tf.Text.ToString ());
			Assert.Equal (3, tf.CursorPosition);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.CursorRight | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("is is a t", tf.Text.ToString ());
			Assert.Equal (6, tf.CursorPosition);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.CursorDown | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("is is a t", tf.Text.ToString ());
			Assert.Equal (8, tf.CursorPosition);
			Assert.True (tf.ProcessKey (new KeyEvent ((Key)((int)'F' + Key.AltMask), new KeyModifiers ())));
			Assert.Equal ("is is a t", tf.Text.ToString ());
			Assert.Equal (9, tf.CursorPosition);
			Assert.True (tf.Used);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.InsertChar, new KeyModifiers ())));
			Assert.Equal ("is is a t", tf.Text.ToString ());
			Assert.Equal (9, tf.CursorPosition);
			Assert.False (tf.Used);
			tf.SelectedStart = 3;
			tf.CursorPosition = 7;
			Assert.Equal ("is a", tf.SelectedText);
			Assert.Equal ("est.", Clipboard.Contents.ToString ());
			Assert.True (tf.ProcessKey (new KeyEvent (Key.C | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("is is a t", tf.Text.ToString ());
			Assert.Equal ("is a", Clipboard.Contents.ToString ());
			Assert.True (tf.ProcessKey (new KeyEvent (Key.X | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("is  t", tf.Text.ToString ());
			Assert.Equal ("is a", Clipboard.Contents.ToString ());
			Assert.True (tf.ProcessKey (new KeyEvent (Key.V | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("is is a t", tf.Text.ToString ());
			Assert.Equal ("is a", Clipboard.Contents.ToString ());
			Assert.Equal (7, tf.CursorPosition);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.K | Key.AltMask, new KeyModifiers ())));
			Assert.Equal (" t", tf.Text.ToString ());
			Assert.Equal ("is is a", Clipboard.Contents.ToString ());
			tf.Text = "TAB to jump between text fields.";
			Assert.Equal (0, tf.CursorPosition);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.DeleteChar | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("to jump between text fields.", tf.Text.ToString ());
			tf.CursorPosition = tf.Text.Length;
			Assert.True (tf.ProcessKey (new KeyEvent (Key.Backspace | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("to jump between text fields", tf.Text.ToString ());
			Assert.True (tf.ProcessKey (new KeyEvent (Key.T | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("to jump between text fields", tf.SelectedText);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.D | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal ("", tf.Text.ToString ());
		}

		[Fact]
		[AutoInitShutdown]
		public void Adjust_First ()
		{
			TextField tf = new TextField () {
				Width = Dim.Fill (),
				Text = "This is a test."
			};
			Application.Top.Add (tf);
			Application.Begin (Application.Top);

			Assert.Equal ("This is a test. ", GetContents ());

			string GetContents ()
			{
				var item = "";
				for (int i = 0; i < 16; i++) {
					item += (char)Application.Driver.Contents [0, i, 0];
				}
				return item;
			}
		}

		[Fact, AutoInitShutdown]
		public void DeleteSelectedText_InsertText_DeleteCharLeft_DeleteCharRight_Cut ()
		{
			var newText = "";
			var oldText = "";
			var tf = new TextField () { Width = 10, Text = "-1" };

			tf.TextChanging += (s, e) => newText = e.NewText.ToString ();
			tf.TextChanged += (s, e) => oldText = e.OldValue.ToString ();

			Application.Top.Add (tf);
			Application.Begin (Application.Top);

			Assert.Equal ("-1", tf.Text.ToString ());

			// InsertText
			tf.SelectedStart = 1;
			tf.CursorPosition = 2;
			Assert.Equal (1, tf.SelectedLength);
			Assert.Equal ("1", tf.SelectedText);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.D2, new KeyModifiers ())));
			Assert.Equal ("-2", newText);
			Assert.Equal ("-1", oldText);
			Assert.Equal ("-2", tf.Text.ToString ());

			// DeleteCharLeft
			tf.SelectedStart = 1;
			tf.CursorPosition = 2;
			Assert.Equal (1, tf.SelectedLength);
			Assert.Equal ("2", tf.SelectedText);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ())));
			Assert.Equal ("-", newText);
			Assert.Equal ("-2", oldText);
			Assert.Equal ("-", tf.Text.ToString ());

			// DeleteCharRight
			tf.Text = "-1";
			tf.SelectedStart = 1;
			tf.CursorPosition = 2;
			Assert.Equal (1, tf.SelectedLength);
			Assert.Equal ("1", tf.SelectedText);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.DeleteChar, new KeyModifiers ())));
			Assert.Equal ("-", newText);
			Assert.Equal ("-1", oldText);
			Assert.Equal ("-", tf.Text.ToString ());

			// Cut
			tf.Text = "-1";
			tf.SelectedStart = 1;
			tf.CursorPosition = 2;
			Assert.Equal (1, tf.SelectedLength);
			Assert.Equal ("1", tf.SelectedText);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.X | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("-", newText);
			Assert.Equal ("-1", oldText);
			Assert.Equal ("-", tf.Text.ToString ());

			// Delete word with accented char
			tf.Text = "Les Misérables movie.";
			Assert.True (tf.MouseEvent (new MouseEvent {
				X = 7,
				Y = 1,
				Flags = MouseFlags.Button1DoubleClicked,
				View = tf
			}));
			Assert.Equal ("Misérables ", tf.SelectedText);
			Assert.Equal (11, tf.SelectedLength);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.Delete, new KeyModifiers ())));
			Assert.Equal ("Les movie.", newText);
			Assert.Equal ("Les Misérables movie.", oldText);
			Assert.Equal ("Les movie.", tf.Text.ToString ());
		}

		[Fact]
		[AutoInitShutdown]
		public void Test_RootKeyEvent_Cancel ()
		{
			Application.RootKeyEvent += SuppressKey;

			var tf = new TextField ();

			Application.Top.Add (tf);
			Application.Begin (Application.Top);

			Application.Driver.SendKeys ('a', ConsoleKey.A, false, false, false);
			Assert.Equal ("a", tf.Text.ToString ());

			// SuppressKey suppresses the 'j' key
			Application.Driver.SendKeys ('j', ConsoleKey.A, false, false, false);
			Assert.Equal ("a", tf.Text.ToString ());

			Application.RootKeyEvent -= SuppressKey;

			// Now that the delegate has been removed we can type j again
			Application.Driver.SendKeys ('j', ConsoleKey.A, false, false, false);
			Assert.Equal ("aj", tf.Text.ToString ());
		}
		[Fact]
		[AutoInitShutdown]
		public void Test_RootMouseKeyEvent_Cancel ()
		{
			Application.RootMouseEvent += SuppressRightClick;

			var tf = new TextField () { Width = 10 };
			int clickCounter = 0;
			tf.MouseClick += (s, m) => { clickCounter++; };

			Application.Top.Add (tf);
			Application.Begin (Application.Top);

			var processMouseEventMethod = typeof (Application).GetMethod ("ProcessMouseEvent", BindingFlags.Static | BindingFlags.NonPublic)
				?? throw new Exception ("Expected private method not found 'ProcessMouseEvent', this method was used for testing mouse behaviours");

			var mouseEvent = new MouseEvent {
				Flags = MouseFlags.Button1Clicked,
				View = tf
			};

			processMouseEventMethod.Invoke (null, new object [] { mouseEvent });
			Assert.Equal (1, clickCounter);

			// Get a fresh instance that represents a right click.
			// Should be ignored because of SuppressRightClick callback
			mouseEvent = new MouseEvent {
				Flags = MouseFlags.Button3Clicked,
				View = tf
			};
			processMouseEventMethod.Invoke (null, new object [] { mouseEvent });
			Assert.Equal (1, clickCounter);

			Application.RootMouseEvent -= SuppressRightClick;

			// Get a fresh instance that represents a right click.
			// Should no longer be ignored as the callback was removed
			mouseEvent = new MouseEvent {
				Flags = MouseFlags.Button3Clicked,
				View = tf
			};

			processMouseEventMethod.Invoke (null, new object [] { mouseEvent });
			Assert.Equal (2, clickCounter);
		}

		private bool SuppressKey (KeyEvent arg)
		{
			if (arg.KeyValue == 'j')
				return true;

			return false;
		}

		private void SuppressRightClick (MouseEvent arg)
		{
			if (arg.Flags.HasFlag (MouseFlags.Button3Clicked))
				arg.Handled = true;
		}

		[Fact, AutoInitShutdown]
		public void ScrollOffset_Initialize ()
		{
			var tf = new TextField ("Testing Scrolls.") {
				X = 1,
				Y = 1,
				Width = 20
			};
			Assert.Equal (0, tf.ScrollOffset);
			Assert.Equal (16, tf.CursorPosition);

			Application.Top.Add (tf);
			Application.Begin (Application.Top);

			Assert.Equal (0, tf.ScrollOffset);
			Assert.Equal (16, tf.CursorPosition);
		}

		[Fact]
		public void HistoryText_IsDirty_ClearHistoryChanges ()
		{
			var text = "Testing";
			var tf = new TextField (text);

			Assert.Equal (text, tf.Text);
			tf.ClearHistoryChanges ();
			Assert.False (tf.IsDirty);

			Assert.True (tf.ProcessKey (new KeyEvent (Key.A, new KeyModifiers ())));
			Assert.Equal ($"{text}A", tf.Text);
			Assert.True (tf.IsDirty);
		}

		[InlineData ("a")] // Lower than selection
		[InlineData ("aaaaaaaaaaa")] // Greater than selection
		[InlineData ("aaaa")] // Equal than selection
		[Theory]
		public void TestSetTextAndMoveCursorToEnd_WhenExistingSelection (string newText)
		{
			var tf = new TextField ();
			tf.Text = "fish";
			tf.CursorPosition = tf.Text.Length;

			tf.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));

			tf.ProcessKey (new KeyEvent (Key.CursorLeft | Key.ShiftMask, new KeyModifiers { Shift = true }));
			tf.ProcessKey (new KeyEvent (Key.CursorLeft | Key.ShiftMask, new KeyModifiers { Shift = true }));

			Assert.Equal (1, tf.CursorPosition);
			Assert.Equal (2, tf.SelectedLength);
			Assert.Equal ("is", tf.SelectedText);

			tf.Text = newText;
			tf.CursorPosition = tf.Text.Length;

			Assert.Equal (newText.Length, tf.CursorPosition);
			Assert.Equal (0, tf.SelectedLength);
			Assert.Null (tf.SelectedText);
		}

		[Fact]
		public void WordBackward_WordForward_SelectedText_With_Accent ()
		{
			string text = "Les Misérables movie.";
			var tf = new TextField (text) { Width = 30 };

			Assert.Equal (21, text.Length);
			Assert.Equal (21, tf.Text.RuneCount);
			Assert.Equal (21, tf.Text.ConsoleWidth);

			var runes = tf.Text.ToRuneList ();
			Assert.Equal (21, runes.Count);
			Assert.Equal (22, tf.Text.Length);

			for (int i = 0; i < runes.Count; i++) {
				var cs = text [i];
				var cus = (char)runes [i];
				Assert.Equal (cs, cus);
			}

			var idx = 15;
			Assert.Equal ('m', text [idx]);
			Assert.Equal ('m', (char)runes [idx]);
			Assert.Equal ("m", ustring.Make (runes [idx]));

			Assert.True (tf.MouseEvent (new MouseEvent {
				X = idx,
				Y = 1,
				Flags = MouseFlags.Button1DoubleClicked,
				View = tf
			}));
			Assert.Equal ("movie.", tf.SelectedText);

			Assert.True (tf.MouseEvent (new MouseEvent {
				X = idx + 1,
				Y = 1,
				Flags = MouseFlags.Button1DoubleClicked,
				View = tf
			}));
			Assert.Equal ("movie.", tf.SelectedText);
		}

		[Fact, AutoInitShutdown]
		public void Words_With_Accents_Incorrect_Order_Will_Result_With_Wrong_Accent_Place ()
		{
			var tf = new TextField ("Les Misérables") { Width = 30 };
			var top = Application.Top;
			top.Add (tf);
			Application.Begin (top);

			TestHelpers.AssertDriverContentsWithFrameAre (@"
Les Misérables", output);

			tf.Text = "Les Mise" + char.ConvertFromUtf32 (int.Parse ("0301", NumberStyles.HexNumber)) + "rables";
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
Les Misérables", output);

			// incorrect order will result with a wrong accent place
			tf.Text = "Les Mis" + char.ConvertFromUtf32 (int.Parse ("0301", NumberStyles.HexNumber)) + "erables";
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
Les Miśerables", output);
		}

		[Fact, AutoInitShutdown]
		public void Accented_Letter_With_Three_Combining_Unicode_Chars ()
		{
			var tf = new TextField ("ắ") { Width = 3 };
			var top = Application.Top;
			top.Add (tf);
			Application.Begin (top);

			TestHelpers.AssertDriverContentsWithFrameAre (@"
ắ", output);

			tf.Text = "\u1eaf";
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
ắ", output);

			tf.Text = "\u0103\u0301";
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
ắ", output);

			tf.Text = "\u0061\u0306\u0301";
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
ắ", output);
		}

		[Fact, AutoInitShutdown]
		public void CaptionedTextField_RendersCaption_WhenNotFocused ()
		{
			var tf = GetTextFieldsInView ();

			tf.Redraw (tf.Bounds);
			TestHelpers.AssertDriverContentsAre ("", output);

			// Caption has no effect when focused
			tf.Caption = "Enter txt";
			Assert.True (tf.HasFocus);
			tf.Redraw (tf.Bounds);
			TestHelpers.AssertDriverContentsAre ("", output);

			Application.Driver.SendKeys ('\t', ConsoleKey.Tab, false, false, false);

			Assert.False (tf.HasFocus);
			tf.Redraw (tf.Bounds);
			TestHelpers.AssertDriverContentsAre ("Enter txt", output);
		}

		[Theory, AutoInitShutdown]
		[InlineData ("blah")]
		[InlineData (" ")]
		public void CaptionedTextField_DoNotRenderCaption_WhenTextPresent (string content)
		{
			var tf = GetTextFieldsInView ();

			tf.Redraw (tf.Bounds);
			TestHelpers.AssertDriverContentsAre ("", output);

			tf.Caption = "Enter txt";
			Application.Driver.SendKeys ('\t', ConsoleKey.Tab, false, false, false);

			// Caption should appear when not focused and no text
			Assert.False (tf.HasFocus);
			tf.Redraw (tf.Bounds);
			TestHelpers.AssertDriverContentsAre ("Enter txt", output);

			// but disapear when text is added
			tf.Text = content;
			tf.Redraw (tf.Bounds);
			TestHelpers.AssertDriverContentsAre (content, output);
		}

		[Fact, AutoInitShutdown]
		public void CaptionedTextField_DoesNotOverspillBounds_Unicode ()
		{
			var caption = "Mise" + Char.ConvertFromUtf32 (Int32.Parse ("0301", NumberStyles.HexNumber)) + "rables";

			Assert.Equal (11, caption.Length);
			Assert.Equal (10, caption.Sum (c => Rune.ColumnWidth (c)));

			var tf = GetTextFieldsInView ();

			tf.Caption = caption;
			Application.Driver.SendKeys ('\t', ConsoleKey.Tab, false, false, false);
			Assert.False (tf.HasFocus);

			tf.Redraw (tf.Bounds);
			TestHelpers.AssertDriverContentsAre ("Misérables", output);
		}

		[Theory, AutoInitShutdown]
		[InlineData ("0123456789", "0123456789")]
		[InlineData ("01234567890", "0123456789")]
		public void CaptionedTextField_DoesNotOverspillBounds (string caption, string expectedRender)
		{
			var tf = GetTextFieldsInView ();
			// Caption has no effect when focused
			tf.Caption = caption;
			Application.Driver.SendKeys ('\t', ConsoleKey.Tab, false, false, false);
			Assert.False (tf.HasFocus);

			tf.Redraw (tf.Bounds);
			TestHelpers.AssertDriverContentsAre (expectedRender, output);
		}

		private TextField GetTextFieldsInView ()
		{
			var tf = new TextField {
				Width = 10
			};
			var tf2 = new TextField {
				Y = 1,
				Width = 10
			};

			var top = Application.Top;
			top.Add (tf);
			top.Add (tf2);

			Application.Begin (top);

			Assert.Same (tf, top.Focused);

			return tf;
		}

		[Fact]
		public void OnEnter_Does_Not_Throw_If_Not_IsInitialized_SetCursorVisibility ()
		{
			var top = new Toplevel ();
			var tf = new TextField () { Width = 10 };
			top.Add (tf);

			var exception = Record.Exception (tf.SetFocus);
			Assert.Null (exception);
		}

		[Fact]
		public void WordBackward_WordForward_Mixed ()
		{
			var tf = new TextField ("Test with0. and!.?;-@+") { Width = 30 };
			tf.ProcessKey (new KeyEvent (Key.CtrlMask | Key.CursorLeft, new KeyModifiers () { Ctrl = true }));
			Assert.Equal (15, tf.CursorPosition);
			tf.ProcessKey (new KeyEvent (Key.CtrlMask | Key.CursorLeft, new KeyModifiers () { Ctrl = true }));
			Assert.Equal (12, tf.CursorPosition);
			tf.ProcessKey (new KeyEvent (Key.CtrlMask | Key.CursorLeft, new KeyModifiers () { Ctrl = true }));
			Assert.Equal (10, tf.CursorPosition);
			tf.ProcessKey (new KeyEvent (Key.CtrlMask | Key.CursorLeft, new KeyModifiers () { Ctrl = true }));
			Assert.Equal (5, tf.CursorPosition);
			tf.ProcessKey (new KeyEvent (Key.CtrlMask | Key.CursorLeft, new KeyModifiers () { Ctrl = true }));
			Assert.Equal (0, tf.CursorPosition);

			tf.ProcessKey (new KeyEvent (Key.CtrlMask | Key.CursorRight, new KeyModifiers () { Ctrl = true }));
			Assert.Equal (5, tf.CursorPosition);
			tf.ProcessKey (new KeyEvent (Key.CtrlMask | Key.CursorRight, new KeyModifiers () { Ctrl = true }));
			Assert.Equal (10, tf.CursorPosition);
			tf.ProcessKey (new KeyEvent (Key.CtrlMask | Key.CursorRight, new KeyModifiers () { Ctrl = true }));
			Assert.Equal (12, tf.CursorPosition);
			tf.ProcessKey (new KeyEvent (Key.CtrlMask | Key.CursorRight, new KeyModifiers () { Ctrl = true }));
			Assert.Equal (15, tf.CursorPosition);
			tf.ProcessKey (new KeyEvent (Key.CtrlMask | Key.CursorRight, new KeyModifiers () { Ctrl = true }));
			Assert.Equal (22, tf.CursorPosition);
		}
	}
}