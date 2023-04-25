using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
		private MenuItem miPlusMinus;
		private MenuItem miArrowSymbols;
		private MenuItem miNoSymbols;
		private MenuItem miColoredSymbols;
		private MenuItem miInvertSymbols;
		private MenuItem miUnicodeSymbols;
		private MenuItem miFullPaths;
		private MenuItem miLeaveLastRow;
		private MenuItem miHighlightModelTextOnly;
		private MenuItem miCustomColors;
		private MenuItem miCursor;
		private MenuItem miMultiSelect;

		private DetailsFrame detailsFrame;

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
					miFullPaths = new MenuItem ("_Full Paths", "", () => SetFullName()){Checked = false, CheckType = MenuItemCheckStyle.Checked},
					miMultiSelect = new MenuItem ("_Multi Select", "", () => SetMultiSelect()){Checked = true, CheckType = MenuItemCheckStyle.Checked},
				}),
				new MenuBarItem ("_Style", new MenuItem [] {
					miShowLines = new MenuItem ("_Show Lines", "", () => ShowLines()){
					Checked = true, CheckType = MenuItemCheckStyle.Checked
						},
					null /*separator*/,
					miPlusMinus = new MenuItem ("_Plus Minus Symbols", "+ -", () => SetExpandableSymbols('+','-')){Checked = true, CheckType = MenuItemCheckStyle.Radio},
					miArrowSymbols = new MenuItem ("_Arrow Symbols", "> v", () => SetExpandableSymbols('>','v')){Checked = false, CheckType = MenuItemCheckStyle.Radio},
					miNoSymbols = new MenuItem ("_No Symbols", "", () => SetExpandableSymbols(null,null)){Checked = false, CheckType = MenuItemCheckStyle.Radio},
					miUnicodeSymbols = new MenuItem ("_Unicode", "ஹ ﷽", () => SetExpandableSymbols('ஹ','﷽')){Checked = false, CheckType = MenuItemCheckStyle.Radio},
					null /*separator*/,
					miColoredSymbols = new MenuItem ("_Colored Symbols", "", () => ShowColoredExpandableSymbols()){Checked = false, CheckType = MenuItemCheckStyle.Checked},
					miInvertSymbols = new MenuItem ("_Invert Symbols", "", () => InvertExpandableSymbols()){Checked = false, CheckType = MenuItemCheckStyle.Checked},
					null /*separator*/,
					miLeaveLastRow = new MenuItem ("_Leave Last Row", "", () => SetLeaveLastRow()){Checked = true, CheckType = MenuItemCheckStyle.Checked},
					miHighlightModelTextOnly = new MenuItem ("_Highlight Model Text Only", "", () => SetCheckHighlightModelTextOnly()){Checked = false, CheckType = MenuItemCheckStyle.Checked},
					null /*separator*/,
					miCustomColors = new MenuItem ("C_ustom Colors Hidden Files", "Yellow/Red", () => SetCustomColors()){Checked = false, CheckType = MenuItemCheckStyle.Checked},
					null /*separator*/,
					miCursor = new MenuItem ("Curs_or (MultiSelect only)", "", () => SetCursor()){Checked = false, CheckType = MenuItemCheckStyle.Checked},
				}),
			});
			Application.Top.Add (menu);

			treeViewFiles = new TreeView<FileSystemInfo> () {
				X = 0,
				Y = 0,
				Width = Dim.Percent (50),
				Height = Dim.Fill (),
			};

			detailsFrame = new DetailsFrame () {
				X = Pos.Right (treeViewFiles),
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};

			Win.Add (detailsFrame);
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
			detailsFrame.FileInfo = fileSystemInfo;
		}

		private void SetupScrollBar ()
		{
			// When using scroll bar leave the last row of the control free (for over-rendering with scroll bar)
			treeViewFiles.Style.LeaveLastRow = true;

			var _scrollBar = new ScrollBarView (treeViewFiles, true);

			_scrollBar.ChangedPosition += (s,e) => {
				treeViewFiles.ScrollOffsetVertical = _scrollBar.Position;
				if (treeViewFiles.ScrollOffsetVertical != _scrollBar.Position) {
					_scrollBar.Position = treeViewFiles.ScrollOffsetVertical;
				}
				treeViewFiles.SetNeedsDisplay ();
			};

			_scrollBar.OtherScrollBarView.ChangedPosition += (s,e) => {
				treeViewFiles.ScrollOffsetHorizontal = _scrollBar.OtherScrollBarView.Position;
				if (treeViewFiles.ScrollOffsetHorizontal != _scrollBar.OtherScrollBarView.Position) {
					_scrollBar.OtherScrollBarView.Position = treeViewFiles.ScrollOffsetHorizontal;
				}
				treeViewFiles.SetNeedsDisplay ();
			};

			treeViewFiles.DrawContent += (s,e) => {
				_scrollBar.Size = treeViewFiles.ContentHeight;
				_scrollBar.Position = treeViewFiles.ScrollOffsetVertical;
				_scrollBar.OtherScrollBarView.Size = treeViewFiles.GetContentWidth (true);
				_scrollBar.OtherScrollBarView.Position = treeViewFiles.ScrollOffsetHorizontal;
				_scrollBar.Refresh ();
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

		private void SetExpandableSymbols (Rune? expand, Rune? collapse)
		{
			miPlusMinus.Checked = expand == '+';
			miArrowSymbols.Checked = expand == '>';
			miNoSymbols.Checked = expand == null;
			miUnicodeSymbols.Checked = expand == 'ஹ';

			treeViewFiles.Style.ExpandableSymbol = expand;
			treeViewFiles.Style.CollapseableSymbol = collapse;
			treeViewFiles.SetNeedsDisplay ();
		}
		private void ShowColoredExpandableSymbols ()
		{
			miColoredSymbols.Checked = !miColoredSymbols.Checked;

			treeViewFiles.Style.ColorExpandSymbol = (bool)miColoredSymbols.Checked;
			treeViewFiles.SetNeedsDisplay ();
		}
		private void InvertExpandableSymbols ()
		{
			miInvertSymbols.Checked = !miInvertSymbols.Checked;

			treeViewFiles.Style.InvertExpandSymbolColors = (bool)miInvertSymbols.Checked;
			treeViewFiles.SetNeedsDisplay ();
		}

		private void SetFullName ()
		{
			miFullPaths.Checked = !miFullPaths.Checked;

			if (miFullPaths.Checked == true) {
				treeViewFiles.AspectGetter = (f) => f.FullName;
			} else {
				treeViewFiles.AspectGetter = (f) => f.Name;
			}
			treeViewFiles.SetNeedsDisplay ();
		}

		private void SetLeaveLastRow ()
		{
			miLeaveLastRow.Checked = !miLeaveLastRow.Checked;
			treeViewFiles.Style.LeaveLastRow = (bool)miLeaveLastRow.Checked;
		}
		private void SetCursor ()
		{
			miCursor.Checked = !miCursor.Checked;
			treeViewFiles.DesiredCursorVisibility = miCursor.Checked == true ? CursorVisibility.Default : CursorVisibility.Invisible;
		}
		private void SetMultiSelect ()
		{
			miMultiSelect.Checked = !miMultiSelect.Checked;
			treeViewFiles.MultiSelect = (bool)miMultiSelect.Checked;
		}

		private void SetCustomColors ()
		{
			var hidden = new ColorScheme {
				Focus = new Terminal.Gui.Attribute (Color.BrightRed, treeViewFiles.ColorScheme.Focus.Background),
				Normal = new Terminal.Gui.Attribute (Color.BrightYellow, treeViewFiles.ColorScheme.Normal.Background),
			};

			miCustomColors.Checked = !miCustomColors.Checked;

			if (miCustomColors.Checked == true) {
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
			miHighlightModelTextOnly.Checked = treeViewFiles.Style.HighlightModelTextOnly;
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
