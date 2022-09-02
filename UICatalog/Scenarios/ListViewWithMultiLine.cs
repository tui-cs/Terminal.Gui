using NStack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "ListView With MultiLine", Description: "ListView With MultiLine Items")]
	[ScenarioCategory ("Controls"), ScenarioCategory ("ListView")]
	public class ListViewWithMultiLine : Scenario {

		public CheckBox _customRenderCB;
		public CheckBox _allowMarkingCB;
		public CheckBox _allowMultipleCB;
		public ListView _listView;

		public List<Type> _scenarios = Scenario.GetDerivedClasses<Scenario> ().OrderBy (t => Scenario.ScenarioMetadata.GetName (t)).ToList ();

		public override void Setup ()
		{
			_customRenderCB = new CheckBox ("Render with multi-lines") {
				X = 0,
				Y = 0,
				Height = 1,
			};
			Win.Add (_customRenderCB);
			_customRenderCB.Toggled += _customRenderCB_Toggled;

			_allowMarkingCB = new CheckBox ("Allow Marking") {
				X = Pos.Right (_customRenderCB) + 1,
				Y = 0,
				Height = 1,
			};
			Win.Add (_allowMarkingCB);
			_allowMarkingCB.Toggled += AllowMarkingCB_Toggled;

			_allowMultipleCB = new CheckBox ("Allow Multi-Select") {
				X = Pos.Right (_allowMarkingCB) + 1,
				Y = 0,
				Height = 1,
				Visible = _allowMarkingCB.Checked
			};
			Win.Add (_allowMultipleCB);
			_allowMultipleCB.Toggled += AllowMultipleCB_Toggled;

			_listView = new ListView () {
				X = 1,
				Y = 3,
				Height = Dim.Fill (),
				Width = Dim.Fill (1),
				AllowsMarking = false,
				AllowsMultipleSelection = false
			};
			_listView.RowRender += ListView_RowRender;
			Win.Add (_listView);

			var scrollBar = new ScrollBarView (_listView, true);

			int lastVertPos = 0;
			int lastHoriPos = 0;
			int lastTop = 0;
			int lastLeft = 0;

			scrollBar.ChangedPosition += () => {
				var os = GetOffset (scrollBar.Position, _listView.TopItem, _listView.Source.Count - 1, lastVertPos, _listView.ItemHeight);
				_listView.TopItem = os;
				if (_listView.TopItem != os) {
					scrollBar.Position = GetPosition (_listView.TopItem, lastVertPos, scrollBar.Size, lastTop, _listView.ItemHeight);
				}
				lastVertPos = scrollBar.Position;
				lastTop = _listView.TopItem;
				_listView.SetNeedsDisplay ();
			};

			scrollBar.OtherScrollBarView.ChangedPosition += () => {
				var os = GetOffset (scrollBar.OtherScrollBarView.Position, _listView.LeftItem, _listView.Maxlength - 1, lastHoriPos);
				_listView.LeftItem = os;
				if (_listView.LeftItem != os) {
					scrollBar.OtherScrollBarView.Position = GetPosition (_listView.LeftItem, lastHoriPos, scrollBar.OtherScrollBarView.Size, lastLeft);
				}
				lastHoriPos = scrollBar.OtherScrollBarView.Position;
				lastLeft = _listView.LeftItem;
				_listView.SetNeedsDisplay ();
			};

			_listView.DrawContent += (e) => {
				if (_listView.Source == null)
					return;
				scrollBar.Size = GetSize (_listView.Source.Count, _listView.ItemHeight, 1);
				scrollBar.Position = GetPosition (_listView.TopItem, lastVertPos, scrollBar.Size, lastTop, _listView.ItemHeight);
				scrollBar.OtherScrollBarView.Size = GetSize (_listView.Maxlength, 1, -1);
				scrollBar.OtherScrollBarView.Position = GetPosition (_listView.LeftItem, lastHoriPos, scrollBar.OtherScrollBarView.Size, lastLeft);
				scrollBar.Refresh ();
			};

			_listView.SetSource (_scenarios);

			var k = "Keep Content Always In Viewport";
			var keepCheckBox = new CheckBox (k, scrollBar.AutoHideScrollBars) {
				X = Pos.AnchorEnd (k.Length + 3),
				Y = 0,
			};
			keepCheckBox.Toggled += (_) => scrollBar.KeepContentAlwaysInViewport = keepCheckBox.Checked;
			Win.Add (keepCheckBox);
		}

		private int GetOffset (int pos, int lastOffset, int size, int lastPos, int step = 1)
		{
			var stp = GetStep (step);
			var os = pos / stp;
			if (pos == lastPos) {
				return lastOffset;
			} else if (pos > lastPos) {
				return Math.Min (os, size);
			} else {
				return Math.Max (os, 0);
			}
		}

		private int GetPosition (int offset, int lastPos, int size, int lastOffset, int step = 1)
		{
			var stp = GetStep (step);
			var pos = (offset * stp);
			if (offset == lastOffset) {
				return lastPos;
			} else if (offset > lastOffset) {
				return Math.Min (pos, size);
			} else {
				return Math.Max (pos, 0);
			}
		}

		private int GetStep (int step)
		{
			return step < 1 ? 1 : step;
		}

		private int GetSize (int size, int step = 1, int sizeOffset = 0)
		{
			var stp = GetStep (step);
			return (size * stp) + (stp - 1) - sizeOffset;
		}

		private void ListView_RowRender (ListViewRowEventArgs obj)
		{
			if (obj.Row == _listView.SelectedItem) {
				return;
			}
			if (_listView.AllowsMarking && _listView.Source.IsMarked (obj.Row)) {
				obj.RowAttribute = new Attribute (Color.BrightRed, Color.BrightYellow);
				return;
			}
			if (obj.Row % 2 == 0) {
				obj.RowAttribute = new Attribute (Color.BrightGreen, Color.Magenta);
			} else {
				obj.RowAttribute = new Attribute (Color.BrightMagenta, Color.Green);
			}
		}

		private void _customRenderCB_Toggled (bool prev)
		{
			if (prev) {
				_listView.SetSource (_scenarios);
				_listView.ItemHeight = 1;
			} else {
				var list = new List<ScenarioItem> ();
				foreach (var item in _scenarios) {
					var si = new ScenarioItem (ScenarioMetadata.GetName (item), ScenarioMetadata.GetDescription (item));
					list.Add (si);
				}
				_listView.SetSource (list);
				_listView.ItemHeight = 2;
			}
			Win.SetNeedsDisplay ();
		}

		private void AllowMarkingCB_Toggled (bool prev)
		{
			_listView.AllowsMarking = !prev;
			_allowMultipleCB.Visible = _listView.AllowsMarking;
			Win.SetNeedsDisplay ();
		}

		private void AllowMultipleCB_Toggled (bool prev)
		{
			_listView.AllowsMultipleSelection = !prev;
			Win.SetNeedsDisplay ();
		}

		internal class ScenarioItem {
			private string name, description;

			public ScenarioItem (string name, string description)
			{
				this.name = name;
				this.description = description;
			}

			public override string ToString ()
			{
				return name + "\n" + description;
			}
		}

		//internal class ScenarioList {
		//	private List<Type> scenarios;

		//	public ScenarioList (List<Type> itemList) => Scenarios = itemList;

		//	public List<Type> Scenarios {
		//		get => scenarios;
		//		set {
		//			if (value != null) {
		//				scenarios = value;
		//			}
		//		}
		//	}

		//	public bool Detailed { get; set; } = false;

		//	//public IList GetMultiLineScenarios ()
		//	//{
		//	//	var list = new List<string> ();
		//	//	foreach (var item in scenarios) {
		//	//		var s = ScenarioMetadata.GetName (item) + "\n" + ScenarioMetadata.GetDescription (item);
		//	//		list.Add (s);
		//	//	}
		//	//	return list;
		//	//}
		//}

		// This is basically the same implementation used by the UICatalog main window
		//internal class ScenarioListDataSource : IListDataSource {
		//	int _nameColumnWidth = 30;
		//	private List<Type> scenarios;
		//	BitArray marks;
		//	int count, len;

		//	public List<Type> Scenarios {
		//		get => scenarios;
		//		set {
		//			if (value != null) {
		//				count = value.Count;
		//				marks = new BitArray (count);
		//				scenarios = value;
		//				len = GetMaxLengthItem ();
		//			}
		//		}
		//	}
		//	public bool IsMarked (int item)
		//	{
		//		if (item >= 0 && item < count)
		//			return marks [item];
		//		return false;
		//	}

		//	public int Count => Scenarios != null ? Scenarios.Count : 0;

		//	public int Length => len;

		//	public ScenarioListDataSource (List<Type> itemList) => Scenarios = itemList;

		//	public void Render (ListView container, ConsoleDriver driver, bool selected, int item, int col, int line, int width, int start = 0)
		//	{
		//		container.Move (col, line);
		//		// Equivalent to an interpolated string like $"{Scenarios[item].Name, -widtestname}"; if such a thing were possible
		//		var s = String.Format (String.Format ("{{0,{0}}}", -_nameColumnWidth), Scenario.ScenarioMetadata.GetName (Scenarios [item]));
		//		RenderUstr (driver, $"{s}  {Scenario.ScenarioMetadata.GetDescription (Scenarios [item])}", col, line, width, start);
		//	}

		//	public void SetMark (int item, bool value)
		//	{
		//		if (item >= 0 && item < count)
		//			marks [item] = value;
		//	}

		//	int GetMaxLengthItem ()
		//	{
		//		if (scenarios?.Count == 0) {
		//			return 0;
		//		}

		//		int maxLength = 0;
		//		for (int i = 0; i < scenarios.Count; i++) {
		//			var s = String.Format (String.Format ("{{0,{0}}}", -_nameColumnWidth), Scenario.ScenarioMetadata.GetName (Scenarios [i]));
		//			var sc = $"{s}  {Scenario.ScenarioMetadata.GetDescription (Scenarios [i])}";
		//			var l = sc.Length;
		//			if (l > maxLength) {
		//				maxLength = l;
		//			}
		//		}

		//		return maxLength;
		//	}

		//	// A slightly adapted method from: https://github.com/gui-cs/Terminal.Gui/blob/fc1faba7452ccbdf49028ac49f0c9f0f42bbae91/Terminal.Gui/Views/ListView.cs#L433-L461
		//	private void RenderUstr (ConsoleDriver driver, ustring ustr, int col, int line, int width, int start = 0)
		//	{
		//		int used = 0;
		//		int index = start;
		//		while (index < ustr.Length) {
		//			(var rune, var size) = Utf8.DecodeRune (ustr, index, index - ustr.Length);
		//			var count = Rune.ColumnWidth (rune);
		//			if (used + count >= width) break;
		//			driver.AddRune (rune);
		//			used += count;
		//			index += size;
		//		}

		//		while (used < width) {
		//			driver.AddRune (' ');
		//			used++;
		//		}
		//	}

		//	public IList ToList ()
		//	{
		//		return Scenarios;
		//	}
		//}
	}
}