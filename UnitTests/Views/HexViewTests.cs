using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Terminal.Gui.ViewsTests {
	public class HexViewTests {
		[Fact]
		public void Constructors_Defaults ()
		{
			var hv = new HexView ();
			Assert.NotNull (hv.Source);
			Assert.IsAssignableFrom<System.IO.MemoryStream> (hv.Source);
			Assert.True (hv.CanFocus);
			Assert.True (hv.AllowEdits);

			hv = new HexView (new System.IO.MemoryStream ());
			Assert.NotNull (hv.Source);
			Assert.IsAssignableFrom<System.IO.Stream> (hv.Source);
			Assert.True (hv.CanFocus);
			Assert.True (hv.AllowEdits);
		}

		private Stream LoadStream (bool unicode = false)
		{
			MemoryStream stream = new MemoryStream ();
			byte [] bArray;
			string memString = "Hello world.\nThis is a test of the Emergency Broadcast System.\n";

			Assert.Equal (63, memString.Length);

			if (unicode) {
				bArray = Encoding.Unicode.GetBytes (memString);
				Assert.Equal (126, bArray.Length);
			} else {
				bArray = Encoding.Default.GetBytes (memString);
				Assert.Equal (63, bArray.Length);
			}
			stream.Write (bArray);

			return stream;
		}

		[Fact]
		public void AllowEdits_Edits_ApplyEdits ()
		{
			var hv = new HexView (LoadStream (true)) {
				Width = 20,
				Height = 20
			};

			Assert.Empty (hv.Edits);
			hv.AllowEdits = false;
			Assert.True (hv.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));
			Assert.False (hv.ProcessKey (new KeyEvent (Key.A, new KeyModifiers ())));
			Assert.Empty (hv.Edits);
			Assert.Equal (126, hv.Source.Length);

			hv.AllowEdits = true;
			Assert.True (hv.ProcessKey (new KeyEvent (Key.D4, new KeyModifiers ())));
			Assert.True (hv.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers ())));
			Assert.Single (hv.Edits);
			Assert.Equal (65, hv.Edits.ToList () [0].Value);
			Assert.Equal ('A', (char)hv.Edits.ToList () [0].Value);
			Assert.Equal (126, hv.Source.Length);

			// Appends byte
			Assert.True (hv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.True (hv.ProcessKey (new KeyEvent (Key.D4, new KeyModifiers ())));
			Assert.True (hv.ProcessKey (new KeyEvent (Key.D2, new KeyModifiers ())));
			Assert.Equal (2, hv.Edits.Count);
			Assert.Equal (66, hv.Edits.ToList () [1].Value);
			Assert.Equal ('B', (char)hv.Edits.ToList () [1].Value);
			Assert.Equal (126, hv.Source.Length);

			hv.ApplyEdits ();
			Assert.Empty (hv.Edits);
			Assert.Equal (127, hv.Source.Length);
		}

		[Fact]
		public void DisplayStart_Source ()
		{
			var hv = new HexView (LoadStream (true)) {
				Width = 20,
				Height = 20
			};

			Assert.Equal (0, hv.DisplayStart);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.PageDown, new KeyModifiers ())));
			Assert.Equal (4 * hv.Frame.Height, hv.DisplayStart);
			Assert.Equal (hv.Source.Length, hv.Source.Position);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			// already on last page and so the DisplayStart is the same as before
			Assert.Equal (4 * hv.Frame.Height, hv.DisplayStart);
			Assert.Equal (hv.Source.Length, hv.Source.Position);
		}

		[Fact]
		public void Edited_Event ()
		{
			var hv = new HexView (LoadStream (true)) { Width = 20, Height = 20 };
			KeyValuePair<long, byte> keyValuePair = default;
			hv.Edited += (s,e) => keyValuePair = new KeyValuePair<long, byte>(e.Position,e.NewValue);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.D4, new KeyModifiers ())));
			Assert.True (hv.ProcessKey (new KeyEvent (Key.D6, new KeyModifiers ())));

			Assert.Equal (0, (int)keyValuePair.Key);
			Assert.Equal (70, (int)keyValuePair.Value);
			Assert.Equal ('F', (char)keyValuePair.Value);
		}

		[Fact]
		public void DiscardEdits_Method ()
		{
			var hv = new HexView (LoadStream (true)) { Width = 20, Height = 20 };
			Assert.True (hv.ProcessKey (new KeyEvent (Key.D4, new KeyModifiers ())));
			Assert.True (hv.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers ())));
			Assert.Single (hv.Edits);
			Assert.Equal (65, hv.Edits.ToList () [0].Value);
			Assert.Equal ('A', (char)hv.Edits.ToList () [0].Value);
			Assert.Equal (126, hv.Source.Length);

			hv.DiscardEdits ();
			Assert.Empty (hv.Edits);
		}

		[Fact]
		public void Position_Using_Encoding_Unicode ()
		{
			var hv = new HexView (LoadStream (true)) { Width = 20, Height = 20 };
			Assert.Equal (126, hv.Source.Length);
			Assert.Equal (126, hv.Source.Position);
			Assert.Equal (1, hv.Position);

			// left side needed to press twice
			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (126, hv.Source.Position);
			Assert.Equal (1, hv.Position);
			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (126, hv.Source.Position);
			Assert.Equal (2, hv.Position);

			// right side only needed to press one time
			Assert.True (hv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal (126, hv.Source.Position);
			Assert.Equal (2, hv.Position);
			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.Equal (126, hv.Source.Position);
			Assert.Equal (1, hv.Position);

			// last position is equal to the source length
			Assert.True (hv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.Equal (126, hv.Source.Position);
			Assert.Equal (127, hv.Position);
			Assert.Equal (hv.Position - 1, hv.Source.Length);
		}

		[Fact]
		public void Position_Using_Encoding_Default ()
		{
			var hv = new HexView (LoadStream ()) { Width = 20, Height = 20 };
			Assert.Equal (63, hv.Source.Length);
			Assert.Equal (63, hv.Source.Position);
			Assert.Equal (1, hv.Position);

			// left side needed to press twice
			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (63, hv.Source.Position);
			Assert.Equal (1, hv.Position);
			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (63, hv.Source.Position);
			Assert.Equal (2, hv.Position);

			// right side only needed to press one time
			Assert.True (hv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal (63, hv.Source.Position);
			Assert.Equal (2, hv.Position);
			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.Equal (63, hv.Source.Position);
			Assert.Equal (1, hv.Position);

			// last position is equal to the source length
			Assert.True (hv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.Equal (63, hv.Source.Position);
			Assert.Equal (64, hv.Position);
			Assert.Equal (hv.Position - 1, hv.Source.Length);
		}

		[Fact]
		[AutoInitShutdown]
		public void CursorPosition_Encoding_Unicode ()
		{
			var hv = new HexView (LoadStream (true)) { Width = Dim.Fill (), Height = Dim.Fill () };
			Application.Top.Add (hv);
			Application.Begin (Application.Top);

			Assert.Equal (new Point (1, 1), hv.CursorPosition);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorRight | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (hv.CursorPosition.X, hv.BytesPerLine);
			Assert.True (hv.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));

			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (new Point (2, 1), hv.CursorPosition);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal (new Point (2, 2), hv.CursorPosition);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			var col = hv.CursorPosition.X;
			var line = hv.CursorPosition.Y;
			var offset = (line - 1) * (hv.BytesPerLine - col);
			Assert.Equal (hv.Position, col * line + offset);
		}

		[Fact]
		[AutoInitShutdown]
		public void CursorPosition_Encoding_Default ()
		{
			var hv = new HexView (LoadStream ()) { Width = Dim.Fill (), Height = Dim.Fill () };
			Application.Top.Add (hv);
			Application.Begin (Application.Top);

			Assert.Equal (new Point (1, 1), hv.CursorPosition);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorRight | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (hv.CursorPosition.X, hv.BytesPerLine);
			Assert.True (hv.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));

			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (new Point (2, 1), hv.CursorPosition);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal (new Point (2, 2), hv.CursorPosition);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			var col = hv.CursorPosition.X;
			var line = hv.CursorPosition.Y;
			var offset = (line - 1) * (hv.BytesPerLine - col);
			Assert.Equal (hv.Position, col * line + offset);
		}

		[Fact]
		[AutoInitShutdown]
		public void PositionChanged_Event ()
		{
			var hv = new HexView (LoadStream ()) { Width = Dim.Fill (), Height = Dim.Fill () };
			HexViewEventArgs hexViewEventArgs = null;
			hv.PositionChanged += (s, e) => hexViewEventArgs = e;
			Application.Top.Add (hv);
			Application.Begin (Application.Top);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()))); // left side must press twice
			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));

			Assert.Equal (12, hexViewEventArgs.BytesPerLine);
			Assert.Equal (new Point (2, 2), hexViewEventArgs.CursorPosition);
			Assert.Equal (14, hexViewEventArgs.Position);
		}

		private class NonSeekableStream : Stream {
			Stream m_stream;
			public NonSeekableStream (Stream baseStream)
			{
				m_stream = baseStream;
			}
			public override bool CanRead {
				get { return m_stream.CanRead; }
			}

			public override bool CanSeek {
				get { return false; }
			}

			public override bool CanWrite {
				get { return m_stream.CanWrite; }
			}

			public override void Flush ()
			{
				m_stream.Flush ();
			}

			public override long Length {
				get { throw new NotSupportedException (); }
			}

			public override long Position {
				get {
					return m_stream.Position;
				}
				set {
					throw new NotSupportedException ();
				}
			}

			public override int Read (byte [] buffer, int offset, int count)
			{
				return m_stream.Read (buffer, offset, count);
			}

			public override long Seek (long offset, SeekOrigin origin)
			{
				throw new NotImplementedException ();
			}

			public override void SetLength (long value)
			{
				throw new NotSupportedException ();
			}

			public override void Write (byte [] buffer, int offset, int count)
			{
				m_stream.Write (buffer, offset, count);
			}
		}

		[Fact]
		public void Exceptions_Tests ()
		{
			Assert.Throws<ArgumentNullException> (() => new HexView (null));
			Assert.Throws<ArgumentException> (() => new HexView (new NonSeekableStream (new MemoryStream ())));
		}

		[Fact]
		[AutoInitShutdown]
		public void Source_Sets_DisplayStart_And_Position_To_Zero_If_Greater_Than_Source_Length ()
		{
			var hv = new HexView (LoadStream ()) { Width = 10, Height = 5 };
			Application.Top.Add (hv);
			Application.Begin (Application.Top);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.Equal (62, hv.DisplayStart);
			Assert.Equal (64, hv.Position);

			hv.Source = new MemoryStream ();
			Assert.Equal (0, hv.DisplayStart);
			Assert.Equal (0, hv.Position - 1);

			hv.Source = LoadStream ();
			hv.Width = Dim.Fill ();
			hv.Height = Dim.Fill ();
			Application.Top.LayoutSubviews ();
			Assert.Equal (0, hv.DisplayStart);
			Assert.Equal (0, hv.Position - 1);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.Equal (0, hv.DisplayStart);
			Assert.Equal (64, hv.Position);

			hv.Source = new MemoryStream ();
			Assert.Equal (0, hv.DisplayStart);
			Assert.Equal (0, hv.Position - 1);
		}

		[Fact]
		public void ApplyEdits_With_Argument ()
		{
			byte [] buffer = Encoding.Default.GetBytes ("Fest");
			var original = new MemoryStream ();
			original.Write (buffer, 0, buffer.Length);
			original.Flush ();
			var copy = new MemoryStream ();
			original.Position = 0;
			original.CopyTo (copy);
			copy.Flush ();
			var hv = new HexView (copy) { Width = Dim.Fill (), Height = Dim.Fill () };
			byte [] readBuffer = new byte [hv.Source.Length];
			hv.Source.Position = 0;
			hv.Source.Read (readBuffer);
			Assert.Equal ("Fest", Encoding.Default.GetString (readBuffer));

			Assert.True (hv.ProcessKey (new KeyEvent (Key.D5, new KeyModifiers ())));
			Assert.True (hv.ProcessKey (new KeyEvent (Key.D4, new KeyModifiers ())));
			readBuffer [hv.Edits.ToList () [0].Key] = hv.Edits.ToList () [0].Value;
			Assert.Equal ("Test", Encoding.Default.GetString (readBuffer));

			hv.ApplyEdits (original);
			original.Position = 0;
			original.Read (buffer);
			copy.Position = 0;
			copy.Read (readBuffer);
			Assert.Equal ("Test", Encoding.Default.GetString (buffer));
			Assert.Equal ("Test", Encoding.Default.GetString (readBuffer));
			Assert.Equal (Encoding.Default.GetString (buffer), Encoding.Default.GetString (readBuffer));
		}

		[Fact]
		[AutoInitShutdown]
		public void KeyBindings_Command ()
		{
			var hv = new HexView (LoadStream ()) { Width = 20, Height = 10 };
			Application.Top.Add (hv);
			Application.Begin (Application.Top);

			Assert.Equal (63, hv.Source.Length);
			Assert.Equal (1, hv.Position);
			Assert.Equal (4, hv.BytesPerLine);

			// right side only needed to press one time
			Assert.True (hv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));

			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (2, hv.Position);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.Equal (1, hv.Position);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal (5, hv.Position);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.Equal (1, hv.Position);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.V | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (41, hv.Position);

			Assert.True (hv.ProcessKey (new KeyEvent ('v' + Key.AltMask, new KeyModifiers ())));
			Assert.Equal (1, hv.Position);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.PageDown, new KeyModifiers ())));
			Assert.Equal (41, hv.Position);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.PageUp, new KeyModifiers ())));
			Assert.Equal (1, hv.Position);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.Equal (64, hv.Position);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));
			Assert.Equal (1, hv.Position);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorRight | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (4, hv.Position);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorLeft | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (1, hv.Position);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorDown | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (37, hv.Position);

			Assert.True (hv.ProcessKey (new KeyEvent (Key.CursorUp | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (1, hv.Position);
		}
	}
}
