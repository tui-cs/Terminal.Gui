using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests {
	public class TextViewTests {
		private static TextView _textView;
		readonly ITestOutputHelper output;

		public TextViewTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		// This class enables test functions annotated with the [InitShutdown] attribute
		// to have a function called before the test function is called and after.
		// 
		// This is necessary because a) Application is a singleton and Init/Shutdown must be called
		// as a pair, and b) all unit test functions should be atomic.
		[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
		public class TextViewTestsAutoInitShutdown : AutoInitShutdownAttribute {

			public static string txt = "TAB to jump between text fields.";
			public override void Before (MethodInfo methodUnderTest)
			{
				base.Before (methodUnderTest);

				//                   1         2         3 
				//         01234567890123456789012345678901=32 (Length)
				var buff = new byte [txt.Length];
				for (int i = 0; i < txt.Length; i++) {
					buff [i] = (byte)txt [i];
				}
				var ms = new System.IO.MemoryStream (buff).ToArray ();
				_textView = new TextView () { Width = 30, Height = 10 };
				_textView.Text = ms;
			}

			public override void After (MethodInfo methodUnderTest)
			{
				_textView = null;
				base.After (methodUnderTest);
			}
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Changing_Selection_Or_CursorPosition_Update_SelectedLength_And_SelectedText ()
		{
			_textView.SelectionStartColumn = 2;
			_textView.SelectionStartRow = 0;
			Assert.Equal (0, _textView.CursorPosition.X);
			Assert.Equal (0, _textView.CursorPosition.Y);
			Assert.Equal (2, _textView.SelectedLength);
			Assert.Equal ("TA", _textView.SelectedText);
			_textView.CursorPosition = new Point (20, 0);
			Assert.Equal (2, _textView.SelectionStartColumn);
			Assert.Equal (0, _textView.SelectionStartRow);
			Assert.Equal (18, _textView.SelectedLength);
			Assert.Equal ("B to jump between ", _textView.SelectedText);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Selection_With_Value_Less_Than_Zero_Changes_To_Zero ()
		{
			_textView.SelectionStartColumn = -2;
			_textView.SelectionStartRow = -2;
			Assert.Equal (0, _textView.SelectionStartColumn);
			Assert.Equal (0, _textView.SelectionStartRow);
			Assert.Equal (0, _textView.SelectedLength);
			Assert.Equal ("", _textView.SelectedText);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Selection_With_Value_Greater_Than_Text_Length_Changes_To_Text_Length ()
		{
			_textView.CursorPosition = new Point (2, 0);
			_textView.SelectionStartColumn = 33;
			_textView.SelectionStartRow = 1;
			Assert.Equal (32, _textView.SelectionStartColumn);
			Assert.Equal (0, _textView.SelectionStartRow);
			Assert.Equal (30, _textView.SelectedLength);
			Assert.Equal ("B to jump between text fields.", _textView.SelectedText);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Selection_With_Empty_Text ()
		{
			_textView = new TextView ();
			_textView.CursorPosition = new Point (2, 0);
			_textView.SelectionStartColumn = 33;
			_textView.SelectionStartRow = 1;
			Assert.Equal (0, _textView.SelectionStartColumn);
			Assert.Equal (0, _textView.SelectionStartRow);
			Assert.Equal (0, _textView.SelectedLength);
			Assert.Equal ("", _textView.SelectedText);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Selection_And_CursorPosition_With_Value_Greater_Than_Text_Length_Changes_Both_To_Text_Length ()
		{
			_textView.CursorPosition = new Point (33, 2);
			_textView.SelectionStartColumn = 33;
			_textView.SelectionStartRow = 33;
			Assert.Equal (32, _textView.CursorPosition.X);
			Assert.Equal (0, _textView.CursorPosition.Y);
			Assert.Equal (32, _textView.SelectionStartColumn);
			Assert.Equal (0, _textView.SelectionStartRow);
			Assert.Equal (0, _textView.SelectedLength);
			Assert.Equal ("", _textView.SelectedText);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void CursorPosition_With_Value_Less_Than_Zero_Changes_To_Zero ()
		{
			_textView.CursorPosition = new Point (-1, -1);
			Assert.Equal (0, _textView.CursorPosition.X);
			Assert.Equal (0, _textView.CursorPosition.Y);
			Assert.Equal (0, _textView.SelectedLength);
			Assert.Equal ("", _textView.SelectedText);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void CursorPosition_With_Value_Greater_Than_Text_Length_Changes_To_Text_Length ()
		{
			_textView.CursorPosition = new Point (33, 1);
			Assert.Equal (32, _textView.CursorPosition.X);
			Assert.Equal (0, _textView.CursorPosition.Y);
			Assert.Equal (0, _textView.SelectedLength);
			Assert.Equal ("", _textView.SelectedText);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void WordForward_With_No_Selection ()
		{
			_textView.CursorPosition = new Point (0, 0);
			var iteration = 0;

			while (_textView.CursorPosition.X < _textView.Text.Length) {
				_textView.ProcessKey (new KeyEvent (Key.CursorRight | Key.CtrlMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (4, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 1:
					Assert.Equal (7, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 2:
					Assert.Equal (12, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 3:
					Assert.Equal (20, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 4:
					Assert.Equal (25, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 5:
					Assert.Equal (32, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void WordBackward_With_No_Selection ()
		{
			_textView.CursorPosition = new Point (_textView.Text.Length, 0);
			var iteration = 0;

			while (_textView.CursorPosition.X > 0) {
				_textView.ProcessKey (new KeyEvent (Key.CursorLeft | Key.CtrlMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (25, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 1:
					Assert.Equal (20, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 2:
					Assert.Equal (12, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 3:
					Assert.Equal (7, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 4:
					Assert.Equal (4, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 5:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void WordForward_With_Selection ()
		{
			_textView.CursorPosition = new Point (0, 0);
			_textView.SelectionStartColumn = 0;
			_textView.SelectionStartRow = 0;
			var iteration = 0;

			while (_textView.CursorPosition.X < _textView.Text.Length) {
				_textView.ProcessKey (new KeyEvent (Key.CursorRight | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (4, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (4, _textView.SelectedLength);
					Assert.Equal ("TAB ", _textView.SelectedText);
					break;
				case 1:
					Assert.Equal (7, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (7, _textView.SelectedLength);
					Assert.Equal ("TAB to ", _textView.SelectedText);
					break;
				case 2:
					Assert.Equal (12, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (12, _textView.SelectedLength);
					Assert.Equal ("TAB to jump ", _textView.SelectedText);
					break;
				case 3:
					Assert.Equal (20, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (20, _textView.SelectedLength);
					Assert.Equal ("TAB to jump between ", _textView.SelectedText);
					break;
				case 4:
					Assert.Equal (25, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (25, _textView.SelectedLength);
					Assert.Equal ("TAB to jump between text ", _textView.SelectedText);
					break;
				case 5:
					Assert.Equal (32, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (32, _textView.SelectedLength);
					Assert.Equal ("TAB to jump between text fields.", _textView.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void WordBackward_With_Selection ()
		{
			_textView.CursorPosition = new Point (_textView.Text.Length, 0);
			_textView.SelectionStartColumn = _textView.Text.Length;
			_textView.SelectionStartRow = 0;
			var iteration = 0;

			while (_textView.CursorPosition.X > 0) {
				_textView.ProcessKey (new KeyEvent (Key.CursorLeft | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (25, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (32, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (7, _textView.SelectedLength);
					Assert.Equal ("fields.", _textView.SelectedText);
					break;
				case 1:
					Assert.Equal (20, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (32, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (12, _textView.SelectedLength);
					Assert.Equal ("text fields.", _textView.SelectedText);
					break;
				case 2:
					Assert.Equal (12, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (32, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (20, _textView.SelectedLength);
					Assert.Equal ("between text fields.", _textView.SelectedText);
					break;
				case 3:
					Assert.Equal (7, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (32, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (25, _textView.SelectedLength);
					Assert.Equal ("jump between text fields.", _textView.SelectedText);
					break;
				case 4:
					Assert.Equal (4, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (32, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (28, _textView.SelectedLength);
					Assert.Equal ("to jump between text fields.", _textView.SelectedText);
					break;
				case 5:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (32, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (32, _textView.SelectedLength);
					Assert.Equal ("TAB to jump between text fields.", _textView.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void WordForward_With_The_Same_Values_For_SelectedStart_And_CursorPosition_And_Not_Starting_At_Beginning_Of_The_Text ()
		{
			_textView.CursorPosition = new Point (10, 0);
			_textView.SelectionStartColumn = 10;
			_textView.SelectionStartRow = 0;
			var iteration = 0;

			while (_textView.CursorPosition.X < _textView.Text.Length) {
				_textView.ProcessKey (new KeyEvent (Key.CursorRight | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (12, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (10, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (2, _textView.SelectedLength);
					Assert.Equal ("p ", _textView.SelectedText);
					break;
				case 1:
					Assert.Equal (20, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (10, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (10, _textView.SelectedLength);
					Assert.Equal ("p between ", _textView.SelectedText);
					break;
				case 2:
					Assert.Equal (25, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (10, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (15, _textView.SelectedLength);
					Assert.Equal ("p between text ", _textView.SelectedText);
					break;
				case 3:
					Assert.Equal (32, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (10, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (22, _textView.SelectedLength);
					Assert.Equal ("p between text fields.", _textView.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void WordBackward_With_The_Same_Values_For_SelectedStart_And_CursorPosition_And_Not_Starting_At_Beginning_Of_The_Text ()
		{
			_textView.CursorPosition = new Point (10, 0);
			_textView.SelectionStartColumn = 10;
			_textView.SelectionStartRow = 0;
			var iteration = 0;

			while (_textView.CursorPosition.X > 0) {
				_textView.ProcessKey (new KeyEvent (Key.CursorLeft | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (7, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (10, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (3, _textView.SelectedLength);
					Assert.Equal ("jum", _textView.SelectedText);
					break;
				case 1:
					Assert.Equal (4, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (10, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (6, _textView.SelectedLength);
					Assert.Equal ("to jum", _textView.SelectedText);
					break;
				case 2:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (10, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (10, _textView.SelectedLength);
					Assert.Equal ("TAB to jum", _textView.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void WordForward_With_No_Selection_And_With_More_Than_Only_One_Whitespace_And_With_Only_One_Letter ()
		{
			//                          1         2         3         4         5    
			//                0123456789012345678901234567890123456789012345678901234=55 (Length)
			_textView.Text = "TAB   t  o  jump         b  etween    t ext   f ields .";
			_textView.CursorPosition = new Point (0, 0);
			var iteration = 0;

			while (_textView.CursorPosition.X < _textView.Text.Length) {
				_textView.ProcessKey (new KeyEvent (Key.CursorRight | Key.CtrlMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (6, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 1:
					Assert.Equal (9, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 2:
					Assert.Equal (12, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 3:
					Assert.Equal (25, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 4:
					Assert.Equal (28, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 5:
					Assert.Equal (38, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 6:
					Assert.Equal (40, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 7:
					Assert.Equal (46, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 8:
					Assert.Equal (48, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 9:
					Assert.Equal (54, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 10:
					Assert.Equal (55, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void WordBackward_With_No_Selection_And_With_More_Than_Only_One_Whitespace_And_With_Only_One_Letter ()
		{
			//                          1         2         3         4         5    
			//                0123456789012345678901234567890123456789012345678901234=55 (Length)
			_textView.Text = "TAB   t  o  jump         b  etween    t ext   f ields .";
			_textView.CursorPosition = new Point (_textView.Text.Length, 0);
			var iteration = 0;

			while (_textView.CursorPosition.X > 0) {
				_textView.ProcessKey (new KeyEvent (Key.CursorLeft | Key.CtrlMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (54, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 1:
					Assert.Equal (48, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 2:
					Assert.Equal (46, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 3:
					Assert.Equal (40, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 4:
					Assert.Equal (38, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 5:
					Assert.Equal (28, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 6:
					Assert.Equal (25, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 7:
					Assert.Equal (12, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 8:
					Assert.Equal (9, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 9:
					Assert.Equal (6, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				case 10:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (0, _textView.SelectedLength);
					Assert.Equal ("", _textView.SelectedText);
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void WordBackward_Multiline_With_Selection ()
		{
			//		          4         3          2         1
			//		  87654321098765432109876 54321098765432109876543210-Length
			//			    1         2              1         2
			//                01234567890123456789012  0123456789012345678901234
			_textView.Text = "This is the first line.\nThis is the second line.";

			_textView.MoveEnd ();
			_textView.SelectionStartColumn = _textView.CurrentColumn;
			_textView.SelectionStartRow = _textView.CurrentRow;
			var iteration = 0;
			bool iterationsFinished = false;

			while (!iterationsFinished) {
				_textView.ProcessKey (new KeyEvent (Key.CursorLeft | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (19, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal (24, _textView.SelectionStartColumn);
					Assert.Equal (1, _textView.SelectionStartRow);
					Assert.Equal (5, _textView.SelectedLength);
					Assert.Equal ("line.", _textView.SelectedText);
					break;
				case 1:
					Assert.Equal (12, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal (24, _textView.SelectionStartColumn);
					Assert.Equal (1, _textView.SelectionStartRow);
					Assert.Equal (12, _textView.SelectedLength);
					Assert.Equal ("second line.", _textView.SelectedText);
					break;
				case 2:
					Assert.Equal (8, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal (24, _textView.SelectionStartColumn);
					Assert.Equal (1, _textView.SelectionStartRow);
					Assert.Equal (16, _textView.SelectedLength);
					Assert.Equal ("the second line.", _textView.SelectedText);
					break;
				case 3:
					Assert.Equal (5, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal (24, _textView.SelectionStartColumn);
					Assert.Equal (1, _textView.SelectionStartRow);
					Assert.Equal (19, _textView.SelectedLength);
					Assert.Equal ("is the second line.", _textView.SelectedText);
					break;
				case 4:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal (24, _textView.SelectionStartColumn);
					Assert.Equal (1, _textView.SelectionStartRow);
					Assert.Equal (24, _textView.SelectedLength);
					Assert.Equal ("This is the second line.", _textView.SelectedText);
					break;
				case 5:
					Assert.Equal (23, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (24, _textView.SelectionStartColumn);
					Assert.Equal (1, _textView.SelectionStartRow);
					Assert.Equal (24 + Environment.NewLine.Length, _textView.SelectedLength);
					Assert.Equal ($"{Environment.NewLine}This is the second line.", _textView.SelectedText);
					break;
				case 6:
					Assert.Equal (18, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (24, _textView.SelectionStartColumn);
					Assert.Equal (1, _textView.SelectionStartRow);
					Assert.Equal (29 + Environment.NewLine.Length, _textView.SelectedLength);
					Assert.Equal ($"line.{Environment.NewLine}This is the second line.", _textView.SelectedText);
					break;
				case 7:
					Assert.Equal (12, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (24, _textView.SelectionStartColumn);
					Assert.Equal (1, _textView.SelectionStartRow);
					Assert.Equal (35 + Environment.NewLine.Length, _textView.SelectedLength);
					Assert.Equal ($"first line.{Environment.NewLine}This is the second line.", _textView.SelectedText);
					break;
				case 8:
					Assert.Equal (8, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (24, _textView.SelectionStartColumn);
					Assert.Equal (1, _textView.SelectionStartRow);
					Assert.Equal (39 + Environment.NewLine.Length, _textView.SelectedLength);
					Assert.Equal ($"the first line.{Environment.NewLine}This is the second line.", _textView.SelectedText);
					break;
				case 9:
					Assert.Equal (5, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (24, _textView.SelectionStartColumn);
					Assert.Equal (1, _textView.SelectionStartRow);
					Assert.Equal (42 + Environment.NewLine.Length, _textView.SelectedLength);
					Assert.Equal ($"is the first line.{Environment.NewLine}This is the second line.", _textView.SelectedText);
					break;
				case 10:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (24, _textView.SelectionStartColumn);
					Assert.Equal (1, _textView.SelectionStartRow);
					Assert.Equal (47 + Environment.NewLine.Length, _textView.SelectedLength);
					Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.", _textView.SelectedText);
					break;
				default:
					iterationsFinished = true;
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void WordForward_Multiline_With_Selection ()
		{
			//			    1         2          3         4
			//		  01234567890123456789012 34567890123456789012345678-Length
			//			    1         2              1         2
			//                01234567890123456789012  0123456789012345678901234
			_textView.Text = "This is the first line.\nThis is the second line.";

			_textView.SelectionStartColumn = _textView.CurrentColumn;
			_textView.SelectionStartRow = _textView.CurrentRow;
			var iteration = 0;
			bool iterationsFinished = false;

			while (!iterationsFinished) {
				_textView.ProcessKey (new KeyEvent (Key.CursorRight | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (5, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (5, _textView.SelectedLength);
					Assert.Equal ("This ", _textView.SelectedText);
					break;
				case 1:
					Assert.Equal (8, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (8, _textView.SelectedLength);
					Assert.Equal ("This is ", _textView.SelectedText);
					break;
				case 2:
					Assert.Equal (12, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (12, _textView.SelectedLength);
					Assert.Equal ("This is the ", _textView.SelectedText);
					break;
				case 3:
					Assert.Equal (18, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (18, _textView.SelectedLength);
					Assert.Equal ("This is the first ", _textView.SelectedText);
					break;
				case 4:
					Assert.Equal (23, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (23, _textView.SelectedLength);
					Assert.Equal ("This is the first line.", _textView.SelectedText);
					break;
				case 5:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (23 + Environment.NewLine.Length, _textView.SelectedLength);
					Assert.Equal ($"This is the first line.{Environment.NewLine}", _textView.SelectedText);
					break;
				case 6:
					Assert.Equal (5, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (28 + Environment.NewLine.Length, _textView.SelectedLength);
					Assert.Equal ($"This is the first line.{Environment.NewLine}This ", _textView.SelectedText);
					break;
				case 7:
					Assert.Equal (8, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (31 + Environment.NewLine.Length, _textView.SelectedLength);
					Assert.Equal ($"This is the first line.{Environment.NewLine}This is ", _textView.SelectedText);
					break;
				case 8:
					Assert.Equal (12, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (35 + Environment.NewLine.Length, _textView.SelectedLength);
					Assert.Equal ($"This is the first line.{Environment.NewLine}This is the ", _textView.SelectedText);
					break;
				case 9:
					Assert.Equal (19, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (42 + Environment.NewLine.Length, _textView.SelectedLength);
					Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second ", _textView.SelectedText);
					break;
				case 10:
					Assert.Equal (24, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal (0, _textView.SelectionStartColumn);
					Assert.Equal (0, _textView.SelectionStartRow);
					Assert.Equal (47 + Environment.NewLine.Length, _textView.SelectedLength);
					Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.", _textView.SelectedText);
					break;
				default:
					iterationsFinished = true;
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Kill_To_End_Delete_Forwards_And_Copy_To_The_Clipboard ()
		{
			_textView.Text = "This is the first line.\nThis is the second line.";
			var iteration = 0;
			bool iterationsFinished = false;

			while (!iterationsFinished) {
				switch (iteration) {
				case 0:
					_textView.ProcessKey (new KeyEvent (Key.K | Key.CtrlMask, new KeyModifiers ()));
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ($"{Environment.NewLine}This is the second line.", _textView.Text.ToString ());
					Assert.Equal ("This is the first line.", Clipboard.Contents.ToString ());
					break;
				case 1:
					_textView.ProcessKey (new KeyEvent (Key.DeleteChar | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ()));
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("This is the second line.", _textView.Text.ToString ());
					Assert.Equal ($"This is the first line.{Environment.NewLine}", Clipboard.Contents.ToString ());
					break;
				case 2:
					_textView.ProcessKey (new KeyEvent (Key.K | Key.CtrlMask, new KeyModifiers ()));
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("", _textView.Text.ToString ());
					Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.", Clipboard.Contents.ToString ());

					// Paste
					_textView.ProcessKey (new KeyEvent (Key.Y | Key.CtrlMask, new KeyModifiers ()));
					Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.", _textView.Text.ToString ());
					break;
				default:
					iterationsFinished = true;
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Kill_To_Start_Delete_Backwards_And_Copy_To_The_Clipboard ()
		{
			_textView.Text = "This is the first line.\nThis is the second line.";
			_textView.MoveEnd ();
			var iteration = 0;
			bool iterationsFinished = false;

			while (!iterationsFinished) {
				switch (iteration) {
				case 0:
					_textView.ProcessKey (new KeyEvent (Key.K | Key.AltMask, new KeyModifiers ()));
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal ($"This is the first line.{Environment.NewLine}", _textView.Text.ToString ());
					Assert.Equal ($"This is the second line.", Clipboard.Contents.ToString ());
					break;
				case 1:
					_textView.ProcessKey (new KeyEvent (Key.Backspace | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ()));
					Assert.Equal (23, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("This is the first line.", _textView.Text.ToString ());
					Assert.Equal ($"This is the second line.{Environment.NewLine}", Clipboard.Contents.ToString ());
					break;
				case 2:
					_textView.ProcessKey (new KeyEvent (Key.K | Key.AltMask, new KeyModifiers ()));
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("", _textView.Text.ToString ());
					Assert.Equal ($"This is the second line.{Environment.NewLine}This is the first line.", Clipboard.Contents.ToString ());

					// Paste inverted
					_textView.ProcessKey (new KeyEvent (Key.Y | Key.CtrlMask, new KeyModifiers ()));
					Assert.Equal ($"This is the second line.{Environment.NewLine}This is the first line.", _textView.Text.ToString ());
					break;
				default:
					iterationsFinished = true;
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Kill_Delete_WordForward ()
		{
			_textView.Text = "This is the first line.";
			var iteration = 0;
			bool iterationsFinished = false;

			while (!iterationsFinished) {
				_textView.ProcessKey (new KeyEvent (Key.DeleteChar | Key.CtrlMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("is the first line.", _textView.Text);
					break;
				case 1:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("the first line.", _textView.Text);
					break;
				case 2:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("first line.", _textView.Text);
					break;
				case 3:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("line.", _textView.Text);
					break;
				case 4:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("", _textView.Text);
					break;
				default:
					iterationsFinished = true;
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Kill_Delete_WordBackward ()
		{
			_textView.Text = "This is the first line.";
			_textView.MoveEnd ();
			var iteration = 0;
			bool iterationsFinished = false;

			while (!iterationsFinished) {
				_textView.ProcessKey (new KeyEvent (Key.Backspace | Key.CtrlMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (18, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("This is the first ", _textView.Text);
					break;
				case 1:
					Assert.Equal (12, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("This is the ", _textView.Text);
					break;
				case 2:
					Assert.Equal (8, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("This is ", _textView.Text);
					break;
				case 3:
					Assert.Equal (5, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("This ", _textView.Text);
					break;
				case 4:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("", _textView.Text);
					break;
				default:
					iterationsFinished = true;
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Kill_Delete_WordForward_Multiline ()
		{
			_textView.Text = "This is the first line.\nThis is the second line.";
			_textView.Width = 4;
			var iteration = 0;
			bool iterationsFinished = false;

			while (!iterationsFinished) {
				_textView.ProcessKey (new KeyEvent (Key.DeleteChar | Key.CtrlMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("is the first line." + Environment.NewLine
						+ "This is the second line.", _textView.Text);
					break;
				case 1:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("the first line." + Environment.NewLine
						+ "This is the second line.", _textView.Text);
					break;
				case 2:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("first line." + Environment.NewLine
						+ "This is the second line.", _textView.Text);
					break;
				case 3:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("line." + Environment.NewLine
						+ "This is the second line.", _textView.Text);
					break;
				case 4:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("" + Environment.NewLine
						+ "This is the second line.", _textView.Text);
					break;
				case 5:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("This is the second line.", _textView.Text);
					break;
				case 6:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("is the second line.", _textView.Text);
					break;
				case 7:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("the second line.", _textView.Text);
					break;
				case 8:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("second line.", _textView.Text);
					break;
				case 9:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("line.", _textView.Text);
					break;
				case 10:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("", _textView.Text);
					break;
				default:
					iterationsFinished = true;
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Kill_Delete_WordBackward_Multiline ()
		{
			_textView.Text = "This is the first line.\nThis is the second line.";
			_textView.Width = 4;
			_textView.MoveEnd ();
			var iteration = 0;
			bool iterationsFinished = false;

			while (!iterationsFinished) {
				_textView.ProcessKey (new KeyEvent (Key.Backspace | Key.CtrlMask, new KeyModifiers ()));
				switch (iteration) {
				case 0:
					Assert.Equal (19, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal ("This is the first line." + Environment.NewLine
						+ "This is the second ", _textView.Text);
					break;
				case 1:
					Assert.Equal (12, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal ("This is the first line." + Environment.NewLine
						+ "This is the ", _textView.Text);
					break;
				case 2:
					Assert.Equal (8, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal ("This is the first line." + Environment.NewLine
						+ "This is ", _textView.Text);
					break;
				case 3:
					Assert.Equal (5, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal ("This is the first line." + Environment.NewLine
						+ "This ", _textView.Text);
					break;
				case 4:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (1, _textView.CursorPosition.Y);
					Assert.Equal ("This is the first line." + Environment.NewLine, _textView.Text);
					break;
				case 5:
					Assert.Equal (23, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("This is the first line.", _textView.Text);
					break;
				case 6:
					Assert.Equal (18, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("This is the first ", _textView.Text);
					break;
				case 7:
					Assert.Equal (12, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("This is the ", _textView.Text);
					break;
				case 8:
					Assert.Equal (8, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("This is ", _textView.Text);
					break;
				case 9:
					Assert.Equal (5, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("This ", _textView.Text);
					break;
				case 10:
					Assert.Equal (0, _textView.CursorPosition.X);
					Assert.Equal (0, _textView.CursorPosition.Y);
					Assert.Equal ("", _textView.Text);
					break;
				default:
					iterationsFinished = true;
					break;
				}
				iteration++;
			}
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Copy_Or_Cut_Null_If_No_Selection ()
		{
			_textView.SelectionStartColumn = 0;
			_textView.SelectionStartRow = 0;
			_textView.ProcessKey (new KeyEvent (Key.C | Key.CtrlMask, new KeyModifiers ())); // Copy
			Assert.Equal ("", _textView.SelectedText);
			_textView.ProcessKey (new KeyEvent (Key.W | Key.CtrlMask, new KeyModifiers ())); // Cut
			Assert.Equal ("", _textView.SelectedText);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Copy_Or_Cut_Not_Null_If_Has_Selection ()
		{
			_textView.SelectionStartColumn = 20;
			_textView.SelectionStartRow = 0;
			_textView.CursorPosition = new Point (24, 0);
			_textView.ProcessKey (new KeyEvent (Key.C | Key.CtrlMask, new KeyModifiers ())); // Copy
			Assert.Equal ("text", _textView.SelectedText);
			_textView.ProcessKey (new KeyEvent (Key.W | Key.CtrlMask, new KeyModifiers ())); // Cut
			Assert.Equal ("", _textView.SelectedText);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Copy_Or_Cut_And_Paste_With_Selection ()
		{
			_textView.SelectionStartColumn = 20;
			_textView.SelectionStartRow = 0;
			_textView.CursorPosition = new Point (24, 0);
			_textView.ProcessKey (new KeyEvent (Key.C | Key.CtrlMask, new KeyModifiers ())); // Copy
			Assert.Equal ("text", _textView.SelectedText);
			Assert.Equal ("TAB to jump between text fields.", _textView.Text);
			_textView.ProcessKey (new KeyEvent (Key.Y | Key.CtrlMask, new KeyModifiers ())); // Paste
			Assert.Equal ("TAB to jump between text fields.", _textView.Text);
			_textView.SelectionStartColumn = 20;
			_textView.SelectionStartRow = 0;
			_textView.ProcessKey (new KeyEvent (Key.W | Key.CtrlMask, new KeyModifiers ())); // Cut
			_textView.ProcessKey (new KeyEvent (Key.Y | Key.CtrlMask, new KeyModifiers ())); // Paste
			Assert.Equal ("TAB to jump between text fields.", _textView.Text);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Copy_Or_Cut_And_Paste_With_No_Selection ()
		{
			_textView.SelectionStartColumn = 20;
			_textView.SelectionStartRow = 0;
			_textView.CursorPosition = new Point (24, 0);
			_textView.ProcessKey (new KeyEvent (Key.C | Key.CtrlMask, new KeyModifiers ())); // Copy
			Assert.Equal ("text", _textView.SelectedText);
			Assert.Equal ("TAB to jump between text fields.", _textView.Text);
			_textView.SelectionStartColumn = 0;
			_textView.SelectionStartRow = 0;
			Assert.Equal (new Point (24, 0), _textView.CursorPosition);
			Assert.True (_textView.Selecting);
			_textView.Selecting = false;
			_textView.ProcessKey (new KeyEvent (Key.Y | Key.CtrlMask, new KeyModifiers ())); // Paste
			Assert.Equal (new Point (28, 0), _textView.CursorPosition);
			Assert.False (_textView.Selecting);
			Assert.Equal ("TAB to jump between texttext fields.", _textView.Text);
			_textView.SelectionStartColumn = 24;
			_textView.SelectionStartRow = 0;
			_textView.ProcessKey (new KeyEvent (Key.W | Key.CtrlMask, new KeyModifiers ())); // Cut
			Assert.Equal (new Point (24, 0), _textView.CursorPosition);
			Assert.False (_textView.Selecting);
			Assert.Equal ("", _textView.SelectedText);
			Assert.Equal ("TAB to jump between text fields.", _textView.Text);
			_textView.SelectionStartColumn = 0;
			_textView.SelectionStartRow = 0;
			_textView.Selecting = false;
			_textView.ProcessKey (new KeyEvent (Key.Y | Key.CtrlMask, new KeyModifiers ())); // Paste
			Assert.Equal (new Point (28, 0), _textView.CursorPosition);
			Assert.False (_textView.Selecting);
			Assert.Equal ("TAB to jump between texttext fields.", _textView.Text);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Cut_Not_Allowed_If_ReadOnly_Is_True ()
		{
			_textView.ReadOnly = true;
			_textView.SelectionStartColumn = 20;
			_textView.SelectionStartRow = 0;
			_textView.CursorPosition = new Point (24, 0);
			_textView.ProcessKey (new KeyEvent (Key.C | Key.CtrlMask, new KeyModifiers ())); // Copy
			Assert.Equal ("text", _textView.SelectedText);
			_textView.ProcessKey (new KeyEvent (Key.W | Key.CtrlMask, new KeyModifiers ())); // Selecting is set to false after Cut.
			Assert.Equal ("", _textView.SelectedText);
			_textView.ReadOnly = false;
			Assert.False (_textView.Selecting);
			_textView.Selecting = true; // Needed to set Selecting to true.
			_textView.ProcessKey (new KeyEvent (Key.C | Key.CtrlMask, new KeyModifiers ())); // Copy
			Assert.Equal ("text", _textView.SelectedText);
			_textView.ProcessKey (new KeyEvent (Key.W | Key.CtrlMask, new KeyModifiers ())); // Cut
			Assert.Equal ("", _textView.SelectedText);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Paste_Always_Clear_The_SelectedText ()
		{
			_textView.SelectionStartColumn = 20;
			_textView.SelectionStartRow = 0;
			_textView.CursorPosition = new Point (24, 0);
			_textView.ProcessKey (new KeyEvent (Key.C | Key.CtrlMask, new KeyModifiers ())); // Copy
			Assert.Equal ("text", _textView.SelectedText);
			_textView.ProcessKey (new KeyEvent (Key.Y | Key.CtrlMask, new KeyModifiers ())); // Paste
			Assert.Equal ("", _textView.SelectedText);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void TextChanged_Event ()
		{
			_textView.TextChanged += (s, e) => {
				if (_textView.Text == "changing") {
					Assert.Equal ("changing", _textView.Text);
					_textView.Text = "changed";
				}
			};

			_textView.Text = "changing";
			Assert.Equal ("changed", _textView.Text);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void TextChanged_Event_NoFires_OnTyping ()
		{
			var eventcount = 0;
			_textView.TextChanged += (s, e) => {
				eventcount++;
			};

			_textView.Text = "ay";
			Assert.Equal (1, eventcount);
			_textView.ProcessKey (new KeyEvent (Key.Y, new KeyModifiers ()));
			Assert.Equal (1, eventcount);
			Assert.Equal ("Yay", _textView.Text.ToString ());
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Used_Is_True_By_Default ()
		{
			_textView.CursorPosition = new Point (10, 0);
			Assert.Equal ("TAB to jump between text fields.", _textView.Text);
			_textView.ProcessKey (new KeyEvent ((Key)0x75, new KeyModifiers ())); // u
			Assert.Equal ("TAB to jumup between text fields.", _textView.Text);
			_textView.ProcessKey (new KeyEvent ((Key)0x73, new KeyModifiers ())); // s
			Assert.Equal ("TAB to jumusp between text fields.", _textView.Text);
			_textView.ProcessKey (new KeyEvent ((Key)0x65, new KeyModifiers ())); // e
			Assert.Equal ("TAB to jumusep between text fields.", _textView.Text);
			_textView.ProcessKey (new KeyEvent ((Key)0x64, new KeyModifiers ())); // d
			Assert.Equal ("TAB to jumusedp between text fields.", _textView.Text);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Used_Is_False ()
		{
			_textView.Used = false;
			_textView.CursorPosition = new Point (10, 0);
			Assert.Equal ("TAB to jump between text fields.", _textView.Text);
			_textView.ProcessKey (new KeyEvent ((Key)0x75, new KeyModifiers ())); // u
			Assert.Equal ("TAB to jumu between text fields.", _textView.Text);
			_textView.ProcessKey (new KeyEvent ((Key)0x73, new KeyModifiers ())); // s
			Assert.Equal ("TAB to jumusbetween text fields.", _textView.Text);
			_textView.ProcessKey (new KeyEvent ((Key)0x65, new KeyModifiers ())); // e
			Assert.Equal ("TAB to jumuseetween text fields.", _textView.Text);
			_textView.ProcessKey (new KeyEvent ((Key)0x64, new KeyModifiers ())); // d
			Assert.Equal ("TAB to jumusedtween text fields.", _textView.Text);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Copy_Without_Selection ()
		{
			_textView.Text = "This is the first line.\nThis is the second line.\n";
			_textView.CursorPosition = new Point (0, _textView.Lines - 1);
			_textView.ProcessKey (new KeyEvent (Key.C | Key.CtrlMask, new KeyModifiers ())); // Copy
			_textView.ProcessKey (new KeyEvent (Key.Y | Key.CtrlMask, new KeyModifiers ())); // Paste
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}{Environment.NewLine}", _textView.Text);
			_textView.CursorPosition = new Point (3, 1);
			_textView.ProcessKey (new KeyEvent (Key.C | Key.CtrlMask, new KeyModifiers ())); // Copy
			_textView.ProcessKey (new KeyEvent (Key.Y | Key.CtrlMask, new KeyModifiers ())); // Paste
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the second line.{Environment.NewLine}{Environment.NewLine}", _textView.Text);
			Assert.Equal (new Point (3, 2), _textView.CursorPosition);
			_textView.ProcessKey (new KeyEvent (Key.Y | Key.CtrlMask, new KeyModifiers ())); // Paste
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the second line.{Environment.NewLine}{Environment.NewLine}", _textView.Text);
			Assert.Equal (new Point (3, 3), _textView.CursorPosition);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void TabWidth_Setting_To_Zero_Keeps_AllowsTab ()
		{
			Assert.Equal (4, _textView.TabWidth);
			Assert.True (_textView.AllowsTab);
			Assert.True (_textView.AllowsReturn);
			Assert.True (_textView.Multiline);
			_textView.TabWidth = -1;
			Assert.Equal (0, _textView.TabWidth);
			Assert.True (_textView.AllowsTab);
			Assert.True (_textView.AllowsReturn);
			Assert.True (_textView.Multiline);
			_textView.ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ()));
			Assert.Equal ("\tTAB to jump between text fields.", _textView.Text);
			_textView.ProcessKey (new KeyEvent (Key.BackTab | Key.ShiftMask, new KeyModifiers ()));
			Assert.Equal ("TAB to jump between text fields.", _textView.Text);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void AllowsTab_Setting_To_True_Changes_TabWidth_To_Default_If_It_Is_Zero ()
		{
			_textView.TabWidth = 0;
			Assert.Equal (0, _textView.TabWidth);
			Assert.True (_textView.AllowsTab);
			Assert.True (_textView.AllowsReturn);
			Assert.True (_textView.Multiline);
			_textView.AllowsTab = true;
			Assert.True (_textView.AllowsTab);
			Assert.Equal (4, _textView.TabWidth);
			Assert.True (_textView.AllowsReturn);
			Assert.True (_textView.Multiline);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void AllowsReturn_Setting_To_True_Changes_Multiline_To_True_If_It_Is_False ()
		{
			Assert.True (_textView.AllowsReturn);
			Assert.True (_textView.Multiline);
			Assert.Equal (4, _textView.TabWidth);
			Assert.True (_textView.AllowsTab);
			_textView.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ()));
			Assert.Equal (Environment.NewLine +
				"TAB to jump between text fields.", _textView.Text);

			_textView.AllowsReturn = false;
			Assert.False (_textView.AllowsReturn);
			Assert.False (_textView.Multiline);
			Assert.Equal (0, _textView.TabWidth);
			Assert.False (_textView.AllowsTab);
			_textView.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ()));
			Assert.Equal (Environment.NewLine +
				"TAB to jump between text fields.", _textView.Text);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Multiline_Setting_Changes_AllowsReturn_AllowsTab_Height_WordWrap ()
		{
			Assert.True (_textView.Multiline);
			Assert.True (_textView.AllowsReturn);
			Assert.Equal (4, _textView.TabWidth);
			Assert.True (_textView.AllowsTab);
			Assert.Equal ("Absolute(30)", _textView.Width.ToString ());
			Assert.Equal ("Absolute(10)", _textView.Height.ToString ());
			Assert.False (_textView.WordWrap);

			_textView.WordWrap = true;
			Assert.True (_textView.WordWrap);
			_textView.Multiline = false;
			Assert.False (_textView.Multiline);
			Assert.False (_textView.AllowsReturn);
			Assert.Equal (0, _textView.TabWidth);
			Assert.False (_textView.AllowsTab);
			Assert.Equal ("Absolute(30)", _textView.Width.ToString ());
			Assert.Equal ("Absolute(1)", _textView.Height.ToString ());
			Assert.False (_textView.WordWrap);

			_textView.WordWrap = true;
			Assert.False (_textView.WordWrap);
			_textView.Multiline = true;
			Assert.True (_textView.Multiline);
			Assert.True (_textView.AllowsReturn);
			Assert.Equal (4, _textView.TabWidth);
			Assert.True (_textView.AllowsTab);
			Assert.Equal ("Absolute(30)", _textView.Width.ToString ());
			Assert.Equal ("Absolute(10)", _textView.Height.ToString ());
			Assert.False (_textView.WordWrap);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Tab_Test_Follow_By_BackTab ()
		{
			Application.Top.Add (_textView);

			Application.Iteration += () => {
				var width = _textView.Bounds.Width - 1;
				Assert.Equal (30, width + 1);
				Assert.Equal (10, _textView.Height);
				_textView.Text = "";
				var col = 0;
				var leftCol = 0;
				var tabWidth = _textView.TabWidth;
				while (col < 100) {
					col++;
					_textView.ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ()));
					Assert.Equal (new Point (col, 0), _textView.CursorPosition);
					leftCol = GetLeftCol (leftCol);
					Assert.Equal (leftCol, _textView.LeftColumn);
				}
				while (col > 0) {
					col--;
					_textView.ProcessKey (new KeyEvent (Key.BackTab | Key.ShiftMask, new KeyModifiers ()));
					Assert.Equal (new Point (col, 0), _textView.CursorPosition);
					leftCol = GetLeftCol (leftCol);
					Assert.Equal (leftCol, _textView.LeftColumn);
				}

				Application.Top.Remove (_textView);
				Application.RequestStop ();
			};

			Application.Run ();
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void BackTab_Test_Follow_By_Tab ()
		{
			Application.Top.Add (_textView);

			Application.Iteration += () => {
				var width = _textView.Bounds.Width - 1;
				Assert.Equal (30, width + 1);
				Assert.Equal (10, _textView.Height);
				_textView.Text = "";
				for (int i = 0; i < 100; i++) {
					_textView.Text += "\t";
				}
				var col = 100;
				var tabWidth = _textView.TabWidth;
				var leftCol = _textView.LeftColumn;
				_textView.MoveEnd ();
				Assert.Equal (new Point (col, 0), _textView.CursorPosition);
				leftCol = GetLeftCol (leftCol);
				Assert.Equal (leftCol, _textView.LeftColumn);
				while (col > 0) {
					col--;
					_textView.ProcessKey (new KeyEvent (Key.BackTab | Key.ShiftMask, new KeyModifiers ()));
					Assert.Equal (new Point (col, 0), _textView.CursorPosition);
					leftCol = GetLeftCol (leftCol);
					Assert.Equal (leftCol, _textView.LeftColumn);
				}
				while (col < 100) {
					col++;
					_textView.ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ()));
					Assert.Equal (new Point (col, 0), _textView.CursorPosition);
					leftCol = GetLeftCol (leftCol);
					Assert.Equal (leftCol, _textView.LeftColumn);
				}

				Application.Top.Remove (_textView);
				Application.RequestStop ();
			};

			Application.Run ();
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Tab_Test_Follow_By_CursorLeft_And_Then_Follow_By_CursorRight ()
		{
			Application.Top.Add (_textView);

			Application.Iteration += () => {
				var width = _textView.Bounds.Width - 1;
				Assert.Equal (30, width + 1);
				Assert.Equal (10, _textView.Height);
				_textView.Text = "";
				var col = 0;
				var leftCol = 0;
				var tabWidth = _textView.TabWidth;
				while (col < 100) {
					col++;
					_textView.ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ()));
					Assert.Equal (new Point (col, 0), _textView.CursorPosition);
					leftCol = GetLeftCol (leftCol);
					Assert.Equal (leftCol, _textView.LeftColumn);
				}
				while (col > 0) {
					col--;
					_textView.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));
					Assert.Equal (new Point (col, 0), _textView.CursorPosition);
					leftCol = GetLeftCol (leftCol);
					Assert.Equal (leftCol, _textView.LeftColumn);
				}
				while (col < 100) {
					col++;
					_textView.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));
					Assert.Equal (new Point (col, 0), _textView.CursorPosition);
					leftCol = GetLeftCol (leftCol);
					Assert.Equal (leftCol, _textView.LeftColumn);
				}

				Application.Top.Remove (_textView);
				Application.RequestStop ();
			};

			Application.Run ();
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Tab_Test_Follow_By_BackTab_With_Text ()
		{
			Application.Top.Add (_textView);

			Application.Iteration += () => {
				var width = _textView.Bounds.Width - 1;
				Assert.Equal (30, width + 1);
				Assert.Equal (10, _textView.Height);
				var col = 0;
				var leftCol = 0;
				Assert.Equal (new Point (col, 0), _textView.CursorPosition);
				Assert.Equal (leftCol, _textView.LeftColumn);
				while (col < 100) {
					col++;
					_textView.ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ()));
					Assert.Equal (new Point (col, 0), _textView.CursorPosition);
					leftCol = GetLeftCol (leftCol);
					Assert.Equal (leftCol, _textView.LeftColumn);
				}
				while (col > 0) {
					col--;
					_textView.ProcessKey (new KeyEvent (Key.BackTab | Key.ShiftMask, new KeyModifiers ()));
					Assert.Equal (new Point (col, 0), _textView.CursorPosition);
					leftCol = GetLeftCol (leftCol);
					Assert.Equal (leftCol, _textView.LeftColumn);
				}

				Application.Top.Remove (_textView);
				Application.RequestStop ();
			};

			Application.Run ();
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Tab_Test_Follow_By_Home_And_Then_Follow_By_End_And_Then_Follow_By_BackTab_With_Text ()
		{
			Application.Top.Add (_textView);

			Application.Iteration += () => {
				var width = _textView.Bounds.Width - 1;
				Assert.Equal (30, width + 1);
				Assert.Equal (10, _textView.Height);
				var col = 0;
				var leftCol = 0;
				Assert.Equal (new Point (col, 0), _textView.CursorPosition);
				Assert.Equal (leftCol, _textView.LeftColumn);
				Assert.Equal ("TAB to jump between text fields.", _textView.Text);
				Assert.Equal (32, _textView.Text.Length);
				while (col < 100) {
					col++;
					_textView.ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ()));
					Assert.Equal (new Point (col, 0), _textView.CursorPosition);
					leftCol = GetLeftCol (leftCol);
					Assert.Equal (leftCol, _textView.LeftColumn);
				}
				_textView.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ()));
				col = 0;
				Assert.Equal (new Point (col, 0), _textView.CursorPosition);
				leftCol = 0;
				Assert.Equal (leftCol, _textView.LeftColumn);

				_textView.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ()));
				col = _textView.Text.Length;
				Assert.Equal (132, _textView.Text.Length);
				Assert.Equal (new Point (col, 0), _textView.CursorPosition);
				leftCol = GetLeftCol (leftCol);
				Assert.Equal (leftCol, _textView.LeftColumn);
				var txt = _textView.Text;
				while (col - 1 > 0 && txt [col - 1] != '\t') {
					col--;
				}
				_textView.CursorPosition = new Point (col, 0);
				leftCol = GetLeftCol (leftCol);
				while (col > 0) {
					col--;
					_textView.ProcessKey (new KeyEvent (Key.BackTab | Key.ShiftMask, new KeyModifiers ()));
					Assert.Equal (new Point (col, 0), _textView.CursorPosition);
					leftCol = GetLeftCol (leftCol);
					Assert.Equal (leftCol, _textView.LeftColumn);
				}
				Assert.Equal ("TAB to jump between text fields.", _textView.Text);
				Assert.Equal (32, _textView.Text.Length);

				Application.Top.Remove (_textView);
				Application.RequestStop ();
			};

			Application.Run ();
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Tab_Test_Follow_By_CursorLeft_And_Then_Follow_By_CursorRight_With_Text ()
		{
			Application.Top.Add (_textView);

			Application.Iteration += () => {
				var width = _textView.Bounds.Width - 1;
				Assert.Equal (30, width + 1);
				Assert.Equal (10, _textView.Height);
				Assert.Equal ("TAB to jump between text fields.", _textView.Text);
				var col = 0;
				var leftCol = 0;
				var tabWidth = _textView.TabWidth;
				while (col < 100) {
					col++;
					_textView.ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ()));
					Assert.Equal (new Point (col, 0), _textView.CursorPosition);
					leftCol = GetLeftCol (leftCol);
					Assert.Equal (leftCol, _textView.LeftColumn);
				}
				Assert.Equal (132, _textView.Text.Length);
				while (col > 0) {
					col--;
					_textView.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));
					Assert.Equal (new Point (col, 0), _textView.CursorPosition);
					leftCol = GetLeftCol (leftCol);
					Assert.Equal (leftCol, _textView.LeftColumn);
				}
				while (col < 100) {
					col++;
					_textView.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));
					Assert.Equal (new Point (col, 0), _textView.CursorPosition);
					leftCol = GetLeftCol (leftCol);
					Assert.Equal (leftCol, _textView.LeftColumn);
				}

				Application.Top.Remove (_textView);
				Application.RequestStop ();
			};

			Application.Run ();
		}

		[Fact]
		public void TextView_MultiLine_But_Without_Tabs ()
		{
			var view = new TextView ();

			// the default for TextView
			Assert.True (view.Multiline);

			view.AllowsTab = false;
			Assert.False (view.AllowsTab);

			Assert.True (view.Multiline);
		}

		private int GetLeftCol (int start)
		{
			var lines = _textView.Text.Split (Environment.NewLine);
			if (lines == null || lines.Length == 0) {
				return 0;
			}
			if (start == _textView.LeftColumn) {
				return start;
			}
			if (_textView.LeftColumn == _textView.CurrentColumn) {
				return _textView.CurrentColumn;
			}
			var cCol = _textView.CurrentColumn;
			var line = lines [_textView.CurrentRow];
			var lCount = cCol > line.Length - 1 ? line.Length - 1 : cCol;
			var width = _textView.Frame.Width;
			var tabWidth = _textView.TabWidth;
			var sumLength = 0;
			var col = 0;

			for (int i = lCount; i >= 0; i--) {
				var r = line [i];
				sumLength += Rune.ColumnWidth (r);
				if (r == '\t') {
					sumLength += tabWidth + 1;
				}
				if (sumLength > width) {
					if (col + width == cCol) {
						col++;
					}
					break;
				} else if ((cCol < line.Length && col > 0 && start < cCol && col == start) || (cCol - col == width - 1)) {
					break;
				}
				col = i;
			}

			return col;
		}

		[Fact]
		public void LoadFile_Throws_If_File_Is_Null ()
		{
			var result = false;
			var tv = new TextView ();
			Assert.Throws<ArgumentNullException> (() => result = tv.LoadFile (null));
			Assert.False (result);
		}

		[Fact]
		public void LoadFile_Throws_If_File_Is_Empty ()
		{
			var result = false;
			var tv = new TextView ();
			Assert.Throws<ArgumentException> (() => result = tv.LoadFile (""));
			Assert.False (result);
		}

		[Fact]
		public void LoadFile_Throws_If_File_Not_Exist ()
		{
			var result = false;
			var tv = new TextView ();
			Assert.Throws<System.IO.FileNotFoundException> (() => result = tv.LoadFile ("blabla"));
			Assert.False (result);
		}

		[Fact]
		public void LoadStream_Throws_If_Stream_Is_Null ()
		{
			var tv = new TextView ();
			Assert.Throws<ArgumentNullException> (() => tv.LoadStream (null));
		}

		[Fact]
		public void LoadStream_Stream_Is_Empty ()
		{
			var tv = new TextView ();
			tv.LoadStream (new System.IO.MemoryStream ());
			Assert.Equal ("", tv.Text);
		}

		[Fact]
		public void LoadStream_CRLF ()
		{
			var text = "This is the first line.\r\nThis is the second line.\r\n";
			var tv = new TextView ();
			tv.LoadStream (new System.IO.MemoryStream (System.Text.Encoding.ASCII.GetBytes (text)));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);
		}

		[Fact]
		public void LoadStream_LF ()
		{
			var text = "This is the first line.\nThis is the second line.\n";
			var tv = new TextView ();
			tv.LoadStream (new System.IO.MemoryStream (System.Text.Encoding.ASCII.GetBytes (text)));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);
		}

		[Fact]
		public void LoadStream_IsDirty ()
		{
			var text = "Testing";
			using (System.IO.MemoryStream stream = new System.IO.MemoryStream ()) {

				var writer = new System.IO.StreamWriter (stream);
				writer.Write (text);
				writer.Flush ();
				stream.Position = 0;

				var tv = new TextView ();
				tv.LoadStream (stream);

				Assert.Equal (7, text.Length);
				Assert.Equal (text.Length, tv.Text.Length);
				Assert.Equal (text, tv.Text);
				Assert.False (tv.IsDirty);
			}
		}

		[Fact]
		public void LoadStream_IsDirty_With_Null_On_The_Text ()
		{
			var text = "Test\0ing";
			using (System.IO.MemoryStream stream = new System.IO.MemoryStream ()) {

				var writer = new System.IO.StreamWriter (stream);
				writer.Write (text);
				writer.Flush ();
				stream.Position = 0;

				var tv = new TextView ();
				tv.LoadStream (stream);

				Assert.Equal (8, text.Length);
				Assert.Equal (text.Length, tv.Text.Length);
				Assert.Equal (8, text.Length);
				Assert.Equal (8, tv.Text.Length);
				Assert.Equal (text, tv.Text);
				Assert.False (tv.IsDirty);
				Assert.Equal ('\u2400', ConsoleDriver.MakePrintable ((Rune)tv.Text [4]));
			}
		}

		[Fact]
		public void StringToRunes_Slipts_CRLF ()
		{
			var text = "This is the first line.\r\nThis is the second line.\r\n";
			var tv = new TextView ();
			tv.Text = text;
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);
		}

		[Fact]
		public void StringToRunes_Slipts_LF ()
		{
			var text = "This is the first line.\nThis is the second line.\n";
			var tv = new TextView ();
			tv.Text = text;
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);
		}

		[Fact]
		public void CloseFile_Throws_If_FilePath_Is_Null ()
		{
			var tv = new TextView ();
			Assert.Throws<ArgumentNullException> (() => tv.CloseFile ());
		}

		[Fact]
		public void WordWrap_Gets_Sets ()
		{
			var tv = new TextView () { WordWrap = true };
			Assert.True (tv.WordWrap);
			tv.WordWrap = false;
			Assert.False (tv.WordWrap);
		}

		[Fact]
		public void WordWrap_True_Text_Always_Returns_Unwrapped ()
		{
			var text = "This is the first line.\nThis is the second line.\n";
			var tv = new TextView () { Width = 10 };
			tv.Text = text;
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);
			tv.WordWrap = true;
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void WordWrap_WrapModel_Output ()
		{
			//          0123456789
			var text = "This is the first line.\nThis is the second line.\n";
			var tv = new TextView () { Width = 10, Height = 10 };
			tv.Text = text;
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);
			tv.WordWrap = true;

			Application.Top.Add (tv);

			tv.Redraw (tv.Bounds);

			TestHelpers.AssertDriverContentsWithFrameAre (@"
This is
the    
first  
line.  
This is
the    
second 
line.  
", output);
		}

		[Fact]
		[AutoInitShutdown]
		public void WordWrap_Deleting_Backwards ()
		{
			var tv = new TextView () {
				Width = 5,
				Height = 2,
				WordWrap = true,
				Text = "aaaa"
			};
			Application.Top.Add (tv);
			Application.Begin (Application.Top);

			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.LeftColumn);
			TestHelpers.AssertDriverContentsAre (@"
aaaa
", output);

			tv.CursorPosition = new Point (5, 0);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ())));
			Application.Refresh ();
			Assert.Equal (0, tv.LeftColumn);
			TestHelpers.AssertDriverContentsAre (@"
aaa
", output);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ())));
			Application.Refresh ();
			Assert.Equal (0, tv.LeftColumn);
			TestHelpers.AssertDriverContentsAre (@"
aa
", output);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ())));
			Application.Refresh ();
			Assert.Equal (0, tv.LeftColumn);
			TestHelpers.AssertDriverContentsAre (@"
a
", output);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ())));
			Application.Refresh ();
			Assert.Equal (0, tv.LeftColumn);
			TestHelpers.AssertDriverContentsAre (@"

", output);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ())));
			Application.Refresh ();
			Assert.Equal (0, tv.LeftColumn);
			TestHelpers.AssertDriverContentsAre (@"

", output);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void WordWrap_ReadOnly_CursorPosition_SelectedText_Copy ()
		{
			//          0123456789
			var text = "This is the first line.\nThis is the second line.\n";
			var tv = new TextView () { Width = 11, Height = 9 };
			tv.Text = text;
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);
			tv.WordWrap = true;

			Application.Top.Add (tv);

			tv.Redraw (tv.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This is  
the first
line.    
This is  
the      
second   
line.    
", output);

			tv.ReadOnly = true;
			tv.CursorPosition = new Point (6, 2);
			Assert.Equal (new Point (5, 2), tv.CursorPosition);
			tv.Redraw (tv.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This is  
the first
line.    
This is  
the      
second   
line.    
", output);

			tv.SelectionStartRow = 0;
			tv.SelectionStartColumn = 0;
			Assert.Equal ("This is the first line.", tv.SelectedText);

			tv.Copy ();
			Assert.Equal ("This is the first line.", Clipboard.Contents);
		}

		[Fact]
		public void Internal_Tests ()
		{
			var txt = "This is a text.";
			var txtRunes = TextModel.ToRunes (txt);
			Assert.Equal (txt.Length, txtRunes.Count);
			Assert.Equal ('T', txtRunes [0]);
			Assert.Equal ('h', txtRunes [1]);
			Assert.Equal ('i', txtRunes [2]);
			Assert.Equal ('s', txtRunes [3]);
			Assert.Equal (' ', txtRunes [4]);
			Assert.Equal ('i', txtRunes [5]);
			Assert.Equal ('s', txtRunes [6]);
			Assert.Equal (' ', txtRunes [7]);
			Assert.Equal ('a', txtRunes [8]);
			Assert.Equal (' ', txtRunes [9]);
			Assert.Equal ('t', txtRunes [10]);
			Assert.Equal ('e', txtRunes [11]);
			Assert.Equal ('x', txtRunes [12]);
			Assert.Equal ('t', txtRunes [13]);
			Assert.Equal ('.', txtRunes [^1]);

			int col = 0;
			Assert.True (TextModel.SetCol (ref col, 80, 79));
			Assert.False (TextModel.SetCol (ref col, 80, 80));
			Assert.Equal (79, col);

			var start = 0;
			var x = 8;
			Assert.Equal (8, TextModel.GetColFromX (txtRunes, start, x));
			Assert.Equal ('a', txtRunes [start + x]);
			start = 1;
			x = 7;
			Assert.Equal (7, TextModel.GetColFromX (txtRunes, start, x));
			Assert.Equal ('a', txtRunes [start + x]);

			Assert.Equal ((15, 15), TextModel.DisplaySize (txtRunes));
			Assert.Equal ((6, 6), TextModel.DisplaySize (txtRunes, 1, 7));

			Assert.Equal (0, TextModel.CalculateLeftColumn (txtRunes, 0, 7, 8));
			Assert.Equal (1, TextModel.CalculateLeftColumn (txtRunes, 0, 8, 8));
			Assert.Equal (2, TextModel.CalculateLeftColumn (txtRunes, 0, 9, 8));

			var tm = new TextModel ();
			tm.AddLine (0, TextModel.ToRunes ("This is first line."));
			tm.AddLine (1, TextModel.ToRunes ("This is last line."));
			Assert.Equal ((new Point (2, 0), true), tm.FindNextText ("is", out bool gaveFullTurn));
			Assert.False (gaveFullTurn);
			Assert.Equal ((new Point (5, 0), true), tm.FindNextText ("is", out gaveFullTurn));
			Assert.False (gaveFullTurn);
			Assert.Equal ((new Point (2, 1), true), tm.FindNextText ("is", out gaveFullTurn));
			Assert.False (gaveFullTurn);
			Assert.Equal ((new Point (5, 1), true), tm.FindNextText ("is", out gaveFullTurn));
			Assert.False (gaveFullTurn);
			Assert.Equal ((new Point (2, 0), true), tm.FindNextText ("is", out gaveFullTurn));
			Assert.True (gaveFullTurn);
			tm.ResetContinuousFind (new Point (0, 0));
			Assert.Equal ((new Point (5, 1), true), tm.FindPreviousText ("is", out gaveFullTurn));
			Assert.False (gaveFullTurn);
			Assert.Equal ((new Point (2, 1), true), tm.FindPreviousText ("is", out gaveFullTurn));
			Assert.False (gaveFullTurn);
			Assert.Equal ((new Point (5, 0), true), tm.FindPreviousText ("is", out gaveFullTurn));
			Assert.False (gaveFullTurn);
			Assert.Equal ((new Point (2, 0), true), tm.FindPreviousText ("is", out gaveFullTurn));
			Assert.False (gaveFullTurn);
			Assert.Equal ((new Point (5, 1), true), tm.FindPreviousText ("is", out gaveFullTurn));
			Assert.True (gaveFullTurn);

			Assert.Equal ((new Point (9, 1), true), tm.ReplaceAllText ("is", false, false, "really"));
			Assert.Equal (TextModel.ToRunes ("Threally really first line."), tm.GetLine (0));
			Assert.Equal (TextModel.ToRunes ("Threally really last line."), tm.GetLine (1));
			tm = new TextModel ();
			tm.AddLine (0, TextModel.ToRunes ("This is first line."));
			tm.AddLine (1, TextModel.ToRunes ("This is last line."));
			Assert.Equal ((new Point (5, 1), true), tm.ReplaceAllText ("is", false, true, "really"));
			Assert.Equal (TextModel.ToRunes ("This really first line."), tm.GetLine (0));
			Assert.Equal (TextModel.ToRunes ("This really last line."), tm.GetLine (1));
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void BottomOffset_Sets_To_Zero_Adjust_TopRow ()
		{
			string text = "";

			for (int i = 0; i < 12; i++) {
				text += $"This is the line {i}\n";
			}
			var tv = new TextView () { Width = 10, Height = 10, BottomOffset = 1 };
			tv.Text = text;

			tv.ProcessKey (new KeyEvent (Key.CtrlMask | Key.End, new KeyModifiers ()));

			Assert.Equal (4, tv.TopRow);
			Assert.Equal (1, tv.BottomOffset);

			tv.BottomOffset = 0;
			Assert.Equal (3, tv.TopRow);
			Assert.Equal (0, tv.BottomOffset);

			tv.BottomOffset = 2;
			Assert.Equal (5, tv.TopRow);
			Assert.Equal (2, tv.BottomOffset);

			tv.BottomOffset = 0;
			Assert.Equal (3, tv.TopRow);
			Assert.Equal (0, tv.BottomOffset);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void RightOffset_Sets_To_Zero_Adjust_leftColumn ()
		{
			string text = "";

			for (int i = 0; i < 12; i++) {
				text += $"{i.ToString () [^1]}";
			}
			var tv = new TextView () { Width = 10, Height = 10, RightOffset = 1 };
			tv.Text = text;

			tv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ()));

			Assert.Equal (4, tv.LeftColumn);
			Assert.Equal (1, tv.RightOffset);

			tv.RightOffset = 0;
			Assert.Equal (3, tv.LeftColumn);
			Assert.Equal (0, tv.RightOffset);

			tv.RightOffset = 2;
			Assert.Equal (5, tv.LeftColumn);
			Assert.Equal (2, tv.RightOffset);

			tv.RightOffset = 0;
			Assert.Equal (3, tv.LeftColumn);
			Assert.Equal (0, tv.RightOffset);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void TextView_SpaceHandling ()
		{
			var tv = new TextView () {
				Width = 10,
				Text = " "
			};

			MouseEvent ev = new MouseEvent () {
				X = 0,
				Y = 0,
				Flags = MouseFlags.Button1DoubleClicked,
			};

			tv.MouseEvent (ev);
			Assert.Equal (1, tv.SelectedLength);

			ev = new MouseEvent () {
				X = 1,
				Y = 0,
				Flags = MouseFlags.Button1DoubleClicked,
			};

			tv.MouseEvent (ev);
			Assert.Equal (1, tv.SelectedLength);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void CanFocus_False_Wont_Focus_With_Mouse ()
		{
			var top = Application.Top;
			var tv = new TextView () {
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
			fv.Add (tv);
			top.Add (fv);

			Application.Begin (top);

			Assert.False (tv.CanFocus);
			Assert.False (tv.HasFocus);
			Assert.False (fv.CanFocus);
			Assert.False (fv.HasFocus);

			tv.MouseEvent (new MouseEvent () {
				X = 1,
				Y = 0,
				Flags = MouseFlags.Button1DoubleClicked
			});

			Assert.Empty (tv.SelectedText);
			Assert.False (tv.CanFocus);
			Assert.False (tv.HasFocus);
			Assert.False (fv.CanFocus);
			Assert.False (fv.HasFocus);

			Assert.Throws<InvalidOperationException> (() => tv.CanFocus = true);
			fv.CanFocus = true;
			tv.CanFocus = true;
			tv.MouseEvent (new MouseEvent () {
				X = 1,
				Y = 0,
				Flags = MouseFlags.Button1DoubleClicked
			});

			Assert.Equal ("some ", tv.SelectedText);
			Assert.True (tv.CanFocus);
			Assert.True (tv.HasFocus);
			Assert.True (fv.CanFocus);
			Assert.True (fv.HasFocus);

			fv.CanFocus = false;
			tv.MouseEvent (new MouseEvent () {
				X = 1,
				Y = 0,
				Flags = MouseFlags.Button1DoubleClicked
			});

			Assert.Equal ("some ", tv.SelectedText); // Setting CanFocus to false don't change the SelectedText
			Assert.False (tv.CanFocus);
			Assert.False (tv.HasFocus);
			Assert.False (fv.CanFocus);
			Assert.False (fv.HasFocus);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void DesiredCursorVisibility_Vertical_Navigation ()
		{
			string text = "";

			for (int i = 0; i < 12; i++) {
				text += $"This is the line {i}\n";
			}
			var tv = new TextView () { Width = 10, Height = 10 };
			tv.Text = text;

			Assert.Equal (0, tv.TopRow);
			tv.PositionCursor ();
			Assert.Equal (CursorVisibility.Default, tv.DesiredCursorVisibility);

			for (int i = 0; i < 12; i++) {
				tv.MouseEvent (new MouseEvent () {
					Flags = MouseFlags.WheeledDown
				});
				tv.PositionCursor ();
				Assert.Equal (i + 1, tv.TopRow);
				Assert.Equal (CursorVisibility.Invisible, tv.DesiredCursorVisibility);
			}

			for (int i = 12; i > 0; i--) {
				tv.MouseEvent (new MouseEvent () {
					Flags = MouseFlags.WheeledUp
				});
				tv.PositionCursor ();
				Assert.Equal (i - 1, tv.TopRow);
				if (i - 1 == 0) {
					Assert.Equal (CursorVisibility.Default, tv.DesiredCursorVisibility);
				} else {
					Assert.Equal (CursorVisibility.Invisible, tv.DesiredCursorVisibility);
				}
			}
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void DesiredCursorVisibility_Horizontal_Navigation ()
		{
			string text = "";

			for (int i = 0; i < 12; i++) {
				text += $"{i.ToString () [^1]}";
			}
			var tv = new TextView () { Width = 10, Height = 10 };
			tv.Text = text;

			Assert.Equal (0, tv.LeftColumn);
			tv.PositionCursor ();
			Assert.Equal (CursorVisibility.Default, tv.DesiredCursorVisibility);

			for (int i = 0; i < 12; i++) {
				tv.MouseEvent (new MouseEvent () {
					Flags = MouseFlags.WheeledRight
				});
				tv.PositionCursor ();
				Assert.Equal (Math.Min (i + 1, 11), tv.LeftColumn);
				Assert.Equal (CursorVisibility.Invisible, tv.DesiredCursorVisibility);
			}

			for (int i = 11; i > 0; i--) {
				tv.MouseEvent (new MouseEvent () {
					Flags = MouseFlags.WheeledLeft
				});
				tv.PositionCursor ();
				Assert.Equal (i - 1, tv.LeftColumn);
				if (i - 1 == 0) {
					Assert.Equal (CursorVisibility.Default, tv.DesiredCursorVisibility);
				} else {
					Assert.Equal (CursorVisibility.Invisible, tv.DesiredCursorVisibility);
				}
			}
		}

		[Fact]
		public void LeftColumn_Add_One_If_Text_Length_Is_Equal_To_Width ()
		{
			var tv = new TextView () {
				Width = 10,
				Text = "1234567890"
			};

			Assert.Equal (Point.Empty, tv.CursorPosition);
			Assert.Equal (0, tv.LeftColumn);

			tv.CursorPosition = new Point (9, 0);
			Assert.Equal (new Point (9, 0), tv.CursorPosition);
			Assert.Equal (0, tv.LeftColumn);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			tv.CursorPosition = new Point (10, 0);
			Assert.Equal (new Point (10, 0), tv.CursorPosition);
			Assert.Equal (1, tv.LeftColumn);
		}

		[Fact]
		[AutoInitShutdown]
		public void KeyBindings_Command ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () {
				Width = 10,
				Height = 2,
				Text = text
			};
			var top = Application.Top;
			top.Add (tv);
			Application.Begin (top);

			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (Point.Empty, tv.CursorPosition);
			Assert.False (tv.ReadOnly);
			Assert.True (tv.CanFocus);

			var g = (SingleWordSuggestionGenerator)tv.Autocomplete.SuggestionGenerator;

			tv.CanFocus = false;
			Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			tv.CanFocus = true;
			Assert.False (tv.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.End | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (2, tv.CurrentRow);
			Assert.Equal (23, tv.CurrentColumn);
			Assert.Equal (tv.CurrentColumn, tv.GetCurrentLine ().Count);
			Assert.Equal (new Point (23, 2), tv.CursorPosition);
			Assert.False (tv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.NotNull (tv.Autocomplete);
			Assert.Empty (g.AllSuggestions);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.F, new KeyModifiers ())));
			tv.Redraw (tv.Bounds);
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.F", tv.Text);
			Assert.Equal (new Point (24, 2), tv.CursorPosition);
			Assert.Empty (tv.Autocomplete.Suggestions);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			tv.Redraw (tv.Bounds);
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (new Point (23, 2), tv.CursorPosition);
			Assert.Empty (tv.Autocomplete.Suggestions);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			tv.Redraw (tv.Bounds);
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.F", tv.Text);
			Assert.Equal (new Point (24, 2), tv.CursorPosition);
			Assert.Empty (tv.Autocomplete.Suggestions);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (new Point (23, 2), tv.CursorPosition);
			g.AllSuggestions = Regex.Matches (tv.Text.ToString (), "\\w+")
				.Select (s => s.Value)
				.Distinct ().ToList ();
			Assert.Equal (7, g.AllSuggestions.Count);
			Assert.Equal ("This", g.AllSuggestions [0]);
			Assert.Equal ("is", g.AllSuggestions [1]);
			Assert.Equal ("the", g.AllSuggestions [2]);
			Assert.Equal ("first", g.AllSuggestions [3]);
			Assert.Equal ("line", g.AllSuggestions [4]);
			Assert.Equal ("second", g.AllSuggestions [5]);
			Assert.Equal ("third", g.AllSuggestions [^1]);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.F, new KeyModifiers ())));
			tv.Redraw (tv.Bounds);
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.F", tv.Text);
			Assert.Equal (new Point (24, 2), tv.CursorPosition);
			Assert.Single (tv.Autocomplete.Suggestions);
			Assert.Equal ("first", tv.Autocomplete.Suggestions[0].Replacement);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (28, 2), tv.CursorPosition);
			Assert.Single (tv.Autocomplete.Suggestions);
			Assert.Equal ("first", tv.Autocomplete.Suggestions[0].Replacement);
			g.AllSuggestions = new List<string> ();
			tv.Autocomplete.ClearSuggestions ();
			Assert.Empty (g.AllSuggestions);
			Assert.Empty (tv.Autocomplete.Suggestions);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.PageUp, new KeyModifiers ())));
			Assert.Equal (24, tv.GetCurrentLine ().Count);
			Assert.Equal (new Point (24, 1), tv.CursorPosition);
			Assert.True (tv.ProcessKey (new KeyEvent (((int)'V' + Key.AltMask), new KeyModifiers ())));
			Assert.Equal (23, tv.GetCurrentLine ().Count);
			Assert.Equal (new Point (23, 0), tv.CursorPosition);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.PageDown, new KeyModifiers ())));
			Assert.Equal (24, tv.GetCurrentLine ().Count);
			Assert.Equal (new Point (23, 1), tv.CursorPosition); // gets the previous length
			Assert.True (tv.ProcessKey (new KeyEvent (Key.V | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (28, tv.GetCurrentLine ().Count);
			Assert.Equal (new Point (23, 2), tv.CursorPosition); // gets the previous length
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.PageUp | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal (24, tv.GetCurrentLine ().Count);
			Assert.Equal (new Point (23, 1), tv.CursorPosition); // gets the previous length
			Assert.Equal (24 + Environment.NewLine.Length, tv.SelectedLength);
			Assert.Equal ($".{Environment.NewLine}This is the third line.", tv.SelectedText);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.PageDown | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal (28, tv.GetCurrentLine ().Count);
			Assert.Equal (new Point (23, 2), tv.CursorPosition); // gets the previous length
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Home | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (Point.Empty, tv.CursorPosition);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.N | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.P | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorDown | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			Assert.Equal (23 + Environment.NewLine.Length, tv.SelectedLength);
			Assert.Equal ($"This is the first line.{Environment.NewLine}", tv.SelectedText);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorUp | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.F | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.B | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorRight | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.Equal (1, tv.SelectedLength);
			Assert.Equal ("T", tv.SelectedText);
			Assert.True (tv.Selecting);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorLeft | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.True (tv.Selecting);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.DeleteChar, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.DeleteChar, new KeyModifiers ())));
			Assert.Equal ($"his is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.D | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"is is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.Equal ($"is is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (21, 0), tv.CursorPosition);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Delete, new KeyModifiers ())));
			Assert.Equal ($"is is the first line{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (20, 0), tv.CursorPosition);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ())));
			Assert.Equal ($"is is the first lin{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (19, 0), tv.CursorPosition);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.End | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal (new Point (19, 0), tv.CursorPosition);
			Assert.Equal (19, tv.SelectedLength);
			Assert.Equal ("is is the first lin", tv.SelectedText);
			Assert.True (tv.Selecting);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Home | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.True (tv.Selecting);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.E | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (19, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.A | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.K | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			Assert.Equal ("is is the first lin", Clipboard.Contents);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Y | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"is is the first lin{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (19, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			Assert.Equal ("is is the first lin", Clipboard.Contents);
			tv.CursorPosition = Point.Empty;
			Assert.True (tv.ProcessKey (new KeyEvent (Key.DeleteChar | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			Assert.Equal ("is is the first lin", Clipboard.Contents);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Y | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"is is the first lin{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (19, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			Assert.Equal ("is is the first lin", Clipboard.Contents);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.K | Key.AltMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			tv.ReadOnly = true;
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Y | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			tv.ReadOnly = false;
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Y | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"is is the first lin{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (19, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			Assert.Equal (0, tv.SelectionStartColumn);
			Assert.Equal (0, tv.SelectionStartRow);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Space | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"is is the first lin{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (19, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.True (tv.Selecting);
			Assert.Equal (19, tv.SelectionStartColumn);
			Assert.Equal (0, tv.SelectionStartRow);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Space | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"is is the first lin{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (19, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			Assert.Equal (19, tv.SelectionStartColumn);
			Assert.Equal (0, tv.SelectionStartRow);
			tv.SelectionStartColumn = 0;
			Assert.True (tv.ProcessKey (new KeyEvent (((int)'C' + Key.AltMask), new KeyModifiers ())));
			Assert.Equal ($"is is the first lin{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (19, 0), tv.CursorPosition);
			Assert.Equal (19, tv.SelectedLength);
			Assert.Equal ("is is the first lin", tv.SelectedText);
			Assert.True (tv.Selecting);
			Assert.Equal (0, tv.SelectionStartColumn);
			Assert.Equal (0, tv.SelectionStartRow);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.C | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"is is the first lin{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (19, 0), tv.CursorPosition);
			Assert.Equal (19, tv.SelectedLength);
			Assert.Equal ("is is the first lin", tv.SelectedText);
			Assert.True (tv.Selecting);
			Assert.Equal (0, tv.SelectionStartColumn);
			Assert.Equal (0, tv.SelectionStartRow);
			Assert.True (tv.ProcessKey (new KeyEvent (((int)'W' + Key.AltMask), new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			Assert.Equal (0, tv.SelectionStartColumn);
			Assert.Equal (0, tv.SelectionStartRow);
			Assert.Equal ("is is the first lin", Clipboard.Contents);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.W | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			Assert.Equal (0, tv.SelectionStartColumn);
			Assert.Equal (0, tv.SelectionStartRow);
			Assert.Equal ("", Clipboard.Contents);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.X | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			Assert.Equal (0, tv.SelectionStartColumn);
			Assert.Equal (0, tv.SelectionStartRow);
			Assert.Equal ("", Clipboard.Contents);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.End | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (28, 2), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.CtrlMask | Key.CursorLeft, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (18, 2), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.CtrlMask | Key.CursorLeft | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (12, 2), tv.CursorPosition);
			Assert.Equal (6, tv.SelectedLength);
			Assert.Equal ("third ", tv.SelectedText);
			Assert.True (tv.Selecting);
			Assert.True (tv.ProcessKey (new KeyEvent ((Key)((int)'B' + Key.AltMask), new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (8, 2), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.CtrlMask | Key.CursorRight, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (12, 2), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.CtrlMask | Key.CursorRight | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (18, 2), tv.CursorPosition);
			Assert.Equal (6, tv.SelectedLength);
			Assert.Equal ("third ", tv.SelectedText);
			Assert.True (tv.Selecting);
			Assert.True (tv.ProcessKey (new KeyEvent ((Key)((int)'F' + Key.AltMask), new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (28, 2), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Home | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.DeleteChar | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting); Assert.True (tv.ProcessKey (new KeyEvent (Key.End | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
			Assert.Equal (new Point (28, 1), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			Assert.False (tv.Selecting); Assert.True (tv.ProcessKey (new KeyEvent (Key.Backspace | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the second line.{Environment.NewLine}This is the third ", tv.Text);
			Assert.Equal (new Point (18, 1), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			Assert.True (tv.AllowsReturn);
			tv.AllowsReturn = false;
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.False (tv.Selecting);
			Assert.False (tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ($"This is the second line.{Environment.NewLine}This is the third ", tv.Text);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			Assert.False (tv.AllowsReturn);
			tv.AllowsReturn = true;
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third ", tv.Text);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.Equal ("", tv.SelectedText);
			Assert.False (tv.Selecting);
			Assert.True (tv.AllowsReturn);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.CtrlMask | Key.End | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third ", tv.Text);
			Assert.Equal (new Point (18, 2), tv.CursorPosition);
			Assert.Equal (42 + Environment.NewLine.Length, tv.SelectedLength);
			Assert.Equal ($"This is the second line.{Environment.NewLine}This is the third ", tv.SelectedText);
			Assert.True (tv.Selecting);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.CtrlMask | Key.Home | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third ", tv.Text);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (Environment.NewLine.Length, tv.SelectedLength);
			Assert.Equal ($"{Environment.NewLine}", tv.SelectedText);
			Assert.True (tv.Selecting);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.T | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third ", tv.Text);
			Assert.Equal (new Point (18, 2), tv.CursorPosition);
			Assert.Equal (42 + Environment.NewLine.Length * 2, tv.SelectedLength);
			Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third ", tv.SelectedText);
			Assert.True (tv.Selecting);
			Assert.True (tv.Used);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.InsertChar, new KeyModifiers ())));
			Assert.False (tv.Used);
			Assert.True (tv.AllowsTab);
			Assert.Equal (new Point (18, 2), tv.CursorPosition);
			tv.AllowsTab = false;
			Assert.False (tv.ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third ", tv.Text);
			Assert.False (tv.AllowsTab);
			tv.AllowsTab = true;
			Assert.Equal (new Point (18, 2), tv.CursorPosition);
			Assert.True (tv.Selecting);
			tv.Selecting = false;
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third \t", tv.Text);
			Assert.True (tv.AllowsTab);
			tv.AllowsTab = false;
			Assert.False (tv.ProcessKey (new KeyEvent (Key.BackTab | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third \t", tv.Text);
			Assert.False (tv.AllowsTab);
			tv.AllowsTab = true;
			Assert.True (tv.ProcessKey (new KeyEvent (Key.BackTab | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third ", tv.Text);
			Assert.True (tv.AllowsTab);
			Assert.False (tv.ProcessKey (new KeyEvent (Key.Tab | Key.CtrlMask, new KeyModifiers ())));
			Assert.False (tv.ProcessKey (new KeyEvent (Application.AlternateForwardKey, new KeyModifiers ())));
			Assert.False (tv.ProcessKey (new KeyEvent (Key.Tab | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ())));
			Assert.False (tv.ProcessKey (new KeyEvent (Application.AlternateBackwardKey, new KeyModifiers ())));
		}

		[Fact]
		public void HistoryText_Exceptions ()
		{
			var ht = new HistoryText ();

			foreach (var ls in Enum.GetValues (typeof (HistoryText.LineStatus))) {
				if ((HistoryText.LineStatus)ls != HistoryText.LineStatus.Original) {
					Assert.Throws<ArgumentException> (() => ht.Add (new List<List<Rune>> () { new List<Rune> () }, Point.Empty,
						(HistoryText.LineStatus)ls));
				}
			}

			Assert.Null (Record.Exception (() => ht.Add (new List<List<Rune>> () { new List<Rune> () }, Point.Empty,
				HistoryText.LineStatus.Original)));
		}

		[Fact]
		[AutoInitShutdown]
		public void HistoryText_Undo_Redo_Single_Line_InsertText ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () {
				Width = 10,
				Height = 2,
				Text = text
			};
			var top = Application.Top;
			top.Add (tv);
			Application.Begin (top);

			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (Point.Empty, tv.CursorPosition);

			var messy = " messy";
			tv.CursorPosition = new Point (7, 1);
			tv.InsertText (messy);
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is messy the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (13, 1), tv.CursorPosition);

			for (int i = 0; i < messy.Length; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 1), tv.CursorPosition);

			for (int i = 0; i < messy.Length; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is messy the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (13, 1), tv.CursorPosition);
		}

		[Fact]
		[AutoInitShutdown]
		public void HistoryText_Undo_Redo_Single_Line_DeleteCharLeft ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () {
				Width = 10,
				Height = 2,
				Text = text
			};
			var top = Application.Top;
			top.Add (tv);
			Application.Begin (top);

			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (Point.Empty, tv.CursorPosition);

			var ntimes = 3;
			tv.CursorPosition = new Point (7, 1);
			for (int i = 0; i < ntimes; i++) {
				tv.DeleteCharLeft ();
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (4, 1), tv.CursorPosition);

			for (int i = 0; i < ntimes; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 1), tv.CursorPosition);

			for (int i = 0; i < ntimes; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (4, 1), tv.CursorPosition);
		}

		[Fact]
		[AutoInitShutdown]
		public void HistoryText_Undo_Redo_Single_Line_DeleteCharRight ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () {
				Width = 10,
				Height = 2,
				Text = text
			};
			var top = Application.Top;
			top.Add (tv);
			Application.Begin (top);

			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (Point.Empty, tv.CursorPosition);

			var ntimes = 3;
			tv.CursorPosition = new Point (7, 1);
			for (int i = 0; i < ntimes; i++) {
				tv.DeleteCharRight ();
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This ise second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 1), tv.CursorPosition);

			for (int i = 0; i < ntimes; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 1), tv.CursorPosition);

			for (int i = 0; i < ntimes; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This ise second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 1), tv.CursorPosition);
		}

		[Fact]
		[AutoInitShutdown]
		public void HistoryText_Undo_Redo_Single_Line_Selected_InsertText ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () {
				Width = 10,
				Height = 2,
				Text = text
			};
			var top = Application.Top;
			top.Add (tv);
			Application.Begin (top);

			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (Point.Empty, tv.CursorPosition);

			var messy = " messy";
			tv.CursorPosition = new Point (7, 1);
			tv.SelectionStartColumn = 11;
			tv.SelectionStartRow = 1;
			Assert.Equal (4, tv.SelectedLength);
			tv.InsertText (messy);
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is messy second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (13, 1), tv.CursorPosition);
			Assert.Equal (11, tv.SelectionStartColumn);
			Assert.Equal (1, tv.SelectionStartRow);
			Assert.Equal (0, tv.SelectedLength);

			for (int i = 0; i < messy.Length; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 1), tv.CursorPosition);
			Assert.Equal (11, tv.SelectionStartColumn);
			Assert.Equal (1, tv.SelectionStartRow);
			Assert.Equal (0, tv.SelectedLength);

			for (int i = 0; i < messy.Length; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is messy second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (13, 1), tv.CursorPosition);
			Assert.Equal (11, tv.SelectionStartColumn);
			Assert.Equal (1, tv.SelectionStartRow);
			Assert.Equal (0, tv.SelectedLength);
		}

		[Fact]
		[AutoInitShutdown]
		public void HistoryText_Undo_Redo_Single_Line_Selected_DeleteCharLeft ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () {
				Width = 10,
				Height = 2,
				Text = text
			};
			var top = Application.Top;
			top.Add (tv);
			Application.Begin (top);

			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (Point.Empty, tv.CursorPosition);

			var ntimes = 3;
			tv.CursorPosition = new Point (7, 1);
			tv.SelectionStartColumn = 11;
			tv.SelectionStartRow = 1;
			Assert.Equal (4, tv.SelectedLength);
			for (int i = 0; i < ntimes; i++) {
				tv.DeleteCharLeft ();
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This  second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (5, 1), tv.CursorPosition);
			Assert.Equal (11, tv.SelectionStartColumn);
			Assert.Equal (1, tv.SelectionStartRow);
			Assert.Equal (0, tv.SelectedLength);

			for (int i = 0; i < ntimes; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 1), tv.CursorPosition);
			Assert.Equal (11, tv.SelectionStartColumn);
			Assert.Equal (1, tv.SelectionStartRow);
			Assert.Equal (0, tv.SelectedLength);

			for (int i = 0; i < ntimes; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This  second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (5, 1), tv.CursorPosition);
			Assert.Equal (11, tv.SelectionStartColumn);
			Assert.Equal (1, tv.SelectionStartRow);
			Assert.Equal (0, tv.SelectedLength);
		}

		[Fact]
		[AutoInitShutdown]
		public void HistoryText_Undo_Redo_Single_Line_Selected_DeleteCharRight ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () {
				Width = 10,
				Height = 2,
				Text = text
			};
			var top = Application.Top;
			top.Add (tv);
			Application.Begin (top);

			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (Point.Empty, tv.CursorPosition);

			var ntimes = 3;
			tv.CursorPosition = new Point (7, 1);
			tv.SelectionStartColumn = 11;
			tv.SelectionStartRow = 1;
			Assert.Equal (4, tv.SelectedLength);
			for (int i = 0; i < ntimes; i++) {
				tv.DeleteCharRight ();
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This isecond line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 1), tv.CursorPosition);
			Assert.Equal (11, tv.SelectionStartColumn);
			Assert.Equal (1, tv.SelectionStartRow);
			Assert.Equal (0, tv.SelectedLength);

			for (int i = 0; i < ntimes; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 1), tv.CursorPosition);
			Assert.Equal (11, tv.SelectionStartColumn);
			Assert.Equal (1, tv.SelectionStartRow);
			Assert.Equal (0, tv.SelectedLength);

			for (int i = 0; i < ntimes; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This isecond line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 1), tv.CursorPosition);
			Assert.Equal (11, tv.SelectionStartColumn);
			Assert.Equal (1, tv.SelectionStartRow);
			Assert.Equal (0, tv.SelectedLength);
		}

		[Fact]
		[AutoInitShutdown]
		public void HistoryText_Undo_Redo_Multi_Line_InsertText ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () {
				Width = 10,
				Height = 2,
				Text = text
			};
			var top = Application.Top;
			top.Add (tv);
			Application.Begin (top);

			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (Point.Empty, tv.CursorPosition);

			var messy = " messy";
			tv.CursorPosition = new Point (7, 1);
			tv.InsertText (messy);
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is messy the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (13, 1), tv.CursorPosition);

			tv.CursorPosition = new Point (7, 0);
			tv.InsertText (messy);
			Assert.Equal ($"This is messy the first line.{Environment.NewLine}This is messy the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (13, 0), tv.CursorPosition);

			tv.CursorPosition = new Point (7, 2);
			tv.InsertText (messy);
			Assert.Equal ($"This is messy the first line.{Environment.NewLine}This is messy the second line.{Environment.NewLine}This is messy the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (13, 2), tv.CursorPosition);

			for (int i = 0; i < messy.Length; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This is messy the first line.{Environment.NewLine}This is messy the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 2), tv.CursorPosition);

			for (int i = 0; i < messy.Length; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is messy the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 0), tv.CursorPosition);

			for (int i = 0; i < messy.Length; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 1), tv.CursorPosition);

			for (int i = 0; i < messy.Length; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is messy the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (13, 1), tv.CursorPosition);

			for (int i = 0; i < messy.Length; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This is messy the first line.{Environment.NewLine}This is messy the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (13, 0), tv.CursorPosition);

			for (int i = 0; i < messy.Length; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This is messy the first line.{Environment.NewLine}This is messy the second line.{Environment.NewLine}This is messy the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (13, 2), tv.CursorPosition);
		}

		[Fact]
		[AutoInitShutdown]
		public void HistoryText_Undo_Redo_Multi_Line_DeleteCharLeft ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () {
				Width = 10,
				Height = 2,
				Text = text
			};
			var top = Application.Top;
			top.Add (tv);
			Application.Begin (top);

			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (Point.Empty, tv.CursorPosition);

			var ntimes = 3;
			tv.CursorPosition = new Point (7, 1);
			for (int i = 0; i < ntimes; i++) {
				tv.DeleteCharLeft ();
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (4, 1), tv.CursorPosition);

			tv.CursorPosition = new Point (7, 0);
			for (int i = 0; i < ntimes; i++) {
				tv.DeleteCharLeft ();
			}
			Assert.Equal ($"This the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (4, 0), tv.CursorPosition);

			tv.CursorPosition = new Point (7, 2);
			for (int i = 0; i < ntimes; i++) {
				tv.DeleteCharLeft ();
			}
			Assert.Equal ($"This the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (4, 2), tv.CursorPosition);

			for (int i = 0; i < ntimes; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));

				switch (i) {
				case 0:
					Assert.Equal ($"This the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This  the third line.", tv.Text);
					Assert.Equal (new Point (5, 2), tv.CursorPosition);
					break;
				case 1:
					Assert.Equal ($"This the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This i the third line.", tv.Text);
					Assert.Equal (new Point (6, 2), tv.CursorPosition);
					break;
				case 2:
					Assert.Equal ($"This the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This is the third line.", tv.Text);
					Assert.Equal (new Point (7, 2), tv.CursorPosition);
					break;
				}
			}
			Assert.Equal ($"This the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 2), tv.CursorPosition);

			for (int i = 0; i < ntimes; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));

				switch (i) {
				case 0:
					Assert.Equal ($"This  the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This is the third line.", tv.Text);
					Assert.Equal (new Point (5, 0), tv.CursorPosition);
					break;
				case 1:
					Assert.Equal ($"This i the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This is the third line.", tv.Text);
					Assert.Equal (new Point (6, 0), tv.CursorPosition);
					break;
				case 2:
					Assert.Equal ($"This is the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This is the third line.", tv.Text);
					Assert.Equal (new Point (7, 0), tv.CursorPosition);
					break;
				}
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 0), tv.CursorPosition);

			for (int i = 0; i < ntimes; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));

				switch (i) {
				case 0:
					Assert.Equal ($"This is the first line.{Environment.NewLine}This  the second line.{Environment.NewLine}This is the third line.", tv.Text);
					Assert.Equal (new Point (5, 1), tv.CursorPosition);
					break;
				case 1:
					Assert.Equal ($"This is the first line.{Environment.NewLine}This i the second line.{Environment.NewLine}This is the third line.", tv.Text);
					Assert.Equal (new Point (6, 1), tv.CursorPosition);
					break;
				case 2:
					Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
					Assert.Equal (new Point (7, 1), tv.CursorPosition);
					break;
				}
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 1), tv.CursorPosition);

			for (int i = 0; i < ntimes; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (4, 1), tv.CursorPosition);

			for (int i = 0; i < ntimes; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (4, 0), tv.CursorPosition);

			for (int i = 0; i < ntimes; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This the first line.{Environment.NewLine}This the second line.{Environment.NewLine}This the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (4, 2), tv.CursorPosition);
		}

		[Fact]
		[AutoInitShutdown]
		public void HistoryText_Undo_Redo_Multi_Line_DeleteCharRight ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () {
				Width = 10,
				Height = 2,
				Text = text
			};
			var top = Application.Top;
			top.Add (tv);
			Application.Begin (top);

			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (Point.Empty, tv.CursorPosition);

			var ntimes = 3;
			tv.CursorPosition = new Point (7, 1);
			for (int i = 0; i < ntimes; i++) {
				tv.DeleteCharRight ();
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This ise second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 1), tv.CursorPosition);

			tv.CursorPosition = new Point (7, 0);
			for (int i = 0; i < ntimes; i++) {
				tv.DeleteCharRight ();
			}
			Assert.Equal ($"This ise first line.{Environment.NewLine}This ise second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 0), tv.CursorPosition);

			tv.CursorPosition = new Point (7, 2);
			for (int i = 0; i < ntimes; i++) {
				tv.DeleteCharRight ();
			}
			Assert.Equal ($"This ise first line.{Environment.NewLine}This ise second line.{Environment.NewLine}This ise third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 2), tv.CursorPosition);

			for (int i = 0; i < ntimes; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This ise first line.{Environment.NewLine}This ise second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 2), tv.CursorPosition);

			for (int i = 0; i < ntimes; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This ise second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 0), tv.CursorPosition);

			for (int i = 0; i < ntimes; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 1), tv.CursorPosition);

			for (int i = 0; i < ntimes; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This ise second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 1), tv.CursorPosition);

			for (int i = 0; i < ntimes; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This ise first line.{Environment.NewLine}This ise second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 0), tv.CursorPosition);

			for (int i = 0; i < ntimes; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal ($"This ise first line.{Environment.NewLine}This ise second line.{Environment.NewLine}This ise third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 2), tv.CursorPosition);
		}

		[Fact]
		[AutoInitShutdown]
		public void HistoryText_Undo_Redo_Multi_Line_Selected_InsertText ()
		{
			var text = $"This is the first line.{Environment.NewLine}This is the second line.\nThis is the third line.";
			var tv = new TextView () {
				Width = 10,
				Height = 2,
				Text = text
			};
			var top = Application.Top;
			top.Add (tv);
			Application.Begin (top);

			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (Point.Empty, tv.CursorPosition);

			var messy = " messy";
			tv.CursorPosition = new Point (7, 0);
			tv.SelectionStartColumn = 11;
			tv.SelectionStartRow = 2;
			Assert.Equal (51 + Environment.NewLine.Length * 2, tv.SelectedLength);
			for (int i = 0; i < messy.Length; i++) {
				tv.InsertText (messy [i].ToString ());

				switch (i) {
				case 0:
					Assert.Equal ("This is  third line.", tv.Text);
					Assert.Equal (new Point (8, 0), tv.CursorPosition);
					break;
				case 1:
					Assert.Equal ("This is m third line.", tv.Text);
					Assert.Equal (new Point (9, 0), tv.CursorPosition);
					break;
				case 2:
					Assert.Equal ("This is me third line.", tv.Text);
					Assert.Equal (new Point (10, 0), tv.CursorPosition);
					break;
				case 3:
					Assert.Equal ("This is mes third line.", tv.Text);
					Assert.Equal (new Point (11, 0), tv.CursorPosition);
					break;
				case 4:
					Assert.Equal ("This is mess third line.", tv.Text);
					Assert.Equal (new Point (12, 0), tv.CursorPosition);
					break;
				case 5:
					Assert.Equal ("This is messy third line.", tv.Text);
					Assert.Equal (new Point (13, 0), tv.CursorPosition);
					break;
				}
			}

			Assert.Equal ($"This is messy third line.", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (13, 0), tv.CursorPosition);
			Assert.Equal (11, tv.SelectionStartColumn);
			Assert.Equal (2, tv.SelectionStartRow);
			Assert.Equal (0, tv.SelectedLength);

			for (int i = 0; i < messy.Length; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));

				switch (i) {
				case 0:
					Assert.Equal ("This is mess third line.", tv.Text);
					Assert.Equal (new Point (12, 0), tv.CursorPosition);
					break;
				case 1:
					Assert.Equal ("This is mes third line.", tv.Text);
					Assert.Equal (new Point (11, 0), tv.CursorPosition);
					break;
				case 2:
					Assert.Equal ("This is me third line.", tv.Text);
					Assert.Equal (new Point (10, 0), tv.CursorPosition);
					break;
				case 3:
					Assert.Equal ("This is m third line.", tv.Text);
					Assert.Equal (new Point (9, 0), tv.CursorPosition);
					break;
				case 4:
					Assert.Equal ("This is  third line.", tv.Text);
					Assert.Equal (new Point (8, 0), tv.CursorPosition);
					break;
				case 5:
					Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
					Assert.Equal (new Point (7, 0), tv.CursorPosition);
					break;
				}
			}
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 0), tv.CursorPosition);
			Assert.Equal (11, tv.SelectionStartColumn);
			Assert.Equal (2, tv.SelectionStartRow);
			Assert.Equal (0, tv.SelectedLength);

			for (int i = 0; i < messy.Length; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));

				switch (i) {
				case 0:
					Assert.Equal ("This is  third line.", tv.Text);
					Assert.Equal (new Point (8, 0), tv.CursorPosition);
					break;
				case 1:
					Assert.Equal ("This is m third line.", tv.Text);
					Assert.Equal (new Point (9, 0), tv.CursorPosition);
					break;
				case 2:
					Assert.Equal ("This is me third line.", tv.Text);
					Assert.Equal (new Point (10, 0), tv.CursorPosition);
					break;
				case 3:
					Assert.Equal ("This is mes third line.", tv.Text);
					Assert.Equal (new Point (11, 0), tv.CursorPosition);
					break;
				case 4:
					Assert.Equal ("This is mess third line.", tv.Text);
					Assert.Equal (new Point (12, 0), tv.CursorPosition);
					break;
				case 5:
					Assert.Equal ("This is messy third line.", tv.Text);
					Assert.Equal (new Point (13, 0), tv.CursorPosition);
					break;
				}
			}
			Assert.Equal ("This is messy third line.", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (13, 0), tv.CursorPosition);
			Assert.Equal (11, tv.SelectionStartColumn);
			Assert.Equal (2, tv.SelectionStartRow);
			Assert.Equal (0, tv.SelectedLength);
		}

		[Fact]
		[AutoInitShutdown]
		public void HistoryText_Undo_Redo_Multi_Line_With_Empty_Text ()
		{
			var tv = new TextView () {
				Width = 10,
				Height = 2
			};
			var top = Application.Top;
			top.Add (tv);
			Application.Begin (top);

			Assert.Equal ("", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (Point.Empty, tv.CursorPosition);
			Assert.False (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.O, new KeyModifiers ())));
			Assert.Equal ("O", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.n, new KeyModifiers ())));
			Assert.Equal ("On", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (2, 0), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.e, new KeyModifiers ())));
			Assert.Equal ("One", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (3, 0), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.T, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}T", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (1, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.w, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Tw", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (2, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.o, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (3, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (0, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.T, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}T", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (1, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.h, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Th", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (2, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.r, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Thr", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (3, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.e, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Thre", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (4, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.e, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (5, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 3), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			// Undoing
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (5, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Thre", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (4, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Thr", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (3, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Th", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (2, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}T", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (1, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (0, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (3, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Tw", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (2, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}T", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (1, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (3, 0), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"On", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (2, 0), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"O", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.False (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.False (tv.IsDirty);

			// Redoing
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"O", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"On", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (2, 0), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (3, 0), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}T", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (1, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Tw", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (2, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (3, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (0, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}T", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (1, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Th", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (2, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Thr", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (3, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Thre", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (4, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (5, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 3), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 3), tv.CursorPosition);
			Assert.True (tv.IsDirty);
		}

		[Fact]
		[AutoInitShutdown]
		public void HistoryText_Undo_Redo_Multi_Line_Selected_With_Empty_Text ()
		{
			var tv = new TextView () {
				Width = 10,
				Height = 2
			};
			var top = Application.Top;
			top.Add (tv);
			Application.Begin (top);

			Assert.Equal ("", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (Point.Empty, tv.CursorPosition);
			Assert.False (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.O, new KeyModifiers ())));
			Assert.Equal ("O", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.n, new KeyModifiers ())));
			Assert.Equal ("On", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (2, 0), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.e, new KeyModifiers ())));
			Assert.Equal ("One", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (3, 0), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.T, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}T", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (1, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.w, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Tw", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (2, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.o, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (3, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (0, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.T, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}T", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (1, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.h, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Th", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (2, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.r, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Thr", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (3, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.e, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Thre", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (4, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.e, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (5, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 3), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			tv.SelectionStartColumn = 0;
			tv.SelectionStartRow = 0;
			tv.CursorPosition = new Point (0, 1);
			Assert.Equal (3 + Environment.NewLine.Length, tv.SelectedLength);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers ())));
			Assert.Equal ($"1Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			tv.SelectionStartColumn = 1;
			tv.SelectionStartRow = 0;
			tv.CursorPosition = new Point (1, 1);
			Assert.Equal (4 + Environment.NewLine.Length, tv.SelectedLength);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.D2, new KeyModifiers ())));
			Assert.Equal ($"12hree{Environment.NewLine}", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (2, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			// Undoing
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"1Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (1, 1), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (5, 2), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Thre", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (4, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Thr", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (3, 2), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Th", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (2, 2), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}T", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (1, 2), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (0, 2), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (3, 1), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Tw", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (2, 1), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}T", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (1, 1), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (3, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"On", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (2, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"O", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.False (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.False (tv.IsDirty);

			// Redoing
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"O", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"On", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (2, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (3, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}T", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (1, 1), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Tw", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (2, 1), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (3, 1), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (0, 2), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}T", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (1, 2), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Th", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (2, 2), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Thr", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (3, 2), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Thre", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (4, 2), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (5, 2), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 3), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"1Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"12hree{Environment.NewLine}", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (2, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);
		}

		[Fact]
		[AutoInitShutdown]
		public void HistoryText_Undo_Redo_Multi_Line_Selected_InsertText_Twice_On_Same_Line ()
		{
			var text = "One\nTwo\nThree";
			var tv = new TextView () {
				Width = 10,
				Height = 2,
				Text = text
			};
			var top = Application.Top;
			top.Add (tv);
			Application.Begin (top);

			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (Point.Empty, tv.CursorPosition);
			Assert.False (tv.IsDirty);

			tv.SelectionStartColumn = 0;
			tv.SelectionStartRow = 0;
			tv.CursorPosition = new Point (0, 1);
			Assert.Equal (3 + Environment.NewLine.Length, tv.SelectedLength);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers ())));
			Assert.Equal ($"1Two{Environment.NewLine}Three", tv.Text);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			tv.SelectionStartColumn = 1;
			tv.SelectionStartRow = 0;
			tv.CursorPosition = new Point (1, 1);
			Assert.Equal (4 + Environment.NewLine.Length, tv.SelectedLength);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.D2, new KeyModifiers ())));
			Assert.Equal ("12hree", tv.Text);
			Assert.Equal (new Point (2, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"1Two{Environment.NewLine}Three", tv.Text);
			Assert.Equal (new Point (1, 1), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.False (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"1Two{Environment.NewLine}Three", tv.Text);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("12hree", tv.Text);
			Assert.Equal (new Point (2, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);
		}

		[Fact]
		[AutoInitShutdown]
		public void HistoryText_Undo_Redo_Multi_Line_Selected_InsertText_Twice_On_Same_Line_With_End_Line ()
		{
			var text = "One\nTwo\nThree\n";
			var tv = new TextView () {
				Width = 10,
				Height = 2,
				Text = text
			};
			var top = Application.Top;
			top.Add (tv);
			Application.Begin (top);

			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (Point.Empty, tv.CursorPosition);
			Assert.False (tv.IsDirty);

			tv.SelectionStartColumn = 0;
			tv.SelectionStartRow = 0;
			tv.CursorPosition = new Point (0, 1);
			Assert.Equal (3 + Environment.NewLine.Length, tv.SelectedLength);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers ())));
			Assert.Equal ($"1Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			tv.SelectionStartColumn = 1;
			tv.SelectionStartRow = 0;
			tv.CursorPosition = new Point (1, 1);
			Assert.Equal (4 + Environment.NewLine.Length, tv.SelectedLength);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.D2, new KeyModifiers ())));
			Assert.Equal ($"12hree{Environment.NewLine}", tv.Text);
			Assert.Equal (new Point (2, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"1Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
			Assert.Equal (new Point (1, 1), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"One{Environment.NewLine}Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.False (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"1Two{Environment.NewLine}Three{Environment.NewLine}", tv.Text);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"12hree{Environment.NewLine}", tv.Text);
			Assert.Equal (new Point (2, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);
		}

		[Fact]
		public void HistoryText_IsDirty_HasHistoryChanges ()
		{
			var tv = new TextView ();

			Assert.Equal ("", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (Point.Empty, tv.CursorPosition);
			Assert.False (tv.IsDirty);
			Assert.False (tv.HasHistoryChanges);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers ())));
			Assert.Equal ("1", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.True (tv.IsDirty);
			Assert.True (tv.HasHistoryChanges);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ($"1{Environment.NewLine}", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);
			Assert.True (tv.HasHistoryChanges);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.D2, new KeyModifiers ())));
			Assert.Equal ($"1{Environment.NewLine}2", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (1, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);
			Assert.True (tv.HasHistoryChanges);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ())));
			Assert.Equal ($"1{Environment.NewLine}", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);
			Assert.True (tv.HasHistoryChanges);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ())));
			Assert.Equal ($"1", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.True (tv.IsDirty);
			Assert.True (tv.HasHistoryChanges);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ())));
			Assert.Equal ($"", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			// IsDirty cannot be based on HasHistoryChanges because HasHistoryChanges is greater than 0
			// The only way is comparing from the original text
			Assert.False (tv.IsDirty);
			// Still true because HasHistoryChanges is greater than 0
			Assert.True (tv.HasHistoryChanges);
		}

		[Fact]
		public void HistoryText_Undo_Redo_Multi_Line_Selected_DeleteCharLeft_All ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () { Text = text };

			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (Point.Empty, tv.CursorPosition);
			Assert.False (tv.IsDirty);
			Assert.False (tv.HasHistoryChanges);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.End | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.SelectedText);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (23, 2), tv.CursorPosition);
			Assert.Equal (70 + Environment.NewLine.Length * 2, tv.SelectedLength);
			Assert.False (tv.IsDirty);
			Assert.False (tv.HasHistoryChanges);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ())));
			Assert.Equal ("", tv.Text);
			Assert.Equal ("", tv.SelectedText);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);
			Assert.True (tv.HasHistoryChanges);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal ("", tv.SelectedText);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (23, 2), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.False (tv.IsDirty);
			Assert.True (tv.HasHistoryChanges);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("", tv.Text);
			Assert.Equal ("", tv.SelectedText);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);
			Assert.True (tv.HasHistoryChanges);
		}

		[Fact]
		public void HistoryText_Undo_Redo_Multi_Line_Selected_DeleteCharRight_All ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () { Text = text };

			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (Point.Empty, tv.CursorPosition);
			Assert.False (tv.IsDirty);
			Assert.False (tv.HasHistoryChanges);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.End | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.SelectedText);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (23, 2), tv.CursorPosition);
			Assert.Equal (70 + Environment.NewLine.Length * 2, tv.SelectedLength);
			Assert.False (tv.IsDirty);
			Assert.False (tv.HasHistoryChanges);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.DeleteChar, new KeyModifiers ())));
			Assert.Equal ("", tv.Text);
			Assert.Equal ("", tv.SelectedText);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);
			Assert.True (tv.HasHistoryChanges);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal ("", tv.SelectedText);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (23, 2), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.False (tv.IsDirty);
			Assert.True (tv.HasHistoryChanges);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("", tv.Text);
			Assert.Equal ("", tv.SelectedText);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.Equal (0, tv.SelectedLength);
			Assert.True (tv.IsDirty);
			Assert.True (tv.HasHistoryChanges);
		}

		[Fact]
		[AutoInitShutdown]
		public void HistoryText_Undo_Redo_Copy_Without_Selection_Multi_Line_Paste ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () { Text = text };

			tv.CursorPosition = new Point (23, 0);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.C | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal ("", tv.SelectedText);
			Assert.Equal ("This is the first line.", Clipboard.Contents);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (23, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Y | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (23, 1), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (23, 0), tv.CursorPosition);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (23, 1), tv.CursorPosition);
		}

		[Fact]
		[AutoInitShutdown]
		public void HistoryText_Undo_Redo_Simple_Copy_Multi_Line_Selected_Paste ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () { Text = text };

			tv.SelectionStartColumn = 12;
			tv.CursorPosition = new Point (17, 0);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.C | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal ("first", tv.SelectedText);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (17, 0), tv.CursorPosition);

			tv.SelectionStartColumn = 12;
			tv.CursorPosition = new Point (11, 1);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Y | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (17, 0), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (12, 0), tv.CursorPosition);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (17, 0), tv.CursorPosition);
		}

		[Fact]
		[AutoInitShutdown]
		public void HistoryText_Undo_Redo_Multi_Line_Selected_Copy_Simple_Paste_Starting_On_Space ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () { Text = text };

			tv.SelectionStartColumn = 12;
			tv.CursorPosition = new Point (18, 1);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.C | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal ($"first line.{Environment.NewLine}This is the second", tv.SelectedText);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (18, 1), tv.CursorPosition);

			tv.Selecting = false;

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Y | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the secondfirst line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (18, 2), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (18, 1), tv.CursorPosition);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the secondfirst line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (18, 2), tv.CursorPosition);
		}

		[Fact]
		[AutoInitShutdown]
		public void HistoryText_Undo_Redo_Multi_Line_Selected_Copy_Simple_Paste_Starting_On_Letter ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () { Text = text };

			tv.SelectionStartColumn = 12;
			tv.CursorPosition = new Point (18, 1);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.C | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal ($"first line.{Environment.NewLine}This is the second", tv.SelectedText);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (18, 1), tv.CursorPosition);

			tv.Selecting = false;
			tv.CursorPosition = new Point (17, 1);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Y | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the seconfirst line.{Environment.NewLine}This is the secondd line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (18, 2), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (17, 1), tv.CursorPosition);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the seconfirst line.{Environment.NewLine}This is the secondd line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (18, 2), tv.CursorPosition);
		}

		[Fact]
		[AutoInitShutdown]
		public void HistoryText_Undo_Redo_Empty_Copy_Without_Selection_Multi_Line_Selected_Paste ()
		{
			var text = "\nThis is the first line.\nThis is the second line.";
			var tv = new TextView () { Text = text };

			Assert.True (tv.ProcessKey (new KeyEvent (Key.C | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the first line.{Environment.NewLine}This is the second line.", tv.Text);
			Assert.Equal ("", tv.SelectedText);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Y | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}{Environment.NewLine}This is the first line.{Environment.NewLine}This is the second line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the first line.{Environment.NewLine}This is the second line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}{Environment.NewLine}This is the first line.{Environment.NewLine}This is the second line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
		}

		[Fact]
		[AutoInitShutdown]
		public void HistoryText_Undo_Redo_Setting_Clipboard_Multi_Line_Selected_Paste ()
		{
			var text = "This is the first line.\nThis is the second line.";
			var tv = new TextView () { Text = text };

			Clipboard.Contents = "Inserted\nNewLine";

			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.", tv.Text);
			Assert.Equal ("", tv.SelectedText);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Y | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"Inserted{Environment.NewLine}NewLineThis is the first line.{Environment.NewLine}This is the second line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 1), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"Inserted{Environment.NewLine}NewLineThis is the first line.{Environment.NewLine}This is the second line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (7, 1), tv.CursorPosition);
		}

		[Fact]
		[AutoInitShutdown]
		public void HistoryText_Undo_Redo_Cut_Multi_Line_Selected_Paste ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () { Text = text };

			tv.SelectionStartColumn = 12;
			tv.CursorPosition = new Point (17, 0);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.W | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the  line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal ("", tv.SelectedText);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (12, 0), tv.CursorPosition);

			tv.SelectionStartColumn = 12;
			tv.CursorPosition = new Point (11, 1);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Y | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (17, 0), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the  line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (12, 0), tv.CursorPosition);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (17, 0), tv.CursorPosition);
		}

		[Fact]
		[AutoInitShutdown]
		public void HistoryText_Undo_Redo_Cut_Simple_Paste_Starting ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () { Text = text };

			tv.SelectionStartColumn = 12;
			tv.CursorPosition = new Point (18, 1);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.W | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the  line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal ("", tv.SelectedText);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (12, 0), tv.CursorPosition);

			tv.Selecting = false;

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Y | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (18, 1), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the  line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (12, 0), tv.CursorPosition);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (18, 1), tv.CursorPosition);
		}

		[Fact]
		[AutoInitShutdown]
		public void HistoryText_Undo_Redo_Cut_Multi_Line_Another_Selected_Paste ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () { Text = text };

			tv.SelectionStartColumn = 12;
			tv.CursorPosition = new Point (17, 0);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.W | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the  line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal ("", tv.SelectedText);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (12, 0), tv.CursorPosition);

			tv.SelectionStartColumn = 12;
			tv.SelectionStartRow = 1;
			tv.CursorPosition = new Point (18, 1);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Y | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the  line.{Environment.NewLine}This is the first line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (17, 1), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the  line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (12, 1), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (12, 0), tv.CursorPosition);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the  line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (12, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the  line.{Environment.NewLine}This is the first line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (17, 1), tv.CursorPosition);
		}

		[Fact]
		public void HistoryText_Undo_Redo_KillWordBackward ()
		{
			var text = "First line.\nSecond line.";
			var tv = new TextView () { Text = text };

			Assert.True (tv.ProcessKey (new KeyEvent (Key.End | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"First line.{Environment.NewLine}Second line.", tv.Text);
			Assert.Equal ("", tv.SelectedText);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (12, 1), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Backspace | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"First line.{Environment.NewLine}Second ", tv.Text);
			Assert.Equal ("", tv.SelectedText);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (7, 1), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Backspace | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"First line.{Environment.NewLine}", tv.Text);
			Assert.Equal ("", tv.SelectedText);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Backspace | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("First line.", tv.Text);
			Assert.Equal ("", tv.SelectedText);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (11, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Backspace | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("First ", tv.Text);
			Assert.Equal ("", tv.SelectedText);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (6, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Backspace | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("", tv.Text);
			Assert.Equal ("", tv.SelectedText);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("First ", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (6, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("First line.", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (11, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"First line.{Environment.NewLine}", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"First line.{Environment.NewLine}Second ", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (7, 1), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"First line.{Environment.NewLine}Second line.", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (12, 1), tv.CursorPosition);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"First line.{Environment.NewLine}Second ", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (7, 1), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"First line.{Environment.NewLine}", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("First line.", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (11, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("First ", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (6, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
		}

		[Fact]
		public void HistoryText_Undo_Redo_KillWordForward ()
		{
			var text = "First line.\nSecond line.";
			var tv = new TextView () { Text = text };

			Assert.True (tv.ProcessKey (new KeyEvent (Key.DeleteChar | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"line.{Environment.NewLine}Second line.", tv.Text);
			Assert.Equal ("", tv.SelectedText);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.DeleteChar | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}Second line.", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.DeleteChar | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("Second line.", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.DeleteChar | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("line.", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.DeleteChar | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("line.", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("Second line.", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}Second line.", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"line.{Environment.NewLine}Second line.", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"First line.{Environment.NewLine}Second line.", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"line.{Environment.NewLine}Second line.", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}Second line.", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("Second line.", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("line.", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
		}

		[Fact]
		[AutoInitShutdown]
		public void HistoryText_Undo_Redo_KillToStartOfLine ()
		{
			var text = "First line.\nSecond line.";
			var tv = new TextView () { Text = text };

			Assert.True (tv.ProcessKey (new KeyEvent (Key.End | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"First line.{Environment.NewLine}Second line.", tv.Text);
			Assert.Equal ("", tv.SelectedText);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (12, 1), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.K | Key.AltMask, new KeyModifiers ())));
			Assert.Equal ($"First line.{Environment.NewLine}", tv.Text);
			Assert.Equal ("", tv.SelectedText);
			Assert.Equal ("Second line.", Clipboard.Contents);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.K | Key.AltMask, new KeyModifiers ())));
			Assert.Equal ("First line.", tv.Text);
			Assert.Equal ("", tv.SelectedText);
			Assert.Equal ($"Second line.{Environment.NewLine}", Clipboard.Contents);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (11, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.K | Key.AltMask, new KeyModifiers ())));
			Assert.Equal ("", tv.Text);
			Assert.Equal ("", tv.SelectedText);
			Assert.Equal ($"Second line.{Environment.NewLine}First line.", Clipboard.Contents);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);


			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("First line.", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (11, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"First line.{Environment.NewLine}", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"First line.{Environment.NewLine}Second line.", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (12, 1), tv.CursorPosition);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"First line.{Environment.NewLine}", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"First line.", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (11, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
		}

		[Fact]
		[AutoInitShutdown]
		public void HistoryText_Undo_Redo_KillToEndOfLine ()
		{
			var text = "First line.\nSecond line.";
			var tv = new TextView () { Text = text };

			Assert.True (tv.ProcessKey (new KeyEvent (Key.K | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}Second line.", tv.Text);
			Assert.Equal ("", tv.SelectedText);
			Assert.Equal ("First line.", Clipboard.Contents);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.K | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("Second line.", tv.Text);
			Assert.Equal ("", tv.SelectedText);
			Assert.Equal ($"First line.{Environment.NewLine}", Clipboard.Contents);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.K | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("", tv.Text);
			Assert.Equal ("", tv.SelectedText);
			Assert.Equal ($"First line.{Environment.NewLine}Second line.", Clipboard.Contents);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("Second line.", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}Second line.", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"First line.{Environment.NewLine}Second line.", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}Second line.", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("Second line.", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
		}

		[Fact]
		public void HistoryText_Undo_Redo_Changing_On_Middle_Clear_History_Forwards ()
		{
			var tv = new TextView ();

			Assert.True (tv.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers ())));
			Assert.Equal ("1", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.D2, new KeyModifiers ())));
			Assert.Equal ("12", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (2, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.D3, new KeyModifiers ())));
			Assert.Equal ("123", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (3, 0), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("12", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (2, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.D4, new KeyModifiers ())));
			Assert.Equal ("124", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (3, 0), tv.CursorPosition);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("124", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (3, 0), tv.CursorPosition);
		}

		[Fact]
		public void HistoryText_Undo_Redo_Single_Line_Selected_Return ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () { Text = text };

			tv.SelectionStartColumn = 12;
			tv.CursorPosition = new Point (17, 0);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (17, 0), tv.CursorPosition);
			Assert.False (tv.IsDirty);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (17, 0), tv.CursorPosition);
			Assert.False (tv.IsDirty);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
		}

		[Fact]
		public void HistoryText_Undo_Redo_Two_Line_Selected_Return ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () { Text = text };

			tv.SelectionStartColumn = 12;
			tv.CursorPosition = new Point (18, 1);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (18, 1), tv.CursorPosition);
			Assert.False (tv.IsDirty);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (18, 1), tv.CursorPosition);
			Assert.False (tv.IsDirty);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
		}

		[Fact]
		public void HistoryText_Undo_Redo_Three_Line_Selected_Return ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () { Text = text };

			tv.SelectionStartColumn = 12;
			tv.CursorPosition = new Point (17, 2);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine} line.", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (17, 2), tv.CursorPosition);
			Assert.False (tv.IsDirty);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine} line.", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (17, 2), tv.CursorPosition);
			Assert.False (tv.IsDirty);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine} line.", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
		}

		[Fact]
		public void HistoryText_Undo_Redo_Single_Second_Line_Selected_Return ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () { Text = text };

			tv.SelectionStartColumn = 12;
			tv.SelectionStartRow = 1;
			tv.CursorPosition = new Point (18, 1);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 2), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (18, 1), tv.CursorPosition);
			Assert.False (tv.IsDirty);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 2), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (18, 1), tv.CursorPosition);
			Assert.False (tv.IsDirty);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 2), tv.CursorPosition);
		}

		[Fact]
		public void HistoryText_Undo_Redo_First_Line_Selected_Return_And_InsertText ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () { Text = text };

			tv.SelectionStartColumn = 12;
			tv.CursorPosition = new Point (17, 0);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.a, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine}a line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (1, 1), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (17, 0), tv.CursorPosition);
			Assert.False (tv.IsDirty);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine}a line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (1, 1), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (17, 0), tv.CursorPosition);
			Assert.False (tv.IsDirty);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine}a line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (1, 1), tv.CursorPosition);
		}

		[Fact]
		public void HistoryText_Undo_Redo_Single_Second_Line_Selected_Return_And_InsertText ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () { Text = text };

			tv.SelectionStartColumn = 12;
			tv.SelectionStartRow = 1;
			tv.CursorPosition = new Point (18, 1);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 2), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.a, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine}a line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (1, 2), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (18, 1), tv.CursorPosition);
			Assert.False (tv.IsDirty);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 2), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine}a line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (1, 2), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 2), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (18, 1), tv.CursorPosition);
			Assert.False (tv.IsDirty);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 2), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine}a line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (1, 2), tv.CursorPosition);
		}

		[Fact]
		public void HistoryText_Undo_Redo_Multi_Line_Selected_All_Return_And_InsertText ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () { Text = text };

			Assert.True (tv.ProcessKey (new KeyEvent (Key.End | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (23, 2), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.a, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}a", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (1, 1), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (23, 2), tv.CursorPosition);
			Assert.False (tv.IsDirty);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}a", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (1, 1), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (23, 2), tv.CursorPosition);
			Assert.False (tv.IsDirty);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}a", tv.Text);
			Assert.Equal (2, tv.Lines);
			Assert.Equal (new Point (1, 1), tv.CursorPosition);
		}

		[Fact]
		public void HistoryText_Undo_Redo_Ending_With_Newline_Multi_Line_Selected_Almost_All_Return_And_InsertText ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.\n";
			var tv = new TextView () { Text = text };

			tv.SelectionStartColumn = 12;
			tv.CursorPosition = new Point (12, 2);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.a, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine}athird line.{Environment.NewLine}", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (1, 1), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.{Environment.NewLine}", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (12, 2), tv.CursorPosition);
			Assert.False (tv.IsDirty);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine}athird line.{Environment.NewLine}", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (1, 1), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.{Environment.NewLine}", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (12, 2), tv.CursorPosition);
			Assert.False (tv.IsDirty);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine}athird line.{Environment.NewLine}", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (1, 1), tv.CursorPosition);
		}

		[Fact]
		public void HistoryText_Undo_Redo_Disabled_On_WordWrap ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.\n";
			var tv = new TextView () { Width = 80, Height = 5, Text = text };

			Assert.False (tv.WordWrap);
			tv.WordWrap = true;

			tv.SelectionStartColumn = 12;
			tv.CursorPosition = new Point (12, 2);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.a, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine}athird line.{Environment.NewLine}", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (1, 1), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"This is the {Environment.NewLine}athird line.{Environment.NewLine}", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (1, 1), tv.CursorPosition);
		}

		[Fact]
		public void HistoryText_Undo_Redo_Multiline_Simples_Tab_BackTab ()
		{
			var text = "First line.\nSecond line.\nThird line.";
			var tv = new TextView () { Width = 80, Height = 5, Text = text };

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ())));
			Assert.Equal ($"\tFirst line.{Environment.NewLine}Second line.{Environment.NewLine}Third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.BackTab, new KeyModifiers ())));
			Assert.Equal ($"First line.{Environment.NewLine}Second line.{Environment.NewLine}Third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"\tFirst line.{Environment.NewLine}Second line.{Environment.NewLine}Third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"First line.{Environment.NewLine}Second line.{Environment.NewLine}Third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			Assert.False (tv.IsDirty);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"\tFirst line.{Environment.NewLine}Second line.{Environment.NewLine}Third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"First line.{Environment.NewLine}Second line.{Environment.NewLine}Third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);
		}

		[Fact]
		public void HistoryText_Undo_Redo_Multiline_Selected_Tab_BackTab ()
		{
			var text = "First line.\nSecond line.\nThird line.";
			var tv = new TextView () { Width = 80, Height = 5, Text = text };

			tv.SelectionStartColumn = 6;
			tv.CursorPosition = new Point (6, 2);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ())));
			Assert.Equal ("First \tline.", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (7, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.BackTab, new KeyModifiers ())));
			Assert.Equal ("First line.", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (6, 0), tv.CursorPosition);

			// Undo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("First \tline.", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (7, 0), tv.CursorPosition);
			Assert.True (tv.IsDirty);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ($"First line.{Environment.NewLine}Second line.{Environment.NewLine}Third line.", tv.Text);
			Assert.Equal (3, tv.Lines);
			Assert.Equal (new Point (6, 2), tv.CursorPosition);
			Assert.False (tv.IsDirty);

			// Redo
			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("First \tline.", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (7, 0), tv.CursorPosition);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal ("First line.", tv.Text);
			Assert.Equal (1, tv.Lines);
			Assert.Equal (new Point (6, 0), tv.CursorPosition);
		}

		[Fact]
		public void HistoryText_ClearHistoryChanges ()
		{
			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView () { Text = text };

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			Assert.True (tv.IsDirty);
			Assert.True (tv.HasHistoryChanges);

			tv.ClearHistoryChanges ();
			Assert.Equal ($"{Environment.NewLine}This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			Assert.False (tv.IsDirty);
			Assert.False (tv.HasHistoryChanges);
		}

		[Fact]
		public void GetRegion_StringFromRunes_Environment_NewLine ()
		{
			var tv = new TextView () { Text = $"1{Environment.NewLine}2" };

			Assert.Equal ($"1{Environment.NewLine}2", tv.Text);
			Assert.Equal ("", tv.SelectedText);

			tv.SelectAll ();
			Assert.Equal ($"1{Environment.NewLine}2", tv.Text);
			Assert.Equal ($"1{Environment.NewLine}2", tv.SelectedText);
		}

		[Fact, AutoInitShutdown]
		public void WordWrap_Not_Throw_If_Width_Is_Less_Than_Zero ()
		{
			var exception = Record.Exception (() => {
				var tv = new TextView () {
					Width = Dim.Fill (),
					Height = Dim.Fill (),
					WordWrap = true,
					Text = "これは、左右のクリップ境界をテストするための非常に長いテキストです。"
				};
			});
			Assert.Null (exception);
		}

		[Fact]
		[AutoInitShutdown]
		public void ScrollDownTillCaretOffscreen_ThenType ()
		{
			var tv = new TextView {
				Width = 10,
				Height = 5
			};

			// add 100 lines of wide text to view
			for (int i = 0; i < 100; i++)
				tv.Text += new string ('x', 100) + Environment.NewLine;

			Assert.Equal (0, tv.CursorPosition.Y);
			tv.ScrollTo (50);
			Assert.Equal (0, tv.CursorPosition.Y);

			tv.ProcessKey (new KeyEvent (Key.p, new KeyModifiers ()));
		}

		[Fact]
		[AutoInitShutdown]
		public void MoveDown_By_Setting_CursorPosition ()
		{
			var tv = new TextView {
				Width = 10,
				Height = 5
			};

			// add 100 lines of wide text to view
			for (int i = 0; i < 100; i++)
				tv.Text += new string ('x', 100) + (i == 99 ? "" : Environment.NewLine);

			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			tv.CursorPosition = new Point (5, 50);
			Assert.Equal (new Point (5, 50), tv.CursorPosition);

			tv.CursorPosition = new Point (200, 200);
			Assert.Equal (new Point (100, 99), tv.CursorPosition);
		}

		[Fact]
		[AutoInitShutdown]
		public void ScrollTo_CursorPosition ()
		{
			var tv = new TextView {
				Width = 10,
				Height = 5
			};

			// add 100 lines of wide text to view
			for (int i = 0; i < 100; i++)
				tv.Text += new string ('x', 100) + (i == 99 ? "" : Environment.NewLine);

			Assert.Equal (new Point (0, 0), tv.CursorPosition);
			tv.ScrollTo (50);
			Assert.Equal (new Point (0, 0), tv.CursorPosition);

			tv.CursorPosition = new Point (tv.LeftColumn, tv.TopRow);
			Assert.Equal (new Point (0, 50), tv.CursorPosition);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void Mouse_Button_Shift_Preserves_Selection ()
		{
			Assert.Equal ("TAB to jump between text fields.", _textView.Text);
			Assert.True (_textView.MouseEvent (new MouseEvent () { X = 12, Y = 0, Flags = MouseFlags.Button1Pressed | MouseFlags.ButtonShift }));
			Assert.Equal (0, _textView.SelectionStartColumn);
			Assert.Equal (0, _textView.SelectionStartRow);
			Assert.Equal (new Point (12, 0), _textView.CursorPosition);
			Assert.True (_textView.Selecting);
			Assert.Equal ("TAB to jump ", _textView.SelectedText);

			Assert.True (_textView.MouseEvent (new MouseEvent () { X = 12, Y = 0, Flags = MouseFlags.Button1Clicked }));
			Assert.Equal (0, _textView.SelectionStartRow);
			Assert.Equal (0, _textView.SelectionStartRow);
			Assert.Equal (new Point (12, 0), _textView.CursorPosition);
			Assert.True (_textView.Selecting);
			Assert.Equal ("TAB to jump ", _textView.SelectedText);

			Assert.True (_textView.MouseEvent (new MouseEvent () { X = 19, Y = 0, Flags = MouseFlags.Button1Pressed | MouseFlags.ButtonShift }));
			Assert.Equal (0, _textView.SelectionStartRow);
			Assert.Equal (0, _textView.SelectionStartRow);
			Assert.Equal (new Point (19, 0), _textView.CursorPosition);
			Assert.True (_textView.Selecting);
			Assert.Equal ("TAB to jump between", _textView.SelectedText);

			Assert.True (_textView.MouseEvent (new MouseEvent () { X = 19, Y = 0, Flags = MouseFlags.Button1Clicked }));
			Assert.Equal (0, _textView.SelectionStartRow);
			Assert.Equal (0, _textView.SelectionStartRow);
			Assert.Equal (new Point (19, 0), _textView.CursorPosition);
			Assert.True (_textView.Selecting);
			Assert.Equal ("TAB to jump between", _textView.SelectedText);

			Assert.True (_textView.MouseEvent (new MouseEvent () { X = 24, Y = 0, Flags = MouseFlags.Button1Pressed | MouseFlags.ButtonShift }));
			Assert.Equal (0, _textView.SelectionStartRow);
			Assert.Equal (0, _textView.SelectionStartRow);
			Assert.Equal (new Point (24, 0), _textView.CursorPosition);
			Assert.True (_textView.Selecting);
			Assert.Equal ("TAB to jump between text", _textView.SelectedText);

			Assert.True (_textView.MouseEvent (new MouseEvent () { X = 24, Y = 0, Flags = MouseFlags.Button1Clicked }));
			Assert.Equal (0, _textView.SelectionStartRow);
			Assert.Equal (0, _textView.SelectionStartRow);
			Assert.Equal (new Point (24, 0), _textView.CursorPosition);
			Assert.True (_textView.Selecting);
			Assert.Equal ("TAB to jump between text", _textView.SelectedText);

			Assert.True (_textView.MouseEvent (new MouseEvent () { X = 24, Y = 0, Flags = MouseFlags.Button1Pressed }));
			Assert.Equal (0, _textView.SelectionStartRow);
			Assert.Equal (0, _textView.SelectionStartRow);
			Assert.Equal (new Point (24, 0), _textView.CursorPosition);
			Assert.True (_textView.Selecting);
			Assert.Equal ("", _textView.SelectedText);
		}

		[Fact, AutoInitShutdown]
		public void UnwrappedCursorPosition_Event ()
		{
			var cp = Point.Empty;
			var tv = new TextView () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Text = "This is the first line.\nThis is the second line.\n"
			};
			tv.UnwrappedCursorPosition += (s, e) => {
				cp = e.Point;
			};
			Application.Top.Add (tv);
			Application.Begin (Application.Top);

			Assert.False (tv.WordWrap);
			Assert.Equal (Point.Empty, tv.CursorPosition);
			Assert.Equal (Point.Empty, cp);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This is the first line. 
This is the second line.
", output);

			tv.WordWrap = true;
			tv.CursorPosition = new Point (12, 0);
			tv.Redraw (tv.Bounds);
			Assert.Equal (new Point (12, 0), tv.CursorPosition);
			Assert.Equal (new Point (12, 0), cp);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This is the first line. 
This is the second line.
", output);

			((FakeDriver)Application.Driver).SetBufferSize (6, 25);
			tv.Redraw (tv.Bounds);
			Assert.Equal (new Point (4, 2), tv.CursorPosition);
			Assert.Equal (new Point (12, 0), cp);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This 
is   
the  
first
     
line.
This 
is   
the  
secon
d    
line.
", output);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			tv.Redraw (tv.Bounds);
			Assert.Equal (new Point (0, 3), tv.CursorPosition);
			Assert.Equal (new Point (12, 0), cp);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This 
is   
the  
first
     
line.
This 
is   
the  
secon
d    
line.
", output);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			tv.Redraw (tv.Bounds);
			Assert.Equal (new Point (1, 3), tv.CursorPosition);
			Assert.Equal (new Point (13, 0), cp);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This 
is   
the  
first
     
line.
This 
is   
the  
secon
d    
line.
", output);

			Assert.True (tv.MouseEvent (new MouseEvent () { X = 0, Y = 3, Flags = MouseFlags.Button1Pressed }));
			tv.Redraw (tv.Bounds);
			Assert.Equal (new Point (0, 3), tv.CursorPosition);
			Assert.Equal (new Point (12, 0), cp);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This 
is   
the  
first
     
line.
This 
is   
the  
secon
d    
line.
", output);
		}

		[Fact]
		[AutoInitShutdown]
		public void DeleteTextBackwards_WordWrap_False_Return_Undo ()
		{
			const string text = "This is the first line.\nThis is the second line.\n";
			var tv = new TextView () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Text = text
			};
			var envText = tv.Text;
			Application.Top.Add (tv);
			Application.Begin (Application.Top);

			Assert.False (tv.WordWrap);
			Assert.Equal (Point.Empty, tv.CursorPosition);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This is the first line. 
This is the second line.
", output);

			tv.CursorPosition = new Point (3, 0);
			Assert.Equal (new Point (3, 0), tv.CursorPosition);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ())));
			tv.Redraw (tv.Bounds);
			Assert.Equal (new Point (2, 0), tv.CursorPosition);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
Ths is the first line.  
This is the second line.
", output);

			tv.CursorPosition = new Point (0, 1);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ())));
			tv.Redraw (tv.Bounds);
			Assert.Equal (new Point (22, 0), tv.CursorPosition);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
Ths is the first line.This is the second line.
", output);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			tv.Redraw (tv.Bounds);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
Ths is the first line.  
This is the second line.
", output);

			while (tv.Text != envText) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal (envText, tv.Text);
			Assert.Equal (new Point (3, 0), tv.CursorPosition);
			Assert.False (tv.IsDirty);
		}

		[Fact]
		[AutoInitShutdown]
		public void DeleteTextBackwards_WordWrap_True_Return_Undo ()
		{
			const string text = "This is the first line.\nThis is the second line.\n";
			var tv = new TextView () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Text = text,
				WordWrap = true
			};
			var envText = tv.Text;
			Application.Top.Add (tv);
			Application.Begin (Application.Top);

			Assert.True (tv.WordWrap);
			Assert.Equal (Point.Empty, tv.CursorPosition);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This is the first line. 
This is the second line.
", output);

			tv.CursorPosition = new Point (3, 0);
			Assert.Equal (new Point (3, 0), tv.CursorPosition);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ())));
			tv.Redraw (tv.Bounds);
			Assert.Equal (new Point (2, 0), tv.CursorPosition);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
Ths is the first line.  
This is the second line.
", output);

			tv.CursorPosition = new Point (0, 1);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ())));
			tv.Redraw (tv.Bounds);
			Assert.Equal (new Point (22, 0), tv.CursorPosition);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
Ths is the first line.This is the second line.
", output);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			tv.Redraw (tv.Bounds);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
Ths is the first line.  
This is the second line.
", output);

			while (tv.Text != envText) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal (envText, tv.Text);
			Assert.Equal (new Point (3, 0), tv.CursorPosition);
			Assert.False (tv.IsDirty);
		}

		[Fact]
		[AutoInitShutdown]
		public void DeleteTextForwards_WordWrap_False_Return_Undo ()
		{
			const string text = "This is the first line.\nThis is the second line.\n";
			var tv = new TextView () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Text = text
			};
			var envText = tv.Text;
			Application.Top.Add (tv);
			Application.Begin (Application.Top);

			Assert.False (tv.WordWrap);
			Assert.Equal (Point.Empty, tv.CursorPosition);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This is the first line. 
This is the second line.
", output);

			tv.CursorPosition = new Point (2, 0);
			Assert.Equal (new Point (2, 0), tv.CursorPosition);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.DeleteChar, new KeyModifiers ())));
			tv.Redraw (tv.Bounds);
			Assert.Equal (new Point (2, 0), tv.CursorPosition);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
Ths is the first line.  
This is the second line.
", output);

			tv.CursorPosition = new Point (22, 0);
			Assert.Equal (new Point (22, 0), tv.CursorPosition);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.DeleteChar, new KeyModifiers ())));
			tv.Redraw (tv.Bounds);
			Assert.Equal (new Point (22, 0), tv.CursorPosition);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
Ths is the first line.This is the second line.
", output);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			tv.Redraw (tv.Bounds);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
Ths is the first line.  
This is the second line.
", output);

			while (tv.Text != envText) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal (envText, tv.Text);
			Assert.Equal (new Point (2, 0), tv.CursorPosition);
			Assert.False (tv.IsDirty);
		}

		[Fact]
		[AutoInitShutdown]
		public void DeleteTextForwards_WordWrap_True_Return_Undo ()
		{
			const string text = "This is the first line.\nThis is the second line.\n";
			var tv = new TextView () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Text = text,
				WordWrap = true
			};
			var envText = tv.Text;
			Application.Top.Add (tv);
			Application.Begin (Application.Top);

			Assert.True (tv.WordWrap);
			Assert.Equal (Point.Empty, tv.CursorPosition);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This is the first line. 
This is the second line.
", output);

			tv.CursorPosition = new Point (2, 0);
			Assert.Equal (new Point (2, 0), tv.CursorPosition);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.DeleteChar, new KeyModifiers ())));
			tv.Redraw (tv.Bounds);
			Assert.Equal (new Point (2, 0), tv.CursorPosition);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
Ths is the first line.  
This is the second line.
", output);

			tv.CursorPosition = new Point (22, 0);
			Assert.Equal (new Point (22, 0), tv.CursorPosition);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.DeleteChar, new KeyModifiers ())));
			tv.Redraw (tv.Bounds);
			Assert.Equal (new Point (22, 0), tv.CursorPosition);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
Ths is the first line.This is the second line.
", output);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			tv.Redraw (tv.Bounds);
			Assert.Equal (new Point (0, 1), tv.CursorPosition);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
Ths is the first line.  
This is the second line.
", output);

			while (tv.Text != envText) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			}
			Assert.Equal (envText, tv.Text);
			Assert.Equal (new Point (2, 0), tv.CursorPosition);
			Assert.False (tv.IsDirty);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void TextView_InsertText_Newline_LF ()
		{
			var tv = new TextView {
				Width = 10,
				Height = 10,
			};
			tv.InsertText ("\naaa\nbbb");
			var p = Environment.OSVersion.Platform;
			if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows) {
				Assert.Equal ("\r\naaa\r\nbbb", tv.Text);
			} else {
				Assert.Equal ("\naaa\nbbb", tv.Text);
			}
			Assert.Equal ($"{Environment.NewLine}aaa{Environment.NewLine}bbb", tv.Text);

			var win = new Window ();
			win.Add (tv);
			Application.Top.Add (win);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (15, 15);
			Application.Refresh ();
			//this passes
			var pos = TestHelpers.AssertDriverContentsWithFrameAre (
			@"
┌─────────────┐
│             │
│aaa          │
│bbb          │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
└─────────────┘", output);

			Assert.Equal (new Rect (0, 0, 15, 15), pos);

			Assert.True (tv.Used);
			tv.Used = false;
			tv.CursorPosition = new Point (0, 0);
			tv.InsertText ("\naaa\nbbb");
			Application.Refresh ();

			TestHelpers.AssertDriverContentsWithFrameAre (
			@"
┌─────────────┐
│             │
│aaa          │
│bbb          │
│aaa          │
│bbb          │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
└─────────────┘", output);
		}

		[Fact]
		[TextViewTestsAutoInitShutdown]
		public void TextView_InsertText_Newline_CRLF ()
		{
			var tv = new TextView {
				Width = 10,
				Height = 10,
			};
			tv.InsertText ("\r\naaa\r\nbbb");
			var p = Environment.OSVersion.Platform;
			if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows) {
				Assert.Equal ("\r\naaa\r\nbbb", tv.Text);
			} else {
				Assert.Equal ("\naaa\nbbb", tv.Text);
			}
			Assert.Equal ($"{Environment.NewLine}aaa{Environment.NewLine}bbb", tv.Text);

			var win = new Window ();
			win.Add (tv);
			Application.Top.Add (win);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (15, 15);
			Application.Refresh ();

			//this passes
			var pos = TestHelpers.AssertDriverContentsWithFrameAre (
			@"
┌─────────────┐
│             │
│aaa          │
│bbb          │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
└─────────────┘", output);

			Assert.Equal (new Rect (0, 0, 15, 15), pos);

			Assert.True (tv.Used);
			tv.Used = false;
			tv.CursorPosition = new Point (0, 0);
			tv.InsertText ("\r\naaa\r\nbbb");
			Application.Refresh ();

			TestHelpers.AssertDriverContentsWithFrameAre (
			@"
┌─────────────┐
│             │
│aaa          │
│bbb          │
│aaa          │
│bbb          │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
└─────────────┘", output);
		}

		[Fact, AutoInitShutdown]
		public void ContentsChanged_Event_NoFires_On_CursorPosition ()
		{
			var eventcount = 0;

			var tv = new TextView {
				Width = 50,
				Height = 10,
			};

			tv.ContentsChanged += (s, e) => {
				eventcount++;
			};
			Assert.Equal (0, eventcount);

			tv.CursorPosition = new Point (0, 0);

			Assert.Equal (0, eventcount);
		}

		[Fact, AutoInitShutdown]
		public void ContentsChanged_Event_Fires_On_InsertText ()
		{
			var eventcount = 0;

			var tv = new TextView {
				Width = 50,
				Height = 10,
			};
			tv.CursorPosition = new Point (0, 0);

			tv.ContentsChanged += (s, e) => {
				eventcount++;
			};

			Assert.Equal (0, eventcount);

			tv.InsertText ("a");
			Assert.Equal (1, eventcount);

			tv.CursorPosition = new Point (0, 0);
			tv.InsertText ("bcd");
			Assert.Equal (4, eventcount);

			tv.InsertText ("e");
			Assert.Equal (5, eventcount);

			tv.InsertText ("\n");
			Assert.Equal (6, eventcount);

			tv.InsertText ("1234");
			Assert.Equal (10, eventcount);
		}

		[Fact, AutoInitShutdown]
		public void ContentsChanged_Event_Fires_On_Init ()
		{
			Application.Iteration += () => {
				Application.RequestStop ();
			};

			var expectedRow = 0;
			var expectedCol = 0;
			var eventcount = 0;

			var tv = new TextView {
				Width = 50,
				Height = 10,
			};
			tv.ContentsChanged += (s, e) => {
				eventcount++;
				Assert.Equal (expectedRow, e.Row);
				Assert.Equal (expectedCol, e.Col);
			};

			Application.Top.Add (tv);
			Application.Begin (Application.Top);
			Assert.Equal (1, eventcount);
		}

		[Fact, AutoInitShutdown]
		public void ContentsChanged_Event_Fires_On_Set_Text ()
		{
			Application.Iteration += () => {
				Application.RequestStop ();
			};
			var eventcount = 0;

			var expectedRow = 0;
			var expectedCol = 0;

			var tv = new TextView {
				Width = 50,
				Height = 10,
				// you'd think col would be 3, but it's 0 because TextView sets
				// row/col = 0 when you set Text
				Text = "abc",
			};
			tv.ContentsChanged += (s, e) => {
				eventcount++;
				Assert.Equal (expectedRow, e.Row);
				Assert.Equal (expectedCol, e.Col);
			};

			Assert.Equal ("abc", tv.Text);

			Application.Top.Add (tv);
			var rs = Application.Begin (Application.Top);
			Assert.Equal (1, eventcount); // for Initialize

			expectedCol = 0;
			tv.Text = "defg";
			Assert.Equal (2, eventcount); // for set Text = "defg"
		}

		[Fact, AutoInitShutdown]
		public void ContentsChanged_Event_Fires_On_Typing ()
		{
			Application.Iteration += () => {
				Application.RequestStop ();
			};
			var eventcount = 0;

			var expectedRow = 0;
			var expectedCol = 0;

			var tv = new TextView {
				Width = 50,
				Height = 10,
			};
			tv.ContentsChanged += (s, e) => {
				eventcount++;
				Assert.Equal (expectedRow, e.Row);
				Assert.Equal (expectedCol, e.Col);
			};

			Application.Top.Add (tv);
			var rs = Application.Begin (Application.Top);
			Assert.Equal (1, eventcount); // for Initialize

			expectedCol = 0;
			tv.Text = "ay";
			Assert.Equal (2, eventcount);

			expectedCol = 1;
			tv.ProcessKey (new KeyEvent (Key.Y, new KeyModifiers ()));
			Assert.Equal (3, eventcount);
			Assert.Equal ("Yay", tv.Text.ToString ());
		}

		[Fact, TextViewTestsAutoInitShutdown]
		public void ContentsChanged_Event_Fires_Using_Kill_Delete_Tests ()
		{
			var eventcount = 0;

			_textView.ContentsChanged += (s, e) => {
				eventcount++;
			};

			var expectedEventCount = 1;
			Kill_Delete_WordForward ();
			Assert.Equal (expectedEventCount, eventcount); // for Initialize

			expectedEventCount += 1;
			Kill_Delete_WordBackward ();
			Assert.Equal (expectedEventCount, eventcount);

			expectedEventCount += 1;
			Kill_To_End_Delete_Forwards_And_Copy_To_The_Clipboard ();
			Assert.Equal (expectedEventCount, eventcount);

			expectedEventCount += 1;
			Kill_To_Start_Delete_Backwards_And_Copy_To_The_Clipboard ();
			Assert.Equal (expectedEventCount, eventcount);
		}


		[Fact, TextViewTestsAutoInitShutdown]
		public void ContentsChanged_Event_Fires_Using_Copy_Or_Cut_Tests ()
		{
			var eventcount = 0;

			_textView.ContentsChanged += (s, e) => {
				eventcount++;
			};

			var expectedEventCount = 1;

			// reset
			_textView.Text = TextViewTestsAutoInitShutdown.txt;
			Assert.Equal (expectedEventCount, eventcount);

			expectedEventCount += 3;
			Copy_Or_Cut_And_Paste_With_No_Selection ();
			Assert.Equal (expectedEventCount, eventcount);

			// reset
			expectedEventCount += 1;
			_textView.Text = TextViewTestsAutoInitShutdown.txt;
			Assert.Equal (expectedEventCount, eventcount);

			expectedEventCount += 3;
			Copy_Or_Cut_And_Paste_With_Selection ();
			Assert.Equal (expectedEventCount, eventcount);

			// reset
			expectedEventCount += 1;
			_textView.Text = TextViewTestsAutoInitShutdown.txt;
			Assert.Equal (expectedEventCount, eventcount);

			expectedEventCount += 1;
			Copy_Or_Cut_Not_Null_If_Has_Selection ();
			Assert.Equal (expectedEventCount, eventcount);

			// reset
			expectedEventCount += 1;
			_textView.Text = TextViewTestsAutoInitShutdown.txt;
			Assert.Equal (expectedEventCount, eventcount);

			expectedEventCount += 1;
			Copy_Or_Cut_Null_If_No_Selection ();
			Assert.Equal (expectedEventCount, eventcount);

			// reset
			expectedEventCount += 1;
			_textView.Text = TextViewTestsAutoInitShutdown.txt;
			Assert.Equal (expectedEventCount, eventcount);

			expectedEventCount += 4;
			Copy_Without_Selection ();
			Assert.Equal (expectedEventCount, eventcount);

			// reset
			expectedEventCount += 1;
			_textView.Text = TextViewTestsAutoInitShutdown.txt;
			Assert.Equal (expectedEventCount, eventcount);

			expectedEventCount += 4;
			Copy_Without_Selection ();
			Assert.Equal (expectedEventCount, eventcount);
		}

		[Fact, TextViewTestsAutoInitShutdown]
		public void ContentsChanged_Event_Fires_On_Undo_Redo ()
		{
			var eventcount = 0;
			var expectedEventCount = 0;

			_textView.ContentsChanged += (s, e) => {
				eventcount++;
			};

			expectedEventCount++;
			_textView.Text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			Assert.Equal (expectedEventCount, eventcount);

			expectedEventCount++;
			Assert.True (_textView.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal (expectedEventCount, eventcount);

			// Undo
			expectedEventCount++;
			Assert.True (_textView.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (expectedEventCount, eventcount);

			// Redo
			expectedEventCount++;
			Assert.True (_textView.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (expectedEventCount, eventcount);

			// Undo
			expectedEventCount++;
			Assert.True (_textView.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (expectedEventCount, eventcount);

			// Redo
			expectedEventCount++;
			Assert.True (_textView.ProcessKey (new KeyEvent (Key.R | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (expectedEventCount, eventcount);
		}

		[Fact]
		public void ContentsChanged_Event_Fires_ClearHistoryChanges ()
		{
			var eventcount = 0;

			var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
			var tv = new TextView {
				Width = 50,
				Height = 10,
				Text = text,
			};
			tv.ContentsChanged += (s, e) => {
				eventcount++;
			};

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ($"{Environment.NewLine}This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
			Assert.Equal (4, tv.Lines);

			var expectedEventCount = 1; // for ENTER key
			Assert.Equal (expectedEventCount, eventcount);

			tv.ClearHistoryChanges ();
			expectedEventCount = 2;
			Assert.Equal (expectedEventCount, eventcount);
		}

		[Fact]
		public void ContentsChanged_Event_Fires_LoadStream_By_Calling_HistoryText_Clear ()
		{
			var eventcount = 0;

			var tv = new TextView {
				Width = 50,
				Height = 10,
			};
			tv.ContentsChanged += (s, e) => {
				eventcount++;
			};

			var text = "This is the first line.\r\nThis is the second line.\r\n";
			tv.LoadStream (new System.IO.MemoryStream (System.Text.Encoding.ASCII.GetBytes (text)));
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);

			Assert.Equal (1, eventcount);
		}

		[Fact]
		public void ContentsChanged_Event_Fires_On_LoadFile_By_Calling_HistoryText_Clear ()
		{
			var eventcount = 0;

			var tv = new TextView {
				Width = 50,
				Height = 10,
			};
			tv.ContentsChanged += (s, e) => {
				eventcount++;
			};

			var fileName = "textview.txt";
			System.IO.File.WriteAllText (fileName, "This is the first line.\r\nThis is the second line.\r\n");

			tv.LoadFile (fileName);
			Assert.Equal (1, eventcount);
			Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);
		}

		[Fact]
		public void ReplaceAllText_Does_Not_Throw_Exception ()
		{
			var textToFind = "hello! hello!";
			var textToReplace = "hello!";
			var tv = new TextView () { Width = 20, Height = 3, Text = textToFind };

			var exception = Record.Exception (() => tv.ReplaceAllText (textToFind, false, false, textToReplace));
			Assert.Null (exception);
			Assert.Equal (textToReplace, tv.Text);
		}
	}
}