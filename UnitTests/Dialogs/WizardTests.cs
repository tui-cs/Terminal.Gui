using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Terminal.Gui;
using Xunit;
using System.Globalization;
using Xunit.Abstractions;
using System.Text;
using static Terminal.Gui.Application;

namespace Terminal.Gui.DialogTests {

	public class WizardTests {
		readonly ITestOutputHelper output;

		public WizardTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		private void RunButtonTestWizard (string title, int width, int height)
		{
			var wizard = new Wizard () { Title = title, Width = width, Height = height };
			Application.End (Application.Begin (wizard));
		}

		// =========== WizardStep Tests

		[Fact, AutoInitShutdown]
		public void WizardStep_ButtonText ()
		{
			// Verify default button text

			// Verify set actually changes property

			// Verify set actually changes buttons for the current step
		}

		[Fact]
		public void WizardStep_Set_Title_Fires_TitleChanging ()
		{
			var r = new Window ();
			Assert.Equal (string.Empty, r.Title);

			string expectedAfter = string.Empty;
			string expectedDuring = string.Empty;
			bool cancel = false;
			r.TitleChanging += (s,args) => {
				Assert.Equal (expectedDuring, args.NewTitle);
				args.Cancel = cancel;
			};

			r.Title = expectedDuring = expectedAfter = "title";
			Assert.Equal (expectedAfter, r.Title);

			r.Title = expectedDuring = expectedAfter = "a different title";
			Assert.Equal (expectedAfter, r.Title);

			// Now setup cancelling the change and change it back to "title"
			cancel = true;
			r.Title = expectedDuring = "title";
			Assert.Equal (expectedAfter, r.Title);
			r.Dispose ();

		}

		[Fact]
		public void WizardStep_Set_Title_Fires_TitleChanged ()
		{
			var r = new Window ();
			Assert.Equal (string.Empty, r.Title);

			string expected = string.Empty;
			r.TitleChanged += (s,args) => {
				Assert.Equal (r.Title, args.NewTitle);
			};

			expected = "title";
			r.Title = expected;
			Assert.Equal (expected, r.Title);

			expected = "another title";
			r.Title = expected;
			Assert.Equal (expected, r.Title);
			r.Dispose ();

		}

		// =========== Wizard Tests
		[Fact, AutoInitShutdown]
		public void DefaultConstructor_SizedProperly ()
		{
			var d = (FakeDriver)Application.Driver;

			var wizard = new Wizard ();
			Assert.NotEqual (0, wizard.Width);
			Assert.NotEqual (0, wizard.Height);
		}

		[Fact, AutoInitShutdown]
		// Verify a zero-step wizard doesn't crash and shows a blank wizard
		// and that the title is correct
		public void ZeroStepWizard_Shows ()
		{
			var d = (FakeDriver)Application.Driver;

			var title = "1234";
			var stepTitle = "";

			int width = 30;
			int height = 6;
			d.SetBufferSize (width, height);

			var btnBackText = "Back";
			var btnBack = $"{CM.Glyphs.LeftBracket} {btnBackText} {CM.Glyphs.RightBracket}";
			var btnNextText = "Finish";
			var btnNext = $"{CM.Glyphs.LeftBracket}{CM.Glyphs.LeftDefaultIndicator} {btnNextText} {CM.Glyphs.RightDefaultIndicator}{CM.Glyphs.RightBracket}";

			var topRow = $"{CM.Glyphs.ULCornerDbl}╡{title}{stepTitle}╞{new string (CM.Glyphs.HLineDbl.ToString () [0], width - title.Length - stepTitle.Length - 4)}{CM.Glyphs.URCornerDbl}";
			var row2 = $"{CM.Glyphs.VLineDbl}{new string (' ', width - 2)}{CM.Glyphs.VLineDbl}";
			var row3 = row2;
			var separatorRow = $"{CM.Glyphs.VLineDbl}{new string (CM.Glyphs.HLine.ToString () [0], width - 2)}{CM.Glyphs.VLineDbl}";
			var buttonRow = $"{CM.Glyphs.VLineDbl}{btnBack}{new string (' ', width - btnBack.Length - btnNext.Length - 2)}{btnNext}{CM.Glyphs.VLineDbl}";
			var bottomRow = $"{CM.Glyphs.LLCornerDbl}{new string (CM.Glyphs.HLineDbl.ToString () [0], width - 2)}{CM.Glyphs.LRCornerDbl}";

			var wizard = new Wizard () { Title = title, Width = width, Height = height };
			var runstate = Application.Begin (wizard);
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{row2}\n{row3}\n{separatorRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);
		}

