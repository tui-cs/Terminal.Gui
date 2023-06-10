using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminal.Gui.Resources;

namespace Terminal.Gui {

	/// <summary>
	/// Provides navigation and a user interface (UI) to collect related data across multiple steps. Each step (<see cref="WizardStep"/>) can host 
	/// arbitrary <see cref="View"/>s, much like a <see cref="Dialog"/>. Each step also has a pane for help text. Along the
	/// bottom of the Wizard view are customizable buttons enabling the user to navigate forward and backward through the Wizard. 
	/// </summary>
	/// <remarks>
	/// The Wizard can be displayed either as a modal (pop-up) <see cref="Window"/> (like <see cref="Dialog"/>) or as an embedded <see cref="View"/>. 
	/// 
	/// By default, <see cref="Wizard.Modal"/> is <c>true</c>. In this case launch the Wizard with <c>Application.Run(wizard)</c>. 
	/// 
	/// See <see cref="Wizard.Modal"/> for more details.
	/// </remarks>
	/// <example>
	/// <code>
	/// using Terminal.Gui;
	/// using System.Text;
	/// 
	/// Application.Init();
	/// 
	/// var wizard = new Wizard ($"Setup Wizard");
	/// 
	/// // Add 1st step
	/// var firstStep = new WizardStep ("End User License Agreement");
	/// wizard.AddStep(firstStep);
	/// firstStep.NextButtonText = "Accept!";
	/// firstStep.HelpText = "This is the End User License Agreement.";
	/// 
	/// // Add 2nd step
	/// var secondStep = new WizardStep ("Second Step");
	/// wizard.AddStep(secondStep);
	/// secondStep.HelpText = "This is the help text for the Second Step.";
	/// var lbl = new Label ("Name:") { AutoSize = true };
	/// secondStep.Add(lbl);
	/// 
	/// var name = new TextField () { X = Pos.Right (lbl) + 1, Width = Dim.Fill () - 1 };
	/// secondStep.Add(name);
	/// 
	/// wizard.Finished += (args) =>
	/// {
	///     MessageBox.Query("Wizard", $"Finished. The Name entered is '{name.Text}'", "Ok");
	///     Application.RequestStop();
	/// };
	/// 
	/// Application.Top.Add (wizard);
	/// Application.Run ();
	/// Application.Shutdown ();
	/// </code>
	/// </example>
	public class Wizard : Dialog {

		/// <summary>
		/// Initializes a new instance of the <see cref="Wizard"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		/// <remarks>
		/// The Wizard will be vertically and horizontally centered in the container.
		/// After initialization use <c>X</c>, <c>Y</c>, <c>Width</c>, and <c>Height</c> change size and position.
		/// </remarks>
		public Wizard () : base ()
		{

			// Using Justify causes the Back and Next buttons to be hard justified against
			// the left and right edge
			ButtonAlignment = ButtonAlignments.Justify;
			BorderStyle = LineStyle.Double;

			//// Add a horiz separator
			var separator = new LineView (Orientation.Horizontal) {
				Y = Pos.AnchorEnd (2)
			};
			Add (separator);

			// BUGBUG: Space is to work around https://github.com/gui-cs/Terminal.Gui/issues/1812
			backBtn = new Button (Strings.wzBack) { AutoSize = true };
			AddButton (backBtn);

			nextfinishBtn = new Button (Strings.wzFinish) { AutoSize = true };
			nextfinishBtn.IsDefault = true;
			AddButton (nextfinishBtn);

			backBtn.Clicked += BackBtn_Clicked;
			nextfinishBtn.Clicked += NextfinishBtn_Clicked;

			Loaded += Wizard_Loaded;
			Closing += Wizard_Closing;
			TitleChanged += Wizard_TitleChanged;

			if (Modal) {
				ClearKeyBinding (Command.QuitToplevel);
				AddKeyBinding (Key.Esc, Command.QuitToplevel);
			}
			SetNeedsLayout ();

		}

