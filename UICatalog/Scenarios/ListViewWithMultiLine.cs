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

		private ComboBox _customRenderCB;
		private CheckBox _allowMarkingCB;
		private CheckBox _allowMultipleCB;
		private ListView _listView;
		private CheckBox _showColHeaders;
		private TextField _txtItemHeight;
		private ComboBox _headersAlignCB;
		private ComboBox _membersAlignCB;

		private List<Scenario> _scenarios;

		public override void Setup ()
		{
			_scenarios = Scenario.GetScenarios ();

			var listRender = new List<string> {
				"Render with single-lines",
				"Render with multi-lines",
				"Render with multi-columns",
				"Render string multi-columns",
				"Render with null lines",
				"Render null lines with header"};

			_customRenderCB = new ComboBox (listRender) {
				X = 0,
				Y = 0,
				Height = listRender.Count + 1,
				Width = 30,
				ReadOnly = true,
				HideDropdownListOnClick = true,
				SelectedItem = 0
			};
			_customRenderCB.OpenSelectedItem += _customRenderCB_OpenSelectedItem;
			Win.Add (_customRenderCB);

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
				Visible = (bool)_allowMarkingCB.Checked
			};
			Win.Add (_allowMultipleCB);
			_allowMultipleCB.Toggled += AllowMultipleCB_Toggled;

			_listView = new ListView () {
				X = 1,
				Y = 4,
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

			scrollBar.ChangedPosition += (s, e) => {
				var os = GetOffset (scrollBar.Position, _listView.TopItem, _listView.Source.Count - 1, lastVertPos, _listView.ItemHeight);
				_listView.TopItem = os;
				if (_listView.TopItem != os) {
					scrollBar.Position = GetPosition (_listView.TopItem, lastVertPos, scrollBar.Size, lastTop, _listView.ItemHeight);
				}
				lastVertPos = scrollBar.Position;
				lastTop = _listView.TopItem;
				_listView.SetNeedsDisplay ();
			};

			scrollBar.OtherScrollBarView.ChangedPosition += (s, e) => {
				var os = GetOffset (scrollBar.OtherScrollBarView.Position, _listView.LeftItem, _listView.Maxlength - 1, lastHoriPos);
				_listView.LeftItem = os;
				if (_listView.LeftItem != os) {
					scrollBar.OtherScrollBarView.Position = GetPosition (_listView.LeftItem, lastHoriPos, scrollBar.OtherScrollBarView.Size, lastLeft);
				}
				lastHoriPos = scrollBar.OtherScrollBarView.Position;
				lastLeft = _listView.LeftItem;
				_listView.SetNeedsDisplay ();
			};

			_listView.DrawContent += (s, e) => {
				if (_listView.Source == null)
					return;
				//scrollBar.Size = GetSize (_listView.Source.Count, _listView.ItemHeight, _listView.ShowColumnHeaders ? -1 : scrollBar.OtherScrollBarView.ShowScrollIndicator ? 1 : 0);
				scrollBar.Size = GetSize (_listView.Source.Count, _listView.ItemHeight);
				//if (scrollBar.OtherScrollBarView.ShowScrollIndicator && _listView.SelectedItem > _listView.TopItem + _listView.VisibleItems - 1) {
				//	_listView.TopItem++;
				//}
				if (scrollBar.OtherScrollBarView.ShowScrollIndicator && _listView.SelectedItem > 0 &&
					_listView.SelectedItem % (_listView.TopItem + _listView.VisibleItems - 1) == 0) {

					_listView.TopItem++;
				}
				scrollBar.Position = GetPosition (_listView.TopItem, lastVertPos, scrollBar.Size, lastTop, _listView.ItemHeight);
				//scrollBar.OtherScrollBarView.Size = GetSize (_listView.Maxlength, 1, scrollBar.ShowScrollIndicator ? 1 : 0);
				scrollBar.OtherScrollBarView.Size = GetSize (_listView.Maxlength);
				//scrollBar.OtherScrollBarView.Size = _listView.Maxlength;
				scrollBar.OtherScrollBarView.Position = GetPosition (_listView.LeftItem, lastHoriPos, scrollBar.OtherScrollBarView.Size, lastLeft);
				scrollBar.Refresh ();
			};
			_listView.ScrollBarType = ScrollBarView.ScrollBarType.Both;

			_listView.Source = new ListWrapper (_scenarios);

			var k = "Keep Content Always In Viewport";
			var keepCheckBox = new CheckBox (k, scrollBar.AutoHideScrollBars) {
				X = Pos.AnchorEnd (k.Length + 3),
				Y = 0,
			};
			keepCheckBox.Toggled += (s, _) => scrollBar.KeepContentAlwaysInViewport = (bool)keepCheckBox.Checked;
			Win.Add (keepCheckBox);
			keepCheckBox.SuperView.SendSubviewToBack (keepCheckBox);

			_customRenderCB.Collapsed += (s, e) => keepCheckBox.SuperView.SendSubviewToBack (keepCheckBox);

			_showColHeaders = new CheckBox ("Show Column Headers", _listView.ShowColumnHeaders) {
				X = Pos.X (_allowMarkingCB),
				Y = Pos.Bottom (_allowMultipleCB)
			};
			_showColHeaders.Toggled += (s, _) => _listView.ShowColumnHeaders = (bool)_showColHeaders.Checked;
			Win.Add (_showColHeaders);

			var label = new Label ("Item Height:") {
				X = Pos.Right (_showColHeaders) + 4,
				Y = Pos.Top (_showColHeaders)
			};
			Win.Add (label);

			_txtItemHeight = new TextField ("1") {
				X = Pos.Right (label),
				Y = Pos.Top (label),
				Width = 3
			};
			_txtItemHeight.TextChanging += (s, e) => {
				if (!int.TryParse (e.NewText.ToString (), out _)) {
					e.Cancel = true;
				}
			};
			_txtItemHeight.TextChanged += (s, _) => {
				try {
					_listView.ItemHeight = int.Parse (_txtItemHeight.Text.ToString ());
				} catch (Exception ex) {
					MessageBox.ErrorQuery ("Error", ex.Message, "Ok");
					_txtItemHeight.Text = "1";
				}
			};
			Win.Add (_txtItemHeight);

			label = new Label ("Headers Alignments:") {
				X = Pos.X (_showColHeaders),
				Y = Pos.Bottom (_showColHeaders)
			};
			Win.Add (label);

			var alignments = Enum.GetNames<TextAlignment> ();

			_headersAlignCB = new ComboBox (alignments) {
				X = Pos.Right (label),
				Y = Pos.Top (label),
				Height = alignments.Length + 1,
				Width = 10,
				ReadOnly = true,
				HideDropdownListOnClick = true,
				SelectedItem = 0
			};
			_headersAlignCB.OpenSelectedItem += (s, e) => {
				if (_listView.ColumnHeaders != null) {
					foreach (var ch in _listView.ColumnHeaders) {
						ch.HeaderAlignment = (TextAlignment)e.Item;
					}
				} else {
					_listView.HeadersAlignment = (TextAlignment)e.Item;
				}
			};
			Win.Add (_headersAlignCB);

			label = new Label ("Members Alignments:") {
				X = Pos.Right (_headersAlignCB) + 4,
				Y = Pos.Top (_headersAlignCB)
			};
			Win.Add (label);

			_membersAlignCB = new ComboBox (alignments) {
				X = Pos.Right (label),
				Y = Pos.Top (label),
				Height = alignments.Length + 1,
				Width = 10,
				ReadOnly = true,
				HideDropdownListOnClick = true,
				SelectedItem = 0
			};
			_membersAlignCB.OpenSelectedItem += (s, e) => {
				if (_listView.ColumnHeaders != null) {
					foreach (var ch in _listView.ColumnHeaders) {
						ch.MemberAlignment = (TextAlignment)e.Item;
					}
				} else {
					_listView.ItemsAlignment = (TextAlignment)e.Item;
				}
			};
			Win.Add (_membersAlignCB);
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
			return (size * stp) + (stp - 1) + sizeOffset;
		}

		private void ListView_RowRender (object sender, ListViewRowEventArgs e)
		{
			if (e.Row == _listView.SelectedItem) {
				return;
			}
			if (_listView.AllowsMarking && _listView.Source.IsMarked (e.Row)) {
				e.RowAttribute = new Attribute (Color.BrightRed, Color.BrightYellow);
				return;
			}
			if (e.Row % 2 == 0) {
				e.RowAttribute = new Attribute (Color.BrightGreen, Color.Magenta);
			} else {
				e.RowAttribute = new Attribute (Color.BrightMagenta, Color.Green);
			}
		}

		private void _customRenderCB_OpenSelectedItem (object sender, ListViewItemEventArgs e)
		{
			switch (e.Item) {
			case 0:
				_listView.Source = new ListWrapper (_scenarios);
				_txtItemHeight.Text = "1";
				_listView.ShowColumnHeaders = (bool)_showColHeaders.Checked;
				_listView.ColumnHeaders = null;
				_headersAlignCB.SelectedItem = (int)_listView.HeadersAlignment;
				_membersAlignCB.SelectedItem = (int)_listView.ItemsAlignment;
				break;
			case 1:
				var listScenarioItem = new List<ScenarioItem> ();
				foreach (var item in _scenarios) {
					var si = new ScenarioItem (item.GetName (), item.GetDescription ());
					listScenarioItem.Add (si);
				}
				_listView.Source = new ListWrapper (listScenarioItem);
				_txtItemHeight.Text = "2";
				_listView.ShowColumnHeaders = (bool)_showColHeaders.Checked;
				_listView.ColumnHeaders = new List<ListView.ColumnHeader> (){
					new ListView.ColumnHeader () {
						MemberName = "Name",
						MemberType = typeof(string)
					},
					new ListView.ColumnHeader () {
						MemberName = "Description",
						MemberType = typeof(string)
					}
				};
				_headersAlignCB.SelectedItem = (int)_listView.ColumnHeaders [0].HeaderAlignment;
				_membersAlignCB.SelectedItem = (int)_listView.ColumnHeaders [0].MemberAlignment;
				break;
			case 2:
				var listNumericItem = new List<NumericItem> ();
				for (int i = 0; i < 100; i++) {
					var ni = new NumericItem ($"Item{i}", i, i / 2f, i / 3d, i / 4, i / 5);
					listNumericItem.Add (ni);
				}
				_listView.Source = new ListWrapper (listNumericItem);
				_txtItemHeight.Text = "1";
				_listView.ShowColumnHeaders = (bool)_showColHeaders.Checked;
				_listView.ColumnHeaders = new List<ListView.ColumnHeader> (){
					new ListView.ColumnHeader () {
						MemberName = "Item",
						MemberType = typeof(string)
					},
					new ListView.ColumnHeader () {
						MemberName = "IntValue",
						MemberType = typeof(int),
						MemberAlignment = TextAlignment.Right
					},
					new ListView.ColumnHeader () {
						MemberName = "FloatValue",
						MemberType = typeof(float),
						MemberFormat = "{0:F3}",
						MemberAlignment = TextAlignment.Right
					},
					new ListView.ColumnHeader () {
						MemberName = "DoubleValue",
						MemberType = typeof(double),
						MemberFormat = "{0:N4}",
						MemberAlignment = TextAlignment.Right
					},
					new ListView.ColumnHeader () {
						MemberName = "DecimalValue",
						MemberType = typeof(decimal),
						MemberFormat = "{0:N2}",
						MemberAlignment = TextAlignment.Right
					},
					new ListView.ColumnHeader () {
						MemberName = "CurrencyValue",
						MemberType = typeof(decimal),
						MemberFormat = "{0:C2}",
						MemberAlignment = TextAlignment.Right
					}
				};
				_headersAlignCB.SelectedItem = (int)_listView.ColumnHeaders [0].HeaderAlignment;
				_membersAlignCB.SelectedItem = (int)_listView.ColumnHeaders [0].MemberAlignment;
				break;
			case 3:
				var listStr = new List<string> ();
				var idx = 0;
				foreach (var item in _scenarios) {
					listStr.Add ($"{idx}\n{item.GetName ()}\n{item.GetDescription ()}");
					idx++;
				}
				_listView.Source = new ListWrapper (listStr);
				_txtItemHeight.Text = "1";
				_listView.ShowColumnHeaders = (bool)_showColHeaders.Checked;
				_listView.ColumnHeaders = new List<ListView.ColumnHeader> (){
					new ListView.ColumnHeader () {
						HeaderName = "Index",
						HeaderAlignment = TextAlignment.Centered,
						MemberAlignment = TextAlignment.Centered,
						MemberType = typeof(int),
						MemberFormat = "{0:00}"
					},
					new ListView.ColumnHeader () {
						HeaderName = "Name",
						HeaderAlignment = TextAlignment.Centered,
						MemberAlignment = TextAlignment.Left
					},
					new ListView.ColumnHeader () {
						HeaderName = "Description",
						HeaderAlignment = TextAlignment.Centered,
						MemberAlignment = TextAlignment.Left
					}
				};
				_headersAlignCB.SelectedItem = (int)_listView.ColumnHeaders [0].HeaderAlignment;
				_membersAlignCB.SelectedItem = (int)_listView.ColumnHeaders [0].MemberAlignment;
				break;
			case 4:
				_listView.Source = new ListWrapper (null);
				_txtItemHeight.Text = "1";
				_listView.ShowColumnHeaders = (bool)_showColHeaders.Checked;
				_listView.ColumnHeaders = null;
				_headersAlignCB.SelectedItem = (int)_listView.HeadersAlignment;
				_membersAlignCB.SelectedItem = (int)_listView.ItemsAlignment;
				break;
			case 5:
				_listView.Source = new ListWrapper (null);
				_txtItemHeight.Text = "1";
				_listView.ShowColumnHeaders = (bool)_showColHeaders.Checked;
				_listView.ColumnHeaders = new List<ListView.ColumnHeader> (){
					new ListView.ColumnHeader () {
						HeaderName = "Name",
						HeaderAlignment = TextAlignment.Centered,
						MemberAlignment = TextAlignment.Left
					},
					new ListView.ColumnHeader () {
						HeaderName = "Description",
						HeaderAlignment = TextAlignment.Centered,
						MemberAlignment = TextAlignment.Left
					}
				};
				_headersAlignCB.SelectedItem = (int)_listView.HeadersAlignment;
				_membersAlignCB.SelectedItem = (int)_listView.ItemsAlignment;
				break;
			}
			Win.SetNeedsDisplay ();
		}

		private void AllowMarkingCB_Toggled (object sender, ToggleEventArgs e)
		{
			_listView.AllowsMarking = (bool)!e.OldValue;
			_allowMultipleCB.Visible = _listView.AllowsMarking;
			Win.SetNeedsDisplay ();
		}

		private void AllowMultipleCB_Toggled (object sender, ToggleEventArgs e)
		{
			_listView.AllowsMultipleSelection = (bool)!e.OldValue;
			Win.SetNeedsDisplay ();
		}

		internal class ScenarioItem {
			public string Name { get; set; }
			public string Description { get; set; }

			public ScenarioItem (string name, string description)
			{
				Name = name;
				Description = description;
			}
		}

		internal class NumericItem {
			public string Item { get; set; }
			public int IntValue { get; set; }
			public float FloatValue { get; set; }
			public double DoubleValue { get; set; }
			public decimal DecimalValue { get; set; }
			public decimal CurrencyValue { get; set; }

			public NumericItem (string item, int intValue, float floatValue, double doubleValue, decimal decimalValue, decimal currencyValue)
			{
				Item = item;
				IntValue = intValue;
				FloatValue = floatValue;
				DoubleValue = doubleValue;
				DecimalValue = decimalValue;
				CurrencyValue = currencyValue;
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