		[Fact, AutoInitShutdown]
		// This test verifies that a single step wizard shows the correct buttons
		// and that the title is correct
		public void OneStepWizard_Shows ()
		{
			var d = (FakeDriver)Application.Driver;

			var title = "1234";
			var stepTitle = "ABCD";

			int width = 30;
			int height = 7;
			d.SetBufferSize (width, height);

			//	var btnBackText = "Back";
			var btnBack = string.Empty; // $"{CM.Glyphs.LeftBracket} {btnBackText} {CM.Glyphs.RightBracket}";
			var btnNextText = "Finish"; // "Next";
			var btnNext = $"{CM.Glyphs.LeftBracket}{CM.Glyphs.LeftDefaultIndicator} {btnNextText} {CM.Glyphs.RightDefaultIndicator}{CM.Glyphs.RightBracket}";

			var topRow = $"{CM.Glyphs.ULCornerDbl}╡{title} - {stepTitle}╞{new string (CM.Glyphs.HLineDbl.ToString () [0], width - title.Length - stepTitle.Length - 7)}{CM.Glyphs.URCornerDbl}";
			var row2 = $"{CM.Glyphs.VLineDbl}{new string (' ', width - 2)}{CM.Glyphs.VLineDbl}";
			var row3 = row2;
			var row4 = row3;
			var separatorRow = $"{CM.Glyphs.VLineDbl}{new string (CM.Glyphs.HLine.ToString () [0], width - 2)}{CM.Glyphs.VLineDbl}";
			var buttonRow = $"{CM.Glyphs.VLineDbl}{btnBack}{new string (' ', width - btnBack.Length - btnNext.Length - 2)}{btnNext}{CM.Glyphs.VLineDbl}";
			var bottomRow = $"{CM.Glyphs.LLCornerDbl}{new string (CM.Glyphs.HLineDbl.ToString () [0], width - 2)}{CM.Glyphs.LRCornerDbl}";

			var wizard = new Wizard () { Title = title, Width = width, Height = height };
			wizard.AddStep (new WizardStep () { Title = stepTitle });
			//wizard.LayoutSubviews ();
			var firstIteration = false;
			var runstate = Application.Begin (wizard);
			Application.RunMainLoopIteration (ref runstate, true, ref firstIteration);

			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{row2}\n{row3}\n{row4}\n{separatorRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);
		}

		[Fact, AutoInitShutdown]
		// This test verifies that the 2nd step in a wizard with 2 steps 
		// shows the correct buttons on both steps
		// and that the title is correct
		public void TwoStepWizard_Next_Shows_SecondStep ()
		{
			// verify step one

			// Next

			// verify step two

			// Back

			// verify step one again
		}

		[Fact, AutoInitShutdown]
		// This test verifies that the 2nd step in a wizard with more than 2 steps 
		// shows the correct buttons on all steps
		// and that the title is correct
		public void ThreeStepWizard_Next_Shows_Steps ()
		{

			// verify step one

			// Next

			// verify step two

			// Back

			// verify step one again
		}

		[Fact, AutoInitShutdown]
		// this test is needed because Wizard overrides Dialog's title behavior ("Title - StepTitle")
		public void Setting_Title_Works ()
		{
			var d = (FakeDriver)Application.Driver;

			var title = "1234";
			var stepTitle = " - ABCD";

			int width = 40;
			int height = 4;
			d.SetBufferSize (width, height);

			var btnNextText = "Finish";
			var btnNext = $"{CM.Glyphs.LeftBracket}{CM.Glyphs.LeftDefaultIndicator} {btnNextText} {CM.Glyphs.RightDefaultIndicator}{CM.Glyphs.RightBracket}";

			var topRow = $"{CM.Glyphs.ULCornerDbl}╡{title}{stepTitle}╞{new string (CM.Glyphs.HLineDbl.ToString () [0], width - title.Length - stepTitle.Length - 4)}{CM.Glyphs.URCornerDbl}";
			var separatorRow = $"{CM.Glyphs.VLineDbl}{new string (CM.Glyphs.HLine.ToString () [0], width - 2)}{CM.Glyphs.VLineDbl}";

			// Once this is fixed, revert to commented out line: https://github.com/gui-cs/Terminal.Gui/issues/1791
			var buttonRow = $"{CM.Glyphs.VLineDbl}{new string (' ', width - btnNext.Length - 2)}{btnNext}{CM.Glyphs.VLineDbl}";
			//var buttonRow = $"{CM.Glyphs.VDLine}{new String (' ', width - btnNext.Length - 2)}{btnNext}{CM.Glyphs.VDLine}";
			var bottomRow = $"{CM.Glyphs.LLCornerDbl}{new string (CM.Glyphs.HLineDbl.ToString () [0], width - 2)}{CM.Glyphs.LRCornerDbl}";

			var wizard = new Wizard () { Title = title, Width = width, Height = height };
			wizard.AddStep (new WizardStep () { Title = "ABCD" });

			Application.End (Application.Begin (wizard));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{separatorRow}\n{buttonRow}\n{bottomRow}", output);
		}