		private void Wizard_TitleChanged (object sender, TitleEventArgs e)
		{
			if (string.IsNullOrEmpty (wizardTitle)) {
				wizardTitle = e.NewTitle;
			}
		}

		private void Wizard_Loaded (object sender, EventArgs args)
		{
			CurrentStep = GetFirstStep (); // gets the first step if CurrentStep == null
		}

		private bool finishedPressed = false;

		private void Wizard_Closing (object sender, ToplevelClosingEventArgs obj)
		{
			if (!finishedPressed) {
				var args = new WizardButtonEventArgs ();
				Cancelled?.Invoke (this, args);
			}
		}

		private void NextfinishBtn_Clicked (object sender, EventArgs e)
		{
			if (CurrentStep == GetLastStep ()) {
				var args = new WizardButtonEventArgs ();
				Finished?.Invoke (this, args);
				if (!args.Cancel) {
					finishedPressed = true;
					if (IsCurrentTop) {
						Application.RequestStop (this);
					} else {
						// Wizard was created as a non-modal (just added to another View). 
						// Do nothing
					}
				}
			} else {
				var args = new WizardButtonEventArgs ();
				MovingNext?.Invoke (this, args);
				if (!args.Cancel) {
					GoNext ();
				}
			}
		}

		/// <summary>
		/// <see cref="Wizard"/> is derived from <see cref="Dialog"/> and Dialog causes <c>Esc</c> to call
		/// <see cref="Application.RequestStop(Toplevel)"/>, closing the Dialog. Wizard overrides <see cref="Responder.ProcessKey(KeyEvent)"/>
		/// to instead fire the <see cref="Cancelled"/> event when Wizard is being used as a non-modal (see <see cref="Wizard.Modal"/>.
		/// See <see cref="Responder.ProcessKey(KeyEvent)"/> for more.
		/// </summary>
		/// <param name="kb"></param>
		/// <returns></returns>
		public override bool ProcessKey (KeyEvent kb)
		{
			if (!Modal) {
				switch (kb.Key) {
				case Key.Esc:
					var args = new WizardButtonEventArgs ();
					Cancelled?.Invoke (this, args);
					return false;
				}
			}
			return base.ProcessKey (kb);
		}

		/// <summary>
		/// Causes the wizad to move to the next enabled step (or last step if <see cref="CurrentStep"/> is not set). 
		/// If there is no previous step, does nothing.
		/// </summary>
		public void GoNext ()
		{
			var nextStep = GetNextStep ();
			if (nextStep != null) {
				GoToStep (nextStep);
			}
		}

		/// <summary>
		/// Returns the next enabled <see cref="WizardStep"/> after the current step. Takes into account steps which
		/// are disabled. If <see cref="CurrentStep"/> is <c>null</c> returns the first enabled step.
		/// </summary>
		/// <returns>The next step after the current step, if there is one; otherwise returns <c>null</c>, which 
		/// indicates either there are no enabled steps or the current step is the last enabled step.</returns>
		public WizardStep GetNextStep ()
		{
			LinkedListNode<WizardStep> step = null;
			if (CurrentStep == null) {
				// Get first step, assume it is next
				step = steps.First;
			} else {
				// Get the step after current
				step = steps.Find (CurrentStep);
				if (step != null) {
					step = step.Next;
				}
			}

			// step now points to the potential next step
			while (step != null) {
				if (step.Value.Enabled) {
					return step.Value;
				}
				step = step.Next;
			}
			return null;
		}

		private void BackBtn_Clicked (object sender, EventArgs e)
		{
			var args = new WizardButtonEventArgs ();
			MovingBack?.Invoke (this, args);
			if (!args.Cancel) {
				GoBack ();
			}
		}

		/// <summary>
		/// Causes the wizad to move to the previous enabled step (or first step if <see cref="CurrentStep"/> is not set). 
		/// If there is no previous step, does nothing.
		/// </summary>
		public void GoBack ()
		{
			var previous = GetPreviousStep ();
			if (previous != null) {
				GoToStep (previous);
			}
		}

