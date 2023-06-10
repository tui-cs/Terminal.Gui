using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "File System Explorer", Description: "Hierarchical file system explorer demonstrating TreeView.")]
	[ScenarioCategory ("Controls"), ScenarioCategory ("TreeView"), ScenarioCategory ("Files and IO")]
	public class TreeViewFileSystem : Scenario {

		/// <summary>
		/// A tree view where nodes are files and folders
		/// </summary>
		TreeView<FileSystemInfo> treeViewFiles;

		MenuItem miShowLines;
		private MenuItem _miPlusMinus;
		private MenuItem _miArrowSymbols;
		private MenuItem _miNoSymbols;
		private MenuItem _miColoredSymbols;
		private MenuItem _miInvertSymbols;
		private MenuItem _miUnicodeSymbols;
		private MenuItem _miFullPaths;
		private MenuItem _miLeaveLastRow;
		private MenuItem _miHighlightModelTextOnly;
		private MenuItem _miCustomColors;
		private MenuItem _miCursor;
		private MenuItem _miMultiSelect;

		private DetailsFrame _detailsFrame;

		public override void Setup ()
		{
			Win.Title = this.GetName ();
			Win.Y = 1; // menu
			Win.Height = Dim.Fill ();
			Application.Top.LayoutSubviews ();

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_Quit", $"{Application.QuitKey}", () => Quit()),
				}),
				new MenuBarItem ("_View", new MenuItem [] {
					_miFullPaths = new MenuItem ("_Full Paths", "", () => SetFullName()){Checked = false, CheckType = MenuItemCheckStyle.Checked},
					_miMultiSelect = new MenuItem ("_Multi Select", "", () => SetMultiSelect()){Checked = true, CheckType = MenuItemCheckStyle.Checked},
				}),
				new MenuBarItem ("_Style", new MenuItem [] {
					miShowLines = new MenuItem ("_Show Lines", "", () => ShowLines()){
					Checked = true, CheckType = MenuItemCheckStyle.Checked
						},
					null /*separator*/,
					_miPlusMinus = new MenuItem ("_Plus Minus Symbols", "+ -", () => SetExpandableSymbols((Rune)'+',(Rune)'-')){Checked = true, CheckType = MenuItemCheckStyle.Radio},
					_miArrowSymbols = new MenuItem ("_Arrow Symbols", "> v", () => SetExpandableSymbols((Rune)'>',(Rune)'v')){Checked = false, CheckType = MenuItemCheckStyle.Radio},
					_miNoSymbols = new MenuItem ("_No Symbols", "", () => SetExpandableSymbols(default,null)){Checked = false, CheckType = MenuItemCheckStyle.Radio},
					_miUnicodeSymbols = new MenuItem ("_Unicode", "ஹ ﷽", () => SetExpandableSymbols((Rune)'ஹ',(Rune)'﷽')){Checked = false, CheckType = MenuItemCheckStyle.Radio},
					null /*separator*/,
					_miColoredSymbols = new MenuItem ("_Colored Symbols", "", () => ShowColoredExpandableSymbols()){Checked = false, CheckType = MenuItemCheckStyle.Checked},
					_miInvertSymbols = new MenuItem ("_Invert Symbols", "", () => InvertExpandableSymbols()){Checked = false, CheckType = MenuItemCheckStyle.Checked},
					null /*separator*/,
					_miLeaveLastRow = new MenuItem ("_Leave Last Row", "", () => SetLeaveLastRow()){Checked = true, CheckType = MenuItemCheckStyle.Checked},
					_miHighlightModelTextOnly = new MenuItem ("_Highlight Model Text Only", "", () => SetCheckHighlightModelTextOnly()){Checked = false, CheckType = MenuItemCheckStyle.Checked},
					null /*separator*/,
					_miCustomColors = new MenuItem ("C_ustom Colors Hidden Files", "Yellow/Red", () => SetCustomColors()){Checked = false, CheckType = MenuItemCheckStyle.Checked},
					null /*separator*/,
					_miCursor = new MenuItem ("Curs_or (MultiSelect only)", "", () => SetCursor()){Checked = false, CheckType = MenuItemCheckStyle.Checked},
				}),
			});
			Application.Top.Add (menu);

			treeViewFiles = new TreeView<FileSystemInfo> () {
				X = 0,
				Y = 0,
				Width = Dim.Percent (50),
				Height = Dim.Fill (),
			};

			_detailsFrame = new DetailsFrame () {
				X = Pos.Right (treeViewFiles),
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};

			Win.Add (_detailsFrame);
			treeViewFiles.MouseClick += TreeViewFiles_MouseClick;
			treeViewFiles.KeyPress += TreeViewFiles_KeyPress;
			treeViewFiles.SelectionChanged += TreeViewFiles_SelectionChanged;

			SetupFileTree ();

			Win.Add (treeViewFiles);
			treeViewFiles.GoToFirst ();
			treeViewFiles.Expand ();

			SetupScrollBar ();

			treeViewFiles.SetFocus ();

		}

		private void TreeViewFiles_SelectionChanged (object sender, SelectionChangedEventArgs<FileSystemInfo> e)
		{
			ShowPropertiesOf (e.NewValue);
		}

		private void TreeViewFiles_KeyPress (object sender, KeyEventEventArgs obj)
		{
			if (obj.KeyEvent.Key == (Key.R | Key.CtrlMask)) {

				var selected = treeViewFiles.SelectedObject;

				// nothing is selected
				if (selected == null)
					return;

				var location = treeViewFiles.GetObjectRow (selected);

				//selected object is offscreen or somehow not found
				if (location == null || location < 0 || location > treeViewFiles.Frame.Height)
					return;

				ShowContextMenu (new Point (
					5 + treeViewFiles.Frame.X,
					location.Value + treeViewFiles.Frame.Y + 2),
					selected);
			}
		}

		private void TreeViewFiles_MouseClick (object sender, MouseEventEventArgs obj)
		{
			// if user right clicks
			if (obj.MouseEvent.Flags.HasFlag (MouseFlags.Button3Clicked)) {

				var rightClicked = treeViewFiles.GetObjectOnRow (obj.MouseEvent.Y);

				// nothing was clicked
				if (rightClicked == null)
					return;

				ShowContextMenu (new Point (
					obj.MouseEvent.X + treeViewFiles.Frame.X,
					obj.MouseEvent.Y + treeViewFiles.Frame.Y + 2),
					rightClicked);
			}
		}

		private void ShowContextMenu (Point screenPoint, FileSystemInfo forObject)
		{
			var menu = new ContextMenu ();
			menu.Position = screenPoint;

			menu.MenuItems = new MenuBarItem (new [] { new MenuItem ("Properties", null, () => ShowPropertiesOf (forObject)) });

			Application.MainLoop.Invoke (menu.Show);
		}

		class DetailsFrame : FrameView {
			private FileSystemInfo fileInfo;

			public DetailsFrame ()
			{
				Title = "Details";
				Visible = true;
				CanFocus = true;
			}

			public FileSystemInfo FileInfo {
				get => fileInfo; set {
					fileInfo = value;
					System.Text.StringBuilder sb = null;
					if (fileInfo is FileInfo f) {
						Title = $"File: {f.Name}";
						sb = new System.Text.StringBuilder ();
						sb.AppendLine ($"Path:\n {f.FullName}\n");
						sb.AppendLine ($"Size:\n {f.Length:N0} bytes\n");
						sb.AppendLine ($"Modified:\n {f.LastWriteTime}\n");
						sb.AppendLine ($"Created:\n {f.CreationTime}");
					}

					if (fileInfo is DirectoryInfo dir) {
						Title = $"Directory: {dir.Name}";
						sb = new System.Text.StringBuilder ();
						sb.AppendLine ($"Path:\n {dir?.FullName}\n");
						sb.AppendLine ($"Modified:\n {dir.LastWriteTime}\n");
						sb.AppendLine ($"Created:\n {dir.CreationTime}\n");
					}
					Text = sb.ToString ();
				}
			}
		}

		private void ShowPropertiesOf (FileSystemInfo fileSystemInfo)
		{
			_detailsFrame.FileInfo = fileSystemInfo;
		}

		private void SetupScrollBar ()
		{
			// When using scroll bar leave the last row of the control free (for over-rendering with scroll bar)
			treeViewFiles.Style.LeaveLastRow = true;

			var scrollBar = new ScrollBarView (treeViewFiles, true);

			scrollBar.ChangedPosition += (s,e) => {
				treeViewFiles.ScrollOffsetVertical = scrollBar.Position;
				if (treeViewFiles.ScrollOffsetVertical != scrollBar.Position) {
					scrollBar.Position = treeViewFiles.ScrollOffsetVertical;
				}
				treeViewFiles.SetNeedsDisplay ();
			};

			scrollBar.OtherScrollBarView.ChangedPosition += (s,e) => {
				treeViewFiles.ScrollOffsetHorizontal = scrollBar.OtherScrollBarView.Position;
				if (treeViewFiles.ScrollOffsetHorizontal != scrollBar.OtherScrollBarView.Position) {
					scrollBar.OtherScrollBarView.Position = treeViewFiles.ScrollOffsetHorizontal;
				}
				treeViewFiles.SetNeedsDisplay ();
			};

			treeViewFiles.DrawContent += (s,e) => {
				scrollBar.Size = treeViewFiles.ContentHeight;
				scrollBar.Position = treeViewFiles.ScrollOffsetVertical;
				scrollBar.OtherScrollBarView.Size = treeViewFiles.GetContentWidth (true);
				scrollBar.OtherScrollBarView.Position = treeViewFiles.ScrollOffsetHorizontal;
				scrollBar.Refresh ();
			};
		}

		private void SetupFileTree ()
		{

			// setup delegates
			treeViewFiles.TreeBuilder = new DelegateTreeBuilder<FileSystemInfo> (

				// Determines how to compute children of any given branch
				GetChildren,
				// As a shortcut to enumerating half the file system, tell tree that all directories are expandable (even if they turn out to be empty later on)				
				(o) => o is DirectoryInfo
			);

			// Determines how to represent objects as strings on the screen
			treeViewFiles.AspectGetter = FileSystemAspectGetter;

			treeViewFiles.AddObjects (DriveInfo.GetDrives ().Select (d => d.RootDirectory));
		}

		private void ShowLines ()
		{
			miShowLines.Checked = !miShowLines.Checked;

			treeViewFiles.Style.ShowBranchLines = (bool)miShowLines.Checked;
			treeViewFiles.SetNeedsDisplay ();
		}

		private void SetExpandableSymbols (Rune expand, Rune? collapse)
		{
			_miPlusMinus.Checked = expand.Value == '+';
			_miArrowSymbols.Checked = expand.Value == '>';
			_miNoSymbols.Checked = expand.Value == default;
			_miUnicodeSymbols.Checked = expand.Value == 'ஹ';

			treeViewFiles.Style.ExpandableSymbol = expand;
			treeViewFiles.Style.CollapseableSymbol = collapse;
			treeViewFiles.SetNeedsDisplay ();
		}
		private void ShowColoredExpandableSymbols ()
		{
			_miColoredSymbols.Checked = !_miColoredSymbols.Checked;

			treeViewFiles.Style.ColorExpandSymbol = (bool)_miColoredSymbols.Checked;
			treeViewFiles.SetNeedsDisplay ();
		}
		private void InvertExpandableSymbols ()
		{
			_miInvertSymbols.Checked = !_miInvertSymbols.Checked;

			treeViewFiles.Style.InvertExpandSymbolColors = (bool)_miInvertSymbols.Checked;
			treeViewFiles.SetNeedsDisplay ();
		}

		private void SetFullName ()
		{
			_miFullPaths.Checked = !_miFullPaths.Checked;

			if (_miFullPaths.Checked == true) {
				treeViewFiles.AspectGetter = (f) => f.FullName;
			} else {
				treeViewFiles.AspectGetter = (f) => f.Name;
			}
			treeViewFiles.SetNeedsDisplay ();
		}

		private void SetLeaveLastRow ()
		{
			_miLeaveLastRow.Checked = !_miLeaveLastRow.Checked;
			treeViewFiles.Style.LeaveLastRow = (bool)_miLeaveLastRow.Checked;
		}
		private void SetCursor ()
		{
			_miCursor.Checked = !_miCursor.Checked;
			treeViewFiles.DesiredCursorVisibility = _miCursor.Checked == true ? CursorVisibility.Default : CursorVisibility.Invisible;
		}
		private void SetMultiSelect ()
		{
			_miMultiSelect.Checked = !_miMultiSelect.Checked;
			treeViewFiles.MultiSelect = (bool)_miMultiSelect.Checked;
		}

		private void SetCustomColors ()
		{
			var hidden = new ColorScheme {
				Focus = new Terminal.Gui.Attribute (Color.BrightRed, treeViewFiles.ColorScheme.Focus.Background),
				Normal = new Terminal.Gui.Attribute (Color.BrightYellow, treeViewFiles.ColorScheme.Normal.Background),
			};

			_miCustomColors.Checked = !_miCustomColors.Checked;

			if (_miCustomColors.Checked == true) {
				treeViewFiles.ColorGetter = (m) => {
					if (m is DirectoryInfo && m.Attributes.HasFlag (FileAttributes.Hidden)) return hidden;
					if (m is FileInfo && m.Attributes.HasFlag (FileAttributes.Hidden)) return hidden;
					return null;
				};
			} else {
				treeViewFiles.ColorGetter = null;
			}
			treeViewFiles.SetNeedsDisplay ();
		}

		private void SetCheckHighlightModelTextOnly ()
		{
			treeViewFiles.Style.HighlightModelTextOnly = !treeViewFiles.Style.HighlightModelTextOnly;
			_miHighlightModelTextOnly.Checked = treeViewFiles.Style.HighlightModelTextOnly;
			treeViewFiles.SetNeedsDisplay ();
		}

		private IEnumerable<FileSystemInfo> GetChildren (FileSystemInfo model)
		{
			// If it is a directory it's children are all contained files and dirs
			if (model is DirectoryInfo d) {
				try {
					return d.GetFileSystemInfos ()
						//show directories first
						.OrderBy (a => a is DirectoryInfo ? 0 : 1)
						.ThenBy (b => b.Name);
				} catch (SystemException) {

					// Access violation or other error getting the file list for directory
					return Enumerable.Empty<FileSystemInfo> ();
				}
			}

			return Enumerable.Empty<FileSystemInfo> (); ;
		}
		private string FileSystemAspectGetter (FileSystemInfo model)
		{
			if (model is DirectoryInfo d) {
				return d.Name;
			}
			if (model is FileInfo f) {
				return f.Name;
			}

			return model.ToString ();
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}
	}
}