		[Fact, AutoInitShutdown]
		public void Navigate_GetPreviousStep_Correct ()
		{
			var wizard = new Wizard ();

			// If no steps should be null
			Assert.Null (wizard.GetPreviousStep ());

			var step1 = new WizardStep () { Title = "step1" };
			wizard.AddStep (step1);

			// If no current step, should be last step
			Assert.Equal (step1.Title, wizard.GetPreviousStep ().Title);

			wizard.CurrentStep = step1;
			// If there is 1 step it's current step should be null
			Assert.Null (wizard.GetPreviousStep ());

			// If one disabled step should be null
			step1.Enabled = false;
			Assert.Null (wizard.GetPreviousStep ());

			// If two steps and at 2 and step 1 is `Enabled = true`should be step1
			var step2 = new WizardStep () { Title = "step2" };
			wizard.AddStep (step2);
			wizard.CurrentStep = step2;
			step1.Enabled = true;
			Assert.Equal (step1.Title, wizard.GetPreviousStep ().Title);

			// If two steps and at 2 and step 1 is `Enabled = false` should be null
			step1.Enabled = false;
			Assert.Null (wizard.GetPreviousStep ());

			// If three steps with Step2.Enabled = true
			//   At step 1 should be null
			//   At step 2 should be step 1
			//   At step 3 should be step 2
			var step3 = new WizardStep () { Title = "step3" };
			wizard.AddStep (step3);
			step1.Enabled = true;
			wizard.CurrentStep = step1;
			step2.Enabled = true;
			step3.Enabled = true;
			Assert.Null (wizard.GetPreviousStep ());
			wizard.CurrentStep = step2;
			Assert.Equal (step1.Title, wizard.GetPreviousStep ().Title);
			wizard.CurrentStep = step3;
			Assert.Equal (step2.Title, wizard.GetPreviousStep ().Title);

			// If three steps with Step2.Enabled = false
			//   At step 1 should be null
			//   At step 3 should be step1
			step1.Enabled = true;
			step2.Enabled = false;
			step3.Enabled = true;
			wizard.CurrentStep = step1;
			Assert.Null (wizard.GetPreviousStep ());
			wizard.CurrentStep = step3;
			Assert.Equal (step1.Title, wizard.GetPreviousStep ().Title);

			// If three steps with Step1.Enabled = false & Step2.Enabled = false
			//   At step 3 should be null

			// If no current step, GetPreviousStep provides equivalent to GetLastStep
			wizard.CurrentStep = null;
			step1.Enabled = true;
			step2.Enabled = true;
			step3.Enabled = true;
			Assert.Equal (step3.Title, wizard.GetPreviousStep ().Title);

			step1.Enabled = false;
			step2.Enabled = true;
			step3.Enabled = true;
			Assert.Equal (step3.Title, wizard.GetPreviousStep ().Title);

			step1.Enabled = false;
			step2.Enabled = false;
			step3.Enabled = true;
			Assert.Equal (step3.Title, wizard.GetPreviousStep ().Title);

			step1.Enabled = false;
			step2.Enabled = true;
			step3.Enabled = false;
			Assert.Equal (step2.Title, wizard.GetPreviousStep ().Title);

			step1.Enabled = true;
			step2.Enabled = false;
			step3.Enabled = false;
			Assert.Equal (step1.Title, wizard.GetPreviousStep ().Title);
		}