		/// <summary>
		/// Returns the first enabled <see cref="WizardStep"/> before the current step. Takes into account steps which
		/// are disabled. If <see cref="CurrentStep"/> is <c>null</c> returns the last enabled step.
		/// </summary>
		/// <returns>The first step ahead of the current step, if there is one; otherwise returns <c>null</c>, which 
		/// indicates either there are no enabled steps or the current step is the first enabled step.</returns>
		public WizardStep GetPreviousStep ()
		{
			LinkedListNode<WizardStep> step = null;
			if (CurrentStep == null) {
				// Get last step, assume it is previous
				step = steps.Last;
			} else {
				// Get the step before current
				step = steps.Find (CurrentStep);
				if (step != null) {
					step = step.Previous;
				}
			}

			// step now points to the potential previous step
			while (step != null) {
				if (step.Value.Enabled) {
					return step.Value;
				}
				step = step.Previous;
			}
			return null;
		}

		/// <summary>
		/// Returns the first enabled step in the Wizard
		/// </summary>
		/// <returns>The last enabled step</returns>
		public WizardStep GetFirstStep ()
		{
			return steps.FirstOrDefault (s => s.Enabled);
		}

		/// <summary>
		/// Returns the last enabled step in the Wizard
		/// </summary>
		/// <returns>The last enabled step</returns>
		public WizardStep GetLastStep ()
		{
			return steps.LastOrDefault (s => s.Enabled);
		}

		private LinkedList<WizardStep> steps = new LinkedList<WizardStep> ();
		private WizardStep currentStep = null;

		/// <summary>
		/// If the <see cref="CurrentStep"/> is not the first step in the wizard, this button causes
		/// the <see cref="MovingBack"/> event to be fired and the wizard moves to the previous step. 
		/// </summary>
		/// <remarks>
		/// Use the <see cref="MovingBack"></see> event to be notified when the user attempts to go back.
		/// </remarks>
		public Button BackButton { get => backBtn; }
		private Button backBtn;

		/// <summary>
		/// If the <see cref="CurrentStep"/> is the last step in the wizard, this button causes
		/// the <see cref="Finished"/> event to be fired and the wizard to close. If the step is not the last step,
		/// the <see cref="MovingNext"/> event will be fired and the wizard will move next step. 
		/// </summary>
		/// <remarks>
		/// Use the <see cref="MovingNext"></see> and <see cref="Finished"></see> events to be notified 
		/// when the user attempts go to the next step or finish the wizard.
		/// </remarks>
		public Button NextFinishButton { get => nextfinishBtn; }
		private Button nextfinishBtn;

		/// <summary>
		/// Adds a step to the wizard. The Next and Back buttons navigate through the added steps in the
		/// order they were added.
		/// </summary>
		/// <param name="newStep"></param>
		/// <remarks>The "Next..." button of the last step added will read "Finish" (unless changed from default).</remarks>
		public void AddStep (WizardStep newStep)
		{
			SizeStep (newStep);

			newStep.EnabledChanged += (s, e) => UpdateButtonsAndTitle ();
			newStep.TitleChanged += (s, e) => UpdateButtonsAndTitle ();
			steps.AddLast (newStep);
			this.Add (newStep);
			UpdateButtonsAndTitle ();
		}

		///// <summary>
		///// The title of the Wizard, shown at the top of the Wizard with " - currentStep.Title" appended.
		///// </summary>
		///// <remarks>
		///// The Title is only displayed when the <see cref="Wizard"/> <see cref="Wizard.Modal"/> is set to <c>false</c>.
		///// </remarks>
		//public new string Title {
		//	get {
		//		// The base (Dialog) Title holds the full title ("Wizard Title - Step Title")
		//		return base.Title;
		//	}
		//	set {
		//		wizardTitle = value;
		//		base.Title = $"{wizardTitle}{(steps.Count > 0 && currentStep != null ? " - " + currentStep.Title : string.Empty)}";
		//	}
		//}
		private string wizardTitle = string.Empty;

