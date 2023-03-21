using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using Terminal.Gui.Trees;
using static UICatalog.Scenario;

namespace UICatalog.Scenarios {

	[ScenarioMetadata (Name: "Interactive Tree", Description: "Create nodes and child nodes in TreeView.")]
	[ScenarioCategory ("Controls"), ScenarioCategory ("TreeView")]
	public class InteractiveTree : Scenario {

		TreeView treeView;

		public override void Setup ()
		{
			Win.Title = this.GetName ();
			Win.Y = 1; // menu
			Win.Height = Dim.Fill (1); // status bar
			Application.Top.LayoutSubviews ();

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_Quit", "", () => Quit()),
				})
				});
			Application.Top.Add (menu);

			treeView = new TreeView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (1),
			};
			treeView.KeyPress += TreeView_KeyPress;

			Win.Add (treeView);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Application.QuitKey, $"{Application.QuitKey} to Quit", () => Quit()),
				new StatusItem(Key.CtrlMask | Key.C, "~^C~ Add Child", () => AddChildNode()),
				new StatusItem(Key.CtrlMask | Key.T, "~^T~ Add Root", () => AddRootNode()),
				new StatusItem(Key.CtrlMask | Key.R, "~^R~ Rename Node", () => RenameNode()),
			});
			Application.Top.Add (statusBar);

		}

		private void TreeView_KeyPress (object sender, KeyEventEventArgs obj)
		{
			if (obj.KeyEvent.Key == Key.DeleteChar) {

				var toDelete = treeView.SelectedObject;

				if (toDelete == null) {
					return;
				}

				obj.Handled = true;

				// if it is a root object remove it
				if (treeView.Objects.Contains (toDelete)) {
					treeView.Remove (toDelete);
				} else {
					var parent = treeView.GetParent (toDelete);

					if (parent == null) {
						MessageBox.ErrorQuery ("Could not delete", $"Parent of '{toDelete}' was unexpectedly null", "Ok");
					} else {
						//update the model
						parent.Children.Remove (toDelete);

						//refresh the tree
						treeView.RefreshObject (parent);
					}
				}
			}
		}

		private void RenameNode ()
		{
			var node = treeView.SelectedObject;

			if (node != null) {
				if (GetText ("Text", "Enter text for node:", node.Text, out string entered)) {
					node.Text = entered;
					treeView.RefreshObject (node);
				}
			}
		}

		private void AddRootNode ()
		{
			if (GetText ("Text", "Enter text for node:", "", out string entered)) {
				treeView.AddObject (new TreeNode (entered));
			}
		}

		private void AddChildNode ()
		{
			var node = treeView.SelectedObject;

			if (node != null) {
				if (GetText ("Text", "Enter text for node:", "", out string entered)) {
					node.Children.Add (new TreeNode (entered));
					treeView.RefreshObject (node);
				}
			}
		}

		private bool GetText (string title, string label, string initialText, out string enteredText)
		{
			bool okPressed = false;

			var ok = new Button ("Ok", is_default: true);
			ok.Clicked += (s,e) => { okPressed = true; Application.RequestStop (); };
			var cancel = new Button ("Cancel");
			cancel.Clicked += (s,e) => { Application.RequestStop (); };
			var d = new Dialog (title, 60, 20, ok, cancel);

			var lbl = new Label () {
				X = 0,
				Y = 1,
				Text = label
			};

			var tf = new TextField () {
				Text = initialText,
				X = 0,
				Y = 2,
				Width = Dim.Fill ()
			};

			d.Add (lbl, tf);
			tf.SetFocus ();

			Application.Run (d);

			enteredText = okPressed ? tf.Text.ToString () : null;
			return okPressed;
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}
	}
}