		[Fact, AutoInitShutdown]
		public void Navigate_GetNextStep_Correct ()
		{
			var wizard = new Wizard ();

			// If no steps should be null
			Assert.Null (wizard.GetNextStep ());

			var step1 = new WizardStep () { Title = "step1" };
			wizard.AddStep (step1);

			// If no current step, should be first step
			Assert.Equal (step1.Title, wizard.GetNextStep ().Title);

			wizard.CurrentStep = step1;
			// If there is 1 step it's current step should be null
			Assert.Null (wizard.GetNextStep ());

			// If one disabled step should be null
			step1.Enabled = false;
			Assert.Null (wizard.GetNextStep ());

			// If two steps and at 1 and step 2 is `Enabled = true`should be step 2
			var step2 = new WizardStep () { Title = "step2" };
			wizard.AddStep (step2);
			Assert.Equal (step2.Title, wizard.GetNextStep ().Title);

			// If two steps and at 1 and step 2 is `Enabled = false` should be null
			step1.Enabled = true;
			wizard.CurrentStep = step1;
			step2.Enabled = false;
			Assert.Null (wizard.GetNextStep ());

			// If three steps with Step2.Enabled = true
			//   At step 1 should be step 2
			//   At step 2 should be step 3
			//   At step 3 should be null
			var step3 = new WizardStep () { Title = "step3" };
			wizard.AddStep (step3);
			step1.Enabled = true;
			wizard.CurrentStep = step1;
			step2.Enabled = true;
			step3.Enabled = true;
			Assert.Equal (step2.Title, wizard.GetNextStep ().Title);
			wizard.CurrentStep = step2;
			Assert.Equal (step3.Title, wizard.GetNextStep ().Title);
			wizard.CurrentStep = step3;
			Assert.Null (wizard.GetNextStep ());

			// If three steps with Step2.Enabled = false
			//   At step 1 should be step 3
			//   At step 3 should be null
			step1.Enabled = true;
			wizard.CurrentStep = step1;
			step2.Enabled = false;
			step3.Enabled = true;
			Assert.Equal (step3.Title, wizard.GetNextStep ().Title);
			wizard.CurrentStep = step3;
			Assert.Null (wizard.GetNextStep ());

			// If three steps with Step2.Enabled = false & Step3.Enabled = false
			//   At step 1 should be null
			step1.Enabled = true;
			wizard.CurrentStep = step1;
			step2.Enabled = false;
			step3.Enabled = false;
			Assert.Null (wizard.GetNextStep ());

			// If no current step, GetNextStep provides equivalent to GetFirstStep
			wizard.CurrentStep = null;
			step1.Enabled = true;
			step2.Enabled = true;
			step3.Enabled = true;
			Assert.Equal (step1.Title, wizard.GetNextStep ().Title);

			step1.Enabled = false;
			step2.Enabled = true;
			step3.Enabled = true;
			Assert.Equal (step2.Title, wizard.GetNextStep ().Title);

			step1.Enabled = false;
			step2.Enabled = false;
			step3.Enabled = true;
			Assert.Equal (step3.Title, wizard.GetNextStep ().Title);

			step1.Enabled = false;
			step2.Enabled = true;
			step3.Enabled = false;
			Assert.Equal (step2.Title, wizard.GetNextStep ().Title);

			step1.Enabled = true;
			step2.Enabled = false;
			step3.Enabled = false;
			Assert.Equal (step1.Title, wizard.GetNextStep ().Title);
		}

		[Fact, AutoInitShutdown]
		public void Navigate_GoNext_Works ()
		{
			// If zero steps do nothing

			// If one step do nothing (enabled or disabled)

			// If two steps
			//    If current is 1
			//        If 2 is enabled 2 becomes current
			//        If 2 is disabled 1 stays current
			//    If current is 2 does nothing
		}

		[Fact, AutoInitShutdown]
		public void Navigate_GoBack_Works ()
		{
			// If zero steps do nothing

			// If one step do nothing (enabled or disabled)

			// If two steps
			//    If current is 1 does nothing
			//    If current is 2 does nothing
			//        If 1 is enabled 2 becomes current
			//        If 1 is disabled 1 stays current
		}

		[Fact, AutoInitShutdown]
		public void Navigate_GetFirstStep_Works ()
		{
			var wizard = new Wizard ();

			Assert.Null (wizard.GetFirstStep ());

			var step1 = new WizardStep () { Title = "step1" };
			wizard.AddStep (step1);
			Assert.Equal (step1.Title, wizard.GetFirstStep ().Title);

			var step2 = new WizardStep () { Title = "step2" };
			wizard.AddStep (step2);
			Assert.Equal (step1.Title, wizard.GetFirstStep ().Title);

			var step3 = new WizardStep () { Title = "step3" };
			wizard.AddStep (step3);
			Assert.Equal (step1.Title, wizard.GetFirstStep ().Title);

			step1.Enabled = false;
			Assert.Equal (step2.Title, wizard.GetFirstStep ().Title);

			step1.Enabled = true;
			Assert.Equal (step1.Title, wizard.GetFirstStep ().Title);

			step1.Enabled = false;
			step2.Enabled = false;
			Assert.Equal (step3.Title, wizard.GetFirstStep ().Title);
		}