		/// <summary>
		/// Raised when the Back button in the <see cref="Wizard"/> is clicked. The Back button is always
		/// the first button in the array of Buttons passed to the <see cref="Wizard"/> constructor, if any.
		/// </summary>
		public event EventHandler<WizardButtonEventArgs> MovingBack;

		/// <summary>
		/// Raised when the Next/Finish button in the <see cref="Wizard"/> is clicked (or the user presses Enter). 
		/// The Next/Finish button is always the last button in the array of Buttons passed to the <see cref="Wizard"/> constructor, 
		/// if any. This event is only raised if the <see cref="CurrentStep"/> is the last Step in the Wizard flow 
		/// (otherwise the <see cref="Finished"/> event is raised).
		/// </summary>
		public event EventHandler<WizardButtonEventArgs> MovingNext;

		/// <summary>
		/// Raised when the Next/Finish button in the <see cref="Wizard"/> is clicked. The Next/Finish button is always
		/// the last button in the array of Buttons passed to the <see cref="Wizard"/> constructor, if any. This event is only
		/// raised if the <see cref="CurrentStep"/> is the last Step in the Wizard flow 
		/// (otherwise the <see cref="Finished"/> event is raised).
		/// </summary>
		public event EventHandler<WizardButtonEventArgs> Finished;

		/// <summary>
		/// Raised when the user has cancelled the <see cref="Wizard"/> by pressin the Esc key. 
		/// To prevent a modal (<see cref="Wizard.Modal"/> is <c>true</c>) Wizard from
		/// closing, cancel the event by setting <see cref="WizardButtonEventArgs.Cancel"/> to 
		/// <c>true</c> before returning from the event handler.
		/// </summary>
		public event EventHandler<WizardButtonEventArgs> Cancelled;

		/// <summary>
		/// This event is raised when the current <see cref="CurrentStep"/>) is about to change. Use <see cref="StepChangeEventArgs.Cancel"/> 
		/// to abort the transition.
		/// </summary>
		public event EventHandler<StepChangeEventArgs> StepChanging;

		/// <summary>
		/// This event is raised after the <see cref="Wizard"/> has changed the <see cref="CurrentStep"/>. 
		/// </summary>
		public event EventHandler<StepChangeEventArgs> StepChanged;

		/// <summary>
		/// Gets or sets the currently active <see cref="WizardStep"/>.
		/// </summary>
		public WizardStep CurrentStep {
			get => currentStep;
			set {
				GoToStep (value);
			}
		}

		/// <summary>
		/// Called when the <see cref="Wizard"/> is about to transition to another <see cref="WizardStep"/>. Fires the <see cref="StepChanging"/> event. 
		/// </summary>
		/// <param name="oldStep">The step the Wizard is about to change from</param>
		/// <param name="newStep">The step the Wizard is about to change to</param>
		/// <returns>True if the change is to be cancelled.</returns>
		public virtual bool OnStepChanging (WizardStep oldStep, WizardStep newStep)
		{
			var args = new StepChangeEventArgs (oldStep, newStep);
			StepChanging?.Invoke (this, args);
			return args.Cancel;
		}

		/// <summary>
		/// Called when the <see cref="Wizard"/> has completed transition to a new <see cref="WizardStep"/>. Fires the <see cref="StepChanged"/> event. 
		/// </summary>
		/// <param name="oldStep">The step the Wizard changed from</param>
		/// <param name="newStep">The step the Wizard has changed to</param>
		/// <returns>True if the change is to be cancelled.</returns>
		public virtual bool OnStepChanged (WizardStep oldStep, WizardStep newStep)
		{
			var args = new StepChangeEventArgs (oldStep, newStep);
			StepChanged?.Invoke (this, args);
			return args.Cancel;
		}

