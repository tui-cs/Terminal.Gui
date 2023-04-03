using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Terminal.Gui.ViewTests {
	public class DateFieldTests {
		[Fact]
		public void Constructors_Defaults ()
		{
			var df = new DateField ();
			Assert.False (df.IsShortFormat);
			Assert.Equal (DateTime.MinValue, df.Date);
			Assert.Equal (1, df.CursorPosition);
			Assert.Equal (new Rect (0, 0, 12, 1), df.Frame);

			var date = DateTime.Now;
			df = new DateField (date);
			Assert.False (df.IsShortFormat);
			Assert.Equal (date, df.Date);
			Assert.Equal (1, df.CursorPosition);
			Assert.Equal (new Rect (0, 0, 12, 1), df.Frame);

			df = new DateField (1, 2, date);
			Assert.False (df.IsShortFormat);
			Assert.Equal (date, df.Date);
			Assert.Equal (1, df.CursorPosition);
			Assert.Equal (new Rect (1, 2, 12, 1), df.Frame);

			df = new DateField (3, 4, date, true);
			Assert.True (df.IsShortFormat);
			Assert.Equal (date, df.Date);
			Assert.Equal (1, df.CursorPosition);
			Assert.Equal (new Rect (3, 4, 10, 1), df.Frame);

			df.IsShortFormat = false;
			Assert.Equal (new Rect (3, 4, 12, 1), df.Frame);
			Assert.Equal (12, df.Width);
		}

		[Fact]
		public void CursorPosition_Min_Is_Always_One_Max_Is_Always_Max_Format ()
		{
			var df = new DateField ();
			Assert.Equal (1, df.CursorPosition);
			df.CursorPosition = 0;
			Assert.Equal (1, df.CursorPosition);
			df.CursorPosition = 11;
			Assert.Equal (10, df.CursorPosition);
			df.IsShortFormat = true;
			df.CursorPosition = 0;
			Assert.Equal (1, df.CursorPosition);
			df.CursorPosition = 9;
			Assert.Equal (8, df.CursorPosition);
		}

		[Fact]
		public void KeyBindings_Command ()
		{
			DateField df = new DateField (DateTime.Parse ("12/12/1971"));
			df.ReadOnly = true;
			Assert.True (df.ProcessKey (new KeyEvent (Key.DeleteChar, new KeyModifiers ())));
			Assert.Equal (" 12/12/1971", df.Text);
			df.ReadOnly = false;
			Assert.True (df.ProcessKey (new KeyEvent (Key.D | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (" 02/12/1971", df.Text);
			df.CursorPosition = 4;
			df.ReadOnly = true;
			Assert.True (df.ProcessKey (new KeyEvent (Key.Delete, new KeyModifiers ())));
			Assert.Equal (" 02/12/1971", df.Text);
			df.ReadOnly = false;
			Assert.True (df.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ())));
			Assert.Equal (" 02/02/1971", df.Text);
			Assert.True (df.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));
			Assert.Equal (1, df.CursorPosition);
			Assert.True (df.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.Equal (10, df.CursorPosition);
			Assert.True (df.ProcessKey (new KeyEvent (Key.A | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (1, df.CursorPosition);
			Assert.True (df.ProcessKey (new KeyEvent (Key.E | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (10, df.CursorPosition);
			Assert.True (df.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.Equal (9, df.CursorPosition);
			Assert.True (df.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (10, df.CursorPosition);
			Assert.False (df.ProcessKey (new KeyEvent (Key.A, new KeyModifiers ())));
			df.ReadOnly = true;
			df.CursorPosition = 1;
			Assert.True (df.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers ())));
			Assert.Equal (" 02/02/1971", df.Text);
			df.ReadOnly = false;
			Assert.True (df.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers ())));
			Assert.Equal (" 12/02/1971", df.Text);
		}
	}
}
