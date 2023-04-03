using Xunit;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.CoreTests {
	public class ResponderTests {
		[Fact]
		public void New_Initializes ()
		{
			var r = new Responder ();
			Assert.NotNull (r);
			Assert.Equal ("Terminal.Gui.Responder", r.ToString ());
			Assert.False (r.CanFocus);
			Assert.False (r.HasFocus);
			Assert.True (r.Enabled);
			Assert.True (r.Visible);
		}

		[Fact]
		public void New_Methods_Return_False ()
		{
			var r = new Responder ();

			Assert.False (r.ProcessKey (new KeyEvent () { Key = Key.Unknown }));
			Assert.False (r.ProcessHotKey (new KeyEvent () { Key = Key.Unknown }));
			Assert.False (r.ProcessColdKey (new KeyEvent () { Key = Key.Unknown }));
			Assert.False (r.OnKeyDown (new KeyEvent () { Key = Key.Unknown }));
			Assert.False (r.OnKeyUp (new KeyEvent () { Key = Key.Unknown }));
			Assert.False (r.MouseEvent (new MouseEvent () { Flags = MouseFlags.AllEvents }));
			Assert.False (r.OnMouseEnter (new MouseEvent () { Flags = MouseFlags.AllEvents }));
			Assert.False (r.OnMouseLeave (new MouseEvent () { Flags = MouseFlags.AllEvents }));
			Assert.False (r.OnEnter (new View ()));
			Assert.False (r.OnLeave (new View ()));
		}

		// Generic lifetime (IDisposable) tests
		[Fact]
		public void Dispose_Works ()
		{

		}
		
		public class DerivedView : View {
			public DerivedView ()
			{
			}

			public override bool OnKeyDown (KeyEvent keyEvent)
			{
				return true;
			}
		}

		[Fact]
		public void IsOverridden_False_IfNotOverridden ()
		{
			// MouseEvent IS defined on Responder but NOT overridden
			Assert.False (Responder.IsOverridden (new Responder () { }, "MouseEvent"));

			// MouseEvent is defined on Responder and NOT overrident on View
			Assert.False (Responder.IsOverridden (new View () { Text = "View does not override MouseEvent" }, "MouseEvent"));
			Assert.False (Responder.IsOverridden (new DerivedView () { Text = "DerivedView does not override MouseEvent" }, "MouseEvent"));

			// MouseEvent is NOT defined on DerivedView 
			Assert.False (Responder.IsOverridden (new DerivedView () { Text = "DerivedView does not override MouseEvent" }, "MouseEvent"));

			// OnKeyDown is defined on View and NOT overrident on Button
			Assert.False (Responder.IsOverridden (new Button () { Text = "Button does not override OnKeyDown" }, "OnKeyDown"));
		}

		[Fact]
		public void IsOverridden_True_IfOverridden ()
		{
			// MouseEvent is defined on Responder IS overriden on ScrollBarView (but not View)
			Assert.True (Responder.IsOverridden (new ScrollBarView () { Text = "ScrollBarView overrides MouseEvent" }, "MouseEvent"));

			// OnKeyDown is defined on View
			Assert.True (Responder.IsOverridden (new View () { Text = "View overrides OnKeyDown" }, "OnKeyDown"));

			// OnKeyDown is defined on DerivedView
			Assert.True (Responder.IsOverridden (new DerivedView () { Text = "DerivedView overrides OnKeyDown" }, "OnKeyDown"));
			
			// ScrollBarView overrides both MouseEvent (from Responder) and Redraw (from View)
			Assert.True (Responder.IsOverridden (new ScrollBarView () { Text = "ScrollBarView overrides MouseEvent" }, "MouseEvent"));
			Assert.True (Responder.IsOverridden (new ScrollBarView () { Text = "ScrollBarView overrides Redraw" }, "Redraw"));

			Assert.True (Responder.IsOverridden (new Button () { Text = "Button overrides MouseEvent" }, "MouseEvent"));
		}
	}
}
