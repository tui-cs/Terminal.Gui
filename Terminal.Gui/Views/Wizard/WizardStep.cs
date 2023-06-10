namespace Terminal.Gui; 

/// <summary>
/// Represents a basic step that is displayed in a <see cref="Wizard"/>. The <see cref="WizardStep"/> view is divided horizontally in two. On the left is the
/// content view where <see cref="View"/>s can be added,  On the right is the help for the step.
/// Set <see cref="WizardStep.HelpText"/> to set the help text. If the help text is empty the help pane will not
/// be shown. 
/// 
/// If there are no Views added to the WizardStep the <see cref="HelpText"/> (if not empty) will fill the wizard step. 
/// </summary>
/// <remarks>
/// If <see cref="Button"/>s are added, do not set <see cref="Button.IsDefault"/> to true as this will conflict
/// with the Next button of the Wizard.
/// 
/// Subscribe to the <see cref="View.VisibleChanged"/> event to be notified when the step is active; see also: <see cref="Wizard.StepChanged"/>.
/// 
/// To enable or disable a step from being shown to the user, set <see cref="View.Enabled"/>.
/// 
/// </remarks>
public class WizardStep : FrameView {
	///// <summary>
	///// The title of the <see cref="WizardStep"/>. 
	///// </summary>
	///// <remarks>The Title is only displayed when the <see cref="Wizard"/> is used as a modal pop-up (see <see cref="Wizard.Modal"/>.</remarks>
	//public new string Title {
	//	// BUGBUG: v2 - No need for this as View now has Title w/ notifications.
	//	get => title;
	//	set {
	//		if (!OnTitleChanging (title, value)) {
	//			var old = title;
	//			title = value;
	//			OnTitleChanged (old, title);
	//		}
	//		base.Title = value;
	//		SetNeedsDisplay ();
	//	}
	//}

	//private string title = string.Empty;

	// The contentView works like the ContentView in FrameView.
	private View contentView = new View () { Data = "WizardContentView" };

	/// <summary>
	/// Sets or gets help text for the <see cref="WizardStep"/>.If <see cref="WizardStep.HelpText"/> is empty
	/// the help pane will not be visible and the content will fill the entire WizardStep.
	/// </summary>
	/// <remarks>The help text is displayed using a read-only <see cref="TextView"/>.</remarks>
	public string HelpText {
		get => helpTextView.Text;
		set {
			helpTextView.Text = value;
			ShowHide ();
			SetNeedsDisplay ();
		}
	}
	private TextView helpTextView = new TextView ();

	/// <summary>
	/// Sets or gets the text for the back button. The back button will only be visible on 
	/// steps after the first step.
	/// </summary>
	/// <remarks>The default text is "Back"</remarks>
	public string BackButtonText { get; set; } = string.Empty;

	/// <summary>
	/// Sets or gets the text for the next/finish button.
	/// </summary>
	/// <remarks>The default text is "Next..." if the Pane is not the last pane. Otherwise it is "Finish"</remarks>
	public string NextButtonText { get; set; } = string.Empty;

