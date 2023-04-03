using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Terminal.Gui.ViewTests {
	public class TimeFieldTests {
		[Fact]
		public void Constructors_Defaults ()
		{
			var tf = new TimeField ();
			Assert.False (tf.IsShortFormat);
			Assert.Equal (TimeSpan.MinValue, tf.Time);
			Assert.Equal (1, tf.CursorPosition);
			Assert.Equal (new Rect (0, 0, 10, 1), tf.Frame);

			var time = DateTime.Now.TimeOfDay;
			tf = new TimeField (time);
			Assert.False (tf.IsShortFormat);
			Assert.Equal (time, tf.Time);
			Assert.Equal (1, tf.CursorPosition);
			Assert.Equal (new Rect (0, 0, 10, 1), tf.Frame);

			tf = new TimeField (1, 2, time);
			Assert.False (tf.IsShortFormat);
			Assert.Equal (time, tf.Time);
			Assert.Equal (1, tf.CursorPosition);
			Assert.Equal (new Rect (1, 2, 10, 1), tf.Frame);

			tf = new TimeField (3, 4, time, true);
			Assert.True (tf.IsShortFormat);
			Assert.Equal (time, tf.Time);
			Assert.Equal (1, tf.CursorPosition);
			Assert.Equal (new Rect (3, 4, 7, 1), tf.Frame);

			tf.IsShortFormat = false;
			Assert.Equal (new Rect (3, 4, 10, 1), tf.Frame);
			Assert.Equal (10, tf.Width);
		}

		[Fact]
		public void CursorPosition_Min_Is_Always_One_Max_Is_Always_Max_Format ()
		{
			var tf = new TimeField ();
			Assert.Equal (1, tf.CursorPosition);
			tf.CursorPosition = 0;
			Assert.Equal (1, tf.CursorPosition);
			tf.CursorPosition = 9;
			Assert.Equal (8, tf.CursorPosition);
			tf.IsShortFormat = true;
			tf.CursorPosition = 0;
			Assert.Equal (1, tf.CursorPosition);
			tf.CursorPosition = 6;
			Assert.Equal (5, tf.CursorPosition);
		}

		[Fact]
		public void KeyBindings_Command ()
		{
			TimeField tf = new TimeField (TimeSpan.Parse ("12:12:19"));
			tf.ReadOnly = true;
			Assert.True (tf.ProcessKey (new KeyEvent (Key.DeleteChar, new KeyModifiers ())));
			Assert.Equal (" 12:12:19", tf.Text);
			tf.ReadOnly = false;
			Assert.True (tf.ProcessKey (new KeyEvent (Key.D | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (" 02:12:19", tf.Text);
			tf.CursorPosition = 4;
			tf.ReadOnly = true;
			Assert.True (tf.ProcessKey (new KeyEvent (Key.Delete, new KeyModifiers ())));
			Assert.Equal (" 02:12:19", tf.Text);
			tf.ReadOnly = false;
			Assert.True (tf.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ())));
			Assert.Equal (" 02:02:19", tf.Text);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));
			Assert.Equal (1, tf.CursorPosition);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.Equal (8, tf.CursorPosition);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.A | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (1, tf.CursorPosition);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.E | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (8, tf.CursorPosition);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.Equal (7, tf.CursorPosition);
			Assert.True (tf.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (8, tf.CursorPosition);
			Assert.False (tf.ProcessKey (new KeyEvent (Key.A, new KeyModifiers ())));
			tf.ReadOnly = true;
			tf.CursorPosition = 1;
			Assert.True (tf.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers ())));
			Assert.Equal (" 02:02:19", tf.Text);
			tf.ReadOnly = false;
			Assert.True (tf.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers ())));
			Assert.Equal (" 12:02:19", tf.Text);
		}
	}
}