		[Fact, AutoInitShutdown]
		public void Navigate_GetLastStep_Works ()
		{
			var wizard = new Wizard ();

			Assert.Null (wizard.GetLastStep ());

			var step1 = new WizardStep () { Title = "step1" };
			wizard.AddStep (step1);
			Assert.Equal (step1.Title, wizard.GetLastStep ().Title);

			var step2 = new WizardStep () { Title = "step2" };
			wizard.AddStep (step2);
			Assert.Equal (step2.Title, wizard.GetLastStep ().Title);

			var step3 = new WizardStep () { Title = "step3" };
			wizard.AddStep (step3);
			Assert.Equal (step3.Title, wizard.GetLastStep ().Title);

			step3.Enabled = false;
			Assert.Equal (step2.Title, wizard.GetLastStep ().Title);

			step3.Enabled = true;
			Assert.Equal (step3.Title, wizard.GetLastStep ().Title);

			step3.Enabled = false;
			step2.Enabled = false;
			Assert.Equal (step1.Title, wizard.GetLastStep ().Title);
		}

		[Fact, AutoInitShutdown]
		public void Finish_Button_Closes ()
		{
			// https://github.com/gui-cs/Terminal.Gui/issues/1833
			var wizard = new Wizard ();
			var step1 = new WizardStep () { Title = "step1" };
			wizard.AddStep (step1);

			var finishedFired = false;
			wizard.Finished += (s, args) => {
				finishedFired = true;
			};

			var closedFired = false;
			wizard.Closed += (s, e) => {
				closedFired = true;
			};

			var runstate = Application.Begin (wizard);
			var firstIteration = true;
			Application.RunMainLoopIteration (ref runstate, true, ref firstIteration);

			wizard.NextFinishButton.OnClicked ();
			Application.RunMainLoopIteration (ref runstate, true, ref firstIteration);
			Application.End (runstate);
			Assert.True (finishedFired);
			Assert.True (closedFired);
			step1.Dispose ();
			wizard.Dispose ();

			// Same test, but with two steps
			wizard = new Wizard ();
			firstIteration = false;
			step1 = new WizardStep () { Title = "step1" };
			wizard.AddStep (step1);
			var step2 = new WizardStep () { Title = "step2" };
			wizard.AddStep (step2);

			finishedFired = false;
			wizard.Finished += (s, args) => {
				finishedFired = true;
			};

			closedFired = false;
			wizard.Closed += (s,e) => {
				closedFired = true;
			};

			runstate = Application.Begin (wizard);
			Application.RunMainLoopIteration (ref runstate, true, ref firstIteration);

			Assert.Equal (step1.Title, wizard.CurrentStep.Title);
			wizard.NextFinishButton.OnClicked ();
			Assert.False (finishedFired);
			Assert.False (closedFired);

			Assert.Equal (step2.Title, wizard.CurrentStep.Title);
			Assert.Equal (wizard.GetLastStep ().Title, wizard.CurrentStep.Title);
			wizard.NextFinishButton.OnClicked ();
			Application.End (runstate);
			Assert.True (finishedFired);
			Assert.True (closedFired);

			step1.Dispose ();
			step2.Dispose ();
			wizard.Dispose ();

			// Same test, but with two steps but the 1st one disabled
			wizard = new Wizard ();
			firstIteration = false;
			step1 = new WizardStep () { Title = "step1" };
			wizard.AddStep (step1);
			step2 = new WizardStep () { Title = "step2" };
			wizard.AddStep (step2);
			step1.Enabled = false;

			finishedFired = false;
			wizard.Finished += (s, args) => {
				finishedFired = true;
			};

			closedFired = false;
			wizard.Closed += (s,e) => {
				closedFired = true;
			};

			runstate = Application.Begin (wizard);
			Application.RunMainLoopIteration (ref runstate, true, ref firstIteration);

			Assert.Equal (step2.Title, wizard.CurrentStep.Title);
			Assert.Equal (wizard.GetLastStep ().Title, wizard.CurrentStep.Title);
			wizard.NextFinishButton.OnClicked ();
			Application.End (runstate);
			Assert.True (finishedFired);
			Assert.True (closedFired);

		}
	}
}