	/// <summary>
	/// Initializes a new instance of the <see cref="Wizard"/> class using <see cref="LayoutStyle.Computed"/> positioning.
	/// </summary>
	public WizardStep ()
	{
		BorderStyle = LineStyle.None;
		base.Add (contentView);

		helpTextView.ReadOnly = true;
		helpTextView.WordWrap = true;
		base.Add (helpTextView);

		// BUGBUG: v2 - Disabling scrolling for now
		//var scrollBar = new ScrollBarView (helpTextView, true);

		//scrollBar.ChangedPosition += (s,e) => {
		//	helpTextView.TopRow = scrollBar.Position;
		//	if (helpTextView.TopRow != scrollBar.Position) {
		//		scrollBar.Position = helpTextView.TopRow;
		//	}
		//	helpTextView.SetNeedsDisplay ();
		//};

		//scrollBar.OtherScrollBarView.ChangedPosition += (s,e) => {
		//	helpTextView.LeftColumn = scrollBar.OtherScrollBarView.Position;
		//	if (helpTextView.LeftColumn != scrollBar.OtherScrollBarView.Position) {
		//		scrollBar.OtherScrollBarView.Position = helpTextView.LeftColumn;
		//	}
		//	helpTextView.SetNeedsDisplay ();
		//};

		//scrollBar.VisibleChanged += (s,e) => {
		//	if (scrollBar.Visible && helpTextView.RightOffset == 0) {
		//		helpTextView.RightOffset = 1;
		//	} else if (!scrollBar.Visible && helpTextView.RightOffset == 1) {
		//		helpTextView.RightOffset = 0;
		//	}
		//};

		//scrollBar.OtherScrollBarView.VisibleChanged += (s,e) => {
		//	if (scrollBar.OtherScrollBarView.Visible && helpTextView.BottomOffset == 0) {
		//		helpTextView.BottomOffset = 1;
		//	} else if (!scrollBar.OtherScrollBarView.Visible && helpTextView.BottomOffset == 1) {
		//		helpTextView.BottomOffset = 0;
		//	}
		//};

		//helpTextView.DrawContent += (s,e) => {
		//	scrollBar.Size = helpTextView.Lines;
		//	scrollBar.Position = helpTextView.TopRow;
		//	if (scrollBar.OtherScrollBarView != null) {
		//		scrollBar.OtherScrollBarView.Size = helpTextView.Maxlength;
		//		scrollBar.OtherScrollBarView.Position = helpTextView.LeftColumn;
		//	}
		//	scrollBar.LayoutSubviews ();
		//	scrollBar.Refresh ();
		//};
		//base.Add (scrollBar);
		ShowHide ();
	}

	/// <summary>
	/// Does the work to show and hide the contentView and helpView as appropriate
	/// </summary>
	internal void ShowHide ()
	{
		contentView.Height = Dim.Fill ();
		helpTextView.Height = Dim.Fill ();
		helpTextView.Width = Dim.Fill ();

		if (contentView.InternalSubviews?.Count > 0) {
			if (helpTextView.Text.Length > 0) {
				contentView.Width = Dim.Percent (70);
				helpTextView.X = Pos.Right (contentView);
				helpTextView.Width = Dim.Fill ();

			} else {
				contentView.Width = Dim.Fill ();
			}
		} else {
			if (helpTextView.Text.Length > 0) {
				helpTextView.X = 0;
			} else {
				// Error - no pane shown
			}

		}
		contentView.Visible = contentView.InternalSubviews?.Count > 0;
		helpTextView.Visible = helpTextView.Text.Length > 0;
	}

	/// <summary>
	/// Add the specified <see cref="View"/> to the <see cref="WizardStep"/>. 
	/// </summary>
	/// <param name="view"><see cref="View"/> to add to this container</param>
	public override void Add (View view)
	{
		contentView.Add (view);
		if (view.CanFocus) {
			CanFocus = true;
		}
		ShowHide ();
	}

	/// <summary>
	///   Removes a <see cref="View"/> from <see cref="WizardStep"/>.
	/// </summary>
	/// <remarks>
	/// </remarks>
	public override void Remove (View view)
	{
		if (view == null) {
			return;
		}

		SetNeedsDisplay ();
		var touched = view.Frame;
		contentView.Remove (view);

		if (contentView.InternalSubviews.Count < 1) {
			this.CanFocus = false;
		}
		ShowHide ();
	}

	/// <summary>
	///   Removes all <see cref="View"/>s from the <see cref="WizardStep"/>.
	/// </summary>
	/// <remarks>
	/// </remarks>
	public override void RemoveAll ()
	{
		contentView.RemoveAll ();
		ShowHide ();
	}

} // end of WizardStep class