		/// <summary>
		/// Changes to the specified <see cref="WizardStep"/>.
		/// </summary>
		/// <param name="newStep">The step to go to.</param>
		/// <returns>True if the transition to the step succeeded. False if the step was not found or the operation was cancelled.</returns>
		public bool GoToStep (WizardStep newStep)
		{
			if (OnStepChanging (currentStep, newStep) || (newStep != null && !newStep.Enabled)) {
				return false;
			}

			// Hide all but the new step
			foreach (WizardStep step in steps) {
				step.Visible = (step == newStep);
				step.ShowHide ();
			}

			var oldStep = currentStep;
			currentStep = newStep;

			UpdateButtonsAndTitle ();

			// Set focus to the nav buttons
			if (backBtn.HasFocus) {
				backBtn.SetFocus ();
			} else {
				nextfinishBtn.SetFocus ();
			}

			if (OnStepChanged (oldStep, currentStep)) {
				// For correctness we do this, but it's meaningless because there's nothing to cancel
				return false;
			}

			return true;
		}

		private void UpdateButtonsAndTitle ()
		{
			if (CurrentStep == null) return;

			Title = $"{wizardTitle}{(steps.Count > 0 ? " - " + CurrentStep.Title : string.Empty)}";

			// Configure the Back button
			backBtn.Text = CurrentStep.BackButtonText != string.Empty ? CurrentStep.BackButtonText : Strings.wzBack; // "_Back";
			backBtn.Visible = (CurrentStep != GetFirstStep ());

			// Configure the Next/Finished button
			if (CurrentStep == GetLastStep ()) {
				nextfinishBtn.Text = CurrentStep.NextButtonText != string.Empty ? CurrentStep.NextButtonText : Strings.wzFinish; // "Fi_nish";
			} else {
				nextfinishBtn.Text = CurrentStep.NextButtonText != string.Empty ? CurrentStep.NextButtonText : Strings.wzNext; // "_Next...";
			}

			SizeStep (CurrentStep);

			SetNeedsLayout ();
			LayoutSubviews ();
			Draw ();
		}

		private void SizeStep (WizardStep step)
		{
			if (Modal) {
				// If we're modal, then we expand the WizardStep so that the top and side 
				// borders and not visible. The bottom border is the separator above the buttons.
				step.X = step.Y = 0;
				step.Height = Dim.Fill (2); // for button frame
				step.Width = Dim.Fill (0);
			} else {
				// If we're not a modal, then we show the border around the WizardStep
				step.X = step.Y = 0;
				step.Height = Dim.Fill (1); // for button frame
				step.Width = Dim.Fill (0);
			}
		}

		/// <summary>
		/// Determines whether the <see cref="Wizard"/> is displayed as modal pop-up or not.
		/// 
		/// The default is <see langword="true"/>. The Wizard will be shown with a frame and title and will behave like
		/// any <see cref="Toplevel"/> window.
		/// 
		/// If set to <c>false</c> the Wizard will have no frame and will behave like any embedded <see cref="View"/>.
		/// 
		/// To use Wizard as an embedded View 
		/// <list type="number">
		/// <item><description>Set <see cref="Modal"/> to <c>false</c>.</description></item>
		/// <item><description>Add the Wizard to a containing view with <see cref="View.Add(View)"/>.</description></item>
		/// </list>
		/// 
		/// If a non-Modal Wizard is added to the application after <see cref="Application.Run(Func{Exception, bool})"/> has been called
		/// the first step must be explicitly set by setting <see cref="CurrentStep"/> to <see cref="GetNextStep()"/>:
		/// <code>
		///    wizard.CurrentStep = wizard.GetNextStep();
		/// </code>
		/// </summary>
		public new bool Modal {
			get => base.Modal;
			set {
				base.Modal = value;
				foreach (var step in steps) {
					SizeStep (step);
				}
				if (base.Modal) {
					ColorScheme = Colors.Dialog;
					BorderStyle = LineStyle.Rounded;
				} else {
					if (SuperView != null) {
						ColorScheme = SuperView.ColorScheme;
					} else {
						ColorScheme = Colors.Base;
					}
					CanFocus = true;
					BorderStyle = LineStyle.None;
				}
			}
		}
	}
}