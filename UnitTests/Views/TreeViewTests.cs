using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests {

	public class TreeViewTests {

		readonly ITestOutputHelper output;

		public TreeViewTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		#region Test Setup Methods
		class Factory {
			public Car [] Cars { get; set; }
			public override string ToString ()
			{
				return "Factory";
			}
		};
		class Car {
			public string Name { get; set; }
			public override string ToString ()
			{
				return Name;
			}
		};

		private TreeView<object> CreateTree ()
		{
			return CreateTree (out _, out _, out _);
		}

		private TreeView<object> CreateTree (out Factory factory1, out Car car1, out Car car2)
		{
			car1 = new Car ();
			car2 = new Car ();

			factory1 = new Factory () {
				Cars = new [] { car1, car2 }
			};

			var tree = new TreeView<object> (new DelegateTreeBuilder<object> ((s) => s is Factory f ? f.Cars : null));
			tree.AddObject (factory1);

			return tree;
		}
		#endregion

		/// <summary>
		/// Tests that <see cref="TreeView.Expand(object)"/> and <see cref="TreeView.IsExpanded(object)"/> are consistent
		/// </summary>
		[Fact]
		public void IsExpanded_TrueAfterExpand ()
		{
			var tree = CreateTree (out Factory f, out _, out _);
			Assert.False (tree.IsExpanded (f));

			tree.Expand (f);
			Assert.True (tree.IsExpanded (f));

			tree.Collapse (f);
			Assert.False (tree.IsExpanded (f));
		}

		[Fact]
		public void EmptyTreeView_ContentSizes ()
		{
			var emptyTree = new TreeView ();
			Assert.Equal (0, emptyTree.ContentHeight);
			Assert.Equal (0, emptyTree.GetContentWidth (true));
			Assert.Equal (0, emptyTree.GetContentWidth (false));
		}
		[Fact]
		public void EmptyTreeViewGeneric_ContentSizes ()
		{
			var emptyTree = new TreeView<string> ();
			Assert.Equal (0, emptyTree.ContentHeight);
			Assert.Equal (0, emptyTree.GetContentWidth (true));
			Assert.Equal (0, emptyTree.GetContentWidth (false));
		}

		/// <summary>
		/// Tests that <see cref="TreeView.Expand(object)"/> results in a correct content height
		/// </summary>
		[Fact]
		public void ContentHeight_BiggerAfterExpand ()
		{
			var tree = CreateTree (out Factory f, out _, out _);
			Assert.Equal (1, tree.ContentHeight);

			tree.Expand (f);
			Assert.Equal (3, tree.ContentHeight);

			tree.Collapse (f);
			Assert.Equal (1, tree.ContentHeight);
		}

		[Fact]
		public void ContentWidth_BiggerAfterExpand ()
		{
			var tree = CreateTree (out Factory f, out Car car1, out _);
			tree.BeginInit (); tree.EndInit ();

			tree.Bounds = new Rect (0, 0, 10, 10);

			InitFakeDriver ();

			//-+Factory
			Assert.Equal (9, tree.GetContentWidth (true));

			car1.Name = "123456789";

			tree.Expand (f);

			//..├-123456789
			Assert.Equal (13, tree.GetContentWidth (true));

			tree.Collapse (f);
			//-+Factory
			Assert.Equal (9, tree.GetContentWidth (true));

			Application.Shutdown ();
		}

		[Fact]
		public void ContentWidth_VisibleVsAll ()
		{
			var tree = CreateTree (out Factory f, out Car car1, out Car car2);
			tree.BeginInit (); tree.EndInit ();
			
			// control only allows 1 row to be viewed at once
			tree.Bounds = new Rect (0, 0, 20, 1);

			InitFakeDriver ();

			//-+Factory
			Assert.Equal (9, tree.GetContentWidth (true));
			Assert.Equal (9, tree.GetContentWidth (false));

			car1.Name = "123456789";
			car2.Name = "12345678";

			tree.Expand (f);

			// Although expanded the bigger (longer) child node is not in the rendered area of the control
			Assert.Equal (9, tree.GetContentWidth (true));
			Assert.Equal (13, tree.GetContentWidth (false)); // If you ask for the global max width it includes the longer child

			// Now that we have scrolled down 1 row we should see the big child
			tree.ScrollOffsetVertical = 1;
			Assert.Equal (13, tree.GetContentWidth (true));
			Assert.Equal (13, tree.GetContentWidth (false));

			// Scroll down so only car2 is visible
			tree.ScrollOffsetVertical = 2;
			Assert.Equal (12, tree.GetContentWidth (true));
			Assert.Equal (13, tree.GetContentWidth (false));

			// Scroll way down (off bottom of control even)
			tree.ScrollOffsetVertical = 5;
			Assert.Equal (0, tree.GetContentWidth (true));
			Assert.Equal (13, tree.GetContentWidth (false));

			Application.Shutdown ();
		}
		/// <summary>
		/// Tests that <see cref="TreeView.IsExpanded(object)"/> and <see cref="TreeView.Expand(object)"/> behaves correctly when an object cannot be expanded (because it has no children)
		/// </summary>
		[Fact]
		public void IsExpanded_FalseIfCannotExpand ()
		{
			var tree = CreateTree (out Factory f, out Car c, out _);

			// expose the car by expanding the factory
			tree.Expand (f);

			// car is not expanded
			Assert.False (tree.IsExpanded (c));

			//try to expand the car (should have no effect because cars have no children)
			tree.Expand (c);

			Assert.False (tree.IsExpanded (c));

			// should also be ignored
			tree.Collapse (c);

			Assert.False (tree.IsExpanded (c));

			Application.Shutdown ();
		}

		/// <summary>
		/// Tests illegal ranges for <see cref="TreeView.ScrollOffset"/>
		/// </summary>
		[Fact]
		public void ScrollOffset_CannotBeNegative ()
		{
			var tree = CreateTree ();

			Assert.Equal (0, tree.ScrollOffsetVertical);

			tree.ScrollOffsetVertical = -100;
			Assert.Equal (0, tree.ScrollOffsetVertical);

			tree.ScrollOffsetVertical = 10;
			Assert.Equal (10, tree.ScrollOffsetVertical);
		}

		/// <summary>
		/// Tests <see cref="TreeView.GetScrollOffsetOf(object)"/> for objects that are as yet undiscovered by the tree
		/// </summary>
		[Fact]
		public void GetScrollOffsetOf_MinusOneForUnRevealed ()
		{
			var tree = CreateTree (out Factory f, out Car c1, out Car c2);

			// to start with the tree is collapsed and only knows about the root object
			Assert.Equal (0, tree.GetScrollOffsetOf (f));
			Assert.Equal (-1, tree.GetScrollOffsetOf (c1));
			Assert.Equal (-1, tree.GetScrollOffsetOf (c2));

			// reveal it by expanding the root object
			tree.Expand (f);

			// tree now knows about children
			Assert.Equal (0, tree.GetScrollOffsetOf (f));
			Assert.Equal (1, tree.GetScrollOffsetOf (c1));
			Assert.Equal (2, tree.GetScrollOffsetOf (c2));

			// after collapsing the root node again
			tree.Collapse (f);

			// tree no longer knows about the locations of these objects
			Assert.Equal (0, tree.GetScrollOffsetOf (f));
			Assert.Equal (-1, tree.GetScrollOffsetOf (c1));
			Assert.Equal (-1, tree.GetScrollOffsetOf (c2));
		}

		/// <summary>
		/// Simulates behind the scenes changes to an object (which children it has) and how to sync that into the tree using <see cref="TreeView.RefreshObject(object, bool)"/>
		/// </summary>
		[Fact]
		public void RefreshObject_ChildRemoved ()
		{
			var tree = CreateTree (out Factory f, out Car c1, out Car c2);

			//reveal it by expanding the root object
			tree.Expand (f);

			Assert.Equal (0, tree.GetScrollOffsetOf (f));
			Assert.Equal (1, tree.GetScrollOffsetOf (c1));
			Assert.Equal (2, tree.GetScrollOffsetOf (c2));

			// Factory now no longer makes Car c1 (only c2)
			f.Cars = new Car [] { c2 };

			// Tree does not know this yet
			Assert.Equal (0, tree.GetScrollOffsetOf (f));
			Assert.Equal (1, tree.GetScrollOffsetOf (c1));
			Assert.Equal (2, tree.GetScrollOffsetOf (c2));

			// If the user has selected the node c1
			tree.SelectedObject = c1;

			// When we refresh the tree
			tree.RefreshObject (f);

			// Now tree knows that factory has only one child node c2
			Assert.Equal (0, tree.GetScrollOffsetOf (f));
			Assert.Equal (-1, tree.GetScrollOffsetOf (c1));
			Assert.Equal (1, tree.GetScrollOffsetOf (c2));

			// The old selection was c1 which is now gone so selection should default to the parent of that branch (the factory)
			Assert.Equal (f, tree.SelectedObject);
		}

		/// <summary>
		/// Tests that <see cref="TreeView.GetParent(object)"/> returns the parent object for
		/// Cars (Factories).  Note that the method only works once the parent branch (Factory)
		/// is expanded to expose the child (Car)
		/// </summary>
		[Fact]
		public void GetParent_ReturnsParentOnlyWhenExpanded ()
		{
			var tree = CreateTree (out Factory f, out Car c1, out Car c2);

			Assert.Null (tree.GetParent (f));
			Assert.Null (tree.GetParent (c1));
			Assert.Null (tree.GetParent (c2));

			// now when we expand the factory we discover the cars
			tree.Expand (f);

			Assert.Null (tree.GetParent (f));
			Assert.Equal (f, tree.GetParent (c1));
			Assert.Equal (f, tree.GetParent (c2));

			tree.Collapse (f);

			Assert.Null (tree.GetParent (f));
			Assert.Null (tree.GetParent (c1));
			Assert.Null (tree.GetParent (c2));
		}

		/// <summary>
		/// Tests how the tree adapts to changes in the ChildrenGetter delegate during runtime
		/// when some branches are expanded and the new delegate returns children for a node that
		/// previously didn't have any children
		/// </summary>
		[Fact]
		public void RefreshObject_AfterChangingChildrenGetterDuringRuntime ()
		{
			var tree = CreateTree (out Factory f, out Car c1, out Car c2);

			string wheel = "Shiny Wheel";

			// Expand the Factory
			tree.Expand (f);

			// c1 cannot have children
			Assert.Equal (f, tree.GetParent (c1));

			// expanding it does nothing
			tree.Expand (c1);
			Assert.False (tree.IsExpanded (c1));

			// change the children getter so that now cars can have wheels
			tree.TreeBuilder = new DelegateTreeBuilder<object> ((o) =>
				 // factories have cars
				 o is Factory ? new object [] { c1, c2 }
				 // cars have wheels
				 : new object [] { wheel });

			// still cannot expand
			tree.Expand (c1);
			Assert.False (tree.IsExpanded (c1));

			tree.RefreshObject (c1);
			tree.Expand (c1);
			Assert.True (tree.IsExpanded (c1));
			Assert.Equal (wheel, tree.GetChildren (c1).FirstOrDefault ());
		}
		/// <summary>
		/// Same as <see cref="RefreshObject_AfterChangingChildrenGetterDuringRuntime"/> but
		/// uses <see cref="TreeView.RebuildTree()"/> instead of <see cref="TreeView.RefreshObject(object, bool)"/>
		/// </summary>
		[Fact]
		public void RebuildTree_AfterChangingChildrenGetterDuringRuntime ()
		{
			var tree = CreateTree (out Factory f, out Car c1, out Car c2);

			string wheel = "Shiny Wheel";

			// Expand the Factory
			tree.Expand (f);

			// c1 cannot have children
			Assert.Equal (f, tree.GetParent (c1));

			// expanding it does nothing
			tree.Expand (c1);
			Assert.False (tree.IsExpanded (c1));

			// change the children getter so that now cars can have wheels
			tree.TreeBuilder = new DelegateTreeBuilder<object> ((o) =>
				 // factories have cars
				 o is Factory ? new object [] { c1, c2 }
				 // cars have wheels
				 : new object [] { wheel });

			// still cannot expand
			tree.Expand (c1);
			Assert.False (tree.IsExpanded (c1));

			// Rebuild the tree
			tree.RebuildTree ();

			// Rebuild should not have collapsed any branches or done anything wierd
			Assert.True (tree.IsExpanded (f));

			tree.Expand (c1);
			Assert.True (tree.IsExpanded (c1));
			Assert.Equal (wheel, tree.GetChildren (c1).FirstOrDefault ());
		}
		/// <summary>
		/// Tests that <see cref="TreeView.GetChildren(object)"/> returns the child objects for
		/// the factory.  Note that the method only works once the parent branch (Factory)
		/// is expanded to expose the child (Car)
		/// </summary>
		[Fact]
		public void GetChildren_ReturnsChildrenOnlyWhenExpanded ()
		{
			var tree = CreateTree (out Factory f, out Car c1, out Car c2);

			Assert.Empty (tree.GetChildren (f));
			Assert.Empty (tree.GetChildren (c1));
			Assert.Empty (tree.GetChildren (c2));

			// now when we expand the factory we discover the cars
			tree.Expand (f);

			Assert.Contains (c1, tree.GetChildren (f));
			Assert.Contains (c2, tree.GetChildren (f));
			Assert.Empty (tree.GetChildren (c1));
			Assert.Empty (tree.GetChildren (c2));

			tree.Collapse (f);

			Assert.Empty (tree.GetChildren (f));
			Assert.Empty (tree.GetChildren (c1));
			Assert.Empty (tree.GetChildren (c2));
		}

		[Fact]
		public void TreeNode_WorksWithoutDelegate ()
		{
			var tree = new TreeView ();

			var root = new TreeNode ("Root");
			root.Children.Add (new TreeNode ("Leaf1"));
			root.Children.Add (new TreeNode ("Leaf2"));

			tree.AddObject (root);

			tree.Expand (root);
			Assert.Equal (2, tree.GetChildren (root).Count ());
		}

		[Fact]
		public void MultiSelect_GetAllSelectedObjects ()
		{
			var tree = new TreeView ();

			TreeNode l1;
			TreeNode l2;
			TreeNode l3;
			TreeNode l4;

			var root = new TreeNode ("Root");
			root.Children.Add (l1 = new TreeNode ("Leaf1"));
			root.Children.Add (l2 = new TreeNode ("Leaf2"));
			root.Children.Add (l3 = new TreeNode ("Leaf3"));
			root.Children.Add (l4 = new TreeNode ("Leaf4"));

			tree.AddObject (root);
			tree.MultiSelect = true;

			tree.Expand (root);
			Assert.Empty (tree.GetAllSelectedObjects ());

			tree.SelectedObject = root;

			Assert.Single (tree.GetAllSelectedObjects (), root);

			// move selection down 1
			tree.AdjustSelection (1, false);

			Assert.Single (tree.GetAllSelectedObjects (), l1);

			// expand selection down 2 (e.g. shift down twice)
			tree.AdjustSelection (1, true);
			tree.AdjustSelection (1, true);

			Assert.Equal (3, tree.GetAllSelectedObjects ().Count ());
			Assert.Contains (l1, tree.GetAllSelectedObjects ());
			Assert.Contains (l2, tree.GetAllSelectedObjects ());
			Assert.Contains (l3, tree.GetAllSelectedObjects ());

			tree.Collapse (root);

			// No selected objects since the root was collapsed
			Assert.Empty (tree.GetAllSelectedObjects ());
		}

		[Fact]
		public void ObjectActivated_Called ()
		{
			var tree = CreateTree (out Factory f, out Car car1, out _);

			InitFakeDriver ();

			object activated = null;
			bool called = false;

			// register for the event
			tree.ObjectActivated += (s, e) => {
				activated = e.ActivatedObject;
				called = true;
			};

			Assert.False (called);

			// no object is selected yet so no event should happen
			tree.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ()));

			Assert.Null (activated);
			Assert.False (called);

			// down to select factory
			tree.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ()));

			tree.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ()));

			Assert.True (called);
			Assert.Same (f, activated);

			Application.Shutdown ();
		}

		[Fact]
		public void GoTo_OnlyAppliesToExposedObjects ()
		{
			var tree = CreateTree (out Factory f, out Car car1, out _);
			tree.BeginInit (); tree.EndInit ();

			// Make tree bounds 1 in height so that EnsureVisible always requires updating scroll offset
			tree.Bounds = new Rect (0, 0, 50, 1);

			Assert.Null (tree.SelectedObject);
			Assert.Equal (0, tree.ScrollOffsetVertical);

			// car 1 is not yet exposed
			tree.GoTo (car1);

			Assert.Null (tree.SelectedObject);
			Assert.Equal (0, tree.ScrollOffsetVertical);

			tree.Expand (f);

			// Car1 is now exposed by expanding the factory
			tree.GoTo (car1);

			Assert.Equal (car1, tree.SelectedObject);
			Assert.Equal (1, tree.ScrollOffsetVertical);
		}

		[Fact]
		public void GoToEnd_ShouldNotFailOnEmptyTreeView ()
		{
			var tree = new TreeView ();

			var exception = Record.Exception (() => tree.GoToEnd ());

			Assert.Null (exception);
		}

		[Fact]
		public void ObjectActivated_CustomKey ()
		{
			var tree = CreateTree (out Factory f, out Car car1, out _);

			InitFakeDriver ();

			tree.ObjectActivationKey = Key.Delete;
			object activated = null;
			bool called = false;

			// register for the event
			tree.ObjectActivated += (s, e) => {
				activated = e.ActivatedObject;
				called = true;
			};

			Assert.False (called);

			// no object is selected yet so no event should happen
			tree.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ()));

			Assert.Null (activated);
			Assert.False (called);

			// down to select factory
			tree.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ()));

			tree.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ()));

			// Enter is not the activation key in this unit test
			Assert.Null (activated);
			Assert.False (called);

			// Delete is the activation key in this test so should result in activation occurring
			tree.ProcessKey (new KeyEvent (Key.Delete, new KeyModifiers ()));

			Assert.True (called);
			Assert.Same (f, activated);

			Application.Shutdown ();
		}

		[Fact]
		public void ObjectActivationButton_DoubleClick ()
		{
			var tree = CreateTree (out Factory f, out Car car1, out _);

			InitFakeDriver ();

			object activated = null;
			bool called = false;

			// register for the event
			tree.ObjectActivated += (s, e) => {
				activated = e.ActivatedObject;
				called = true;
			};

			Assert.False (called);

			// double click triggers activation
			tree.MouseEvent (new MouseEvent () { Y = 0, Flags = MouseFlags.Button1DoubleClicked });

			Assert.True (called);
			Assert.Same (f, activated);
			Assert.Same (f, tree.SelectedObject);

			Application.Shutdown ();
		}

		[Fact]
		public void ObjectActivationButton_SetToNull ()
		{
			var tree = CreateTree (out Factory f, out Car car1, out _);

			InitFakeDriver ();

			// disable activation
			tree.ObjectActivationButton = null;

			object activated = null;
			bool called = false;

			// register for the event
			tree.ObjectActivated += (s, e) => {
				activated = e.ActivatedObject;
				called = true;
			};

			Assert.False (called);

			// double click does nothing because we changed button to null
			tree.MouseEvent (new MouseEvent () { Y = 0, Flags = MouseFlags.Button1DoubleClicked });

			Assert.False (called);
			Assert.Null (activated);
			Assert.Null (tree.SelectedObject);

			Application.Shutdown ();
		}

		[Fact]
		public void ObjectActivationButton_RightClick ()
		{
			var tree = CreateTree (out Factory f, out Car car1, out _);

			InitFakeDriver ();

			tree.ObjectActivationButton = MouseFlags.Button2Clicked;
			tree.ExpandAll ();

			object activated = null;
			bool called = false;

			// register for the event
			tree.ObjectActivated += (s, e) => {
				activated = e.ActivatedObject;
				called = true;
			};

			Assert.False (called);

			// double click does nothing because we changed button binding to right click
			tree.MouseEvent (new MouseEvent () { Y = 1, Flags = MouseFlags.Button1DoubleClicked });

			Assert.Null (activated);
			Assert.False (called);

			tree.MouseEvent (new MouseEvent () { Y = 1, Flags = MouseFlags.Button2Clicked });

			Assert.True (called);
			Assert.Same (car1, activated);
			Assert.Same (car1, tree.SelectedObject);

			Application.Shutdown ();
		}


		/// <summary>
		/// Simulates behind the scenes changes to an object (which children it has) and how to sync that into the tree using <see cref="TreeView.RefreshObject(object, bool)"/>
		/// </summary>
		[Fact]
		public void RefreshObject_EqualityTest ()
		{
			var obj1 = new EqualityTestObject () { Name = "Bob", Age = 1 };
			var obj2 = new EqualityTestObject () { Name = "Bob", Age = 2 }; ;

			string root = "root";

			var tree = new TreeView<object> ();
			tree.TreeBuilder = new DelegateTreeBuilder<object> ((s) => ReferenceEquals (s, root) ? new object [] { obj1 } : null);
			tree.AddObject (root);

			// Tree is not expanded so the root has no children yet
			Assert.Empty (tree.GetChildren (root));

			tree.Expand (root);

			// now that the tree is expanded we should get our child returned
			Assert.Equal (1, tree.GetChildren (root).Count (child => ReferenceEquals (obj1, child)));

			// change the getter to return an Equal object (but not the same reference - obj2)
			tree.TreeBuilder = new DelegateTreeBuilder<object> ((s) => ReferenceEquals (s, root) ? new object [] { obj2 } : null);

			// tree has cached the knowledge of what children the root has so won't know about the change (we still get obj1)
			Assert.Equal (1, tree.GetChildren (root).Count (child => ReferenceEquals (obj1, child)));

			// now that we refresh the root we should get the new child reference (obj2)
			tree.RefreshObject (root);
			Assert.Equal (1, tree.GetChildren (root).Count (child => ReferenceEquals (obj2, child)));

		}
		[Fact, AutoInitShutdown]
		public void TestGetObjectOnRow ()
		{
			var tv = new TreeView { Width = 20, Height = 10 };
			tv.BeginInit (); tv.EndInit ();
			var n1 = new TreeNode ("normal");
			var n1_1 = new TreeNode ("pink");
			var n1_2 = new TreeNode ("normal");
			n1.Children.Add (n1_1);
			n1.Children.Add (n1_2);

			var n2 = new TreeNode ("pink");
			tv.AddObject (n1);
			tv.AddObject (n2);
			tv.Expand (n1);

			tv.ColorScheme = new ColorScheme ();
			tv.LayoutSubviews ();
			tv.Redraw (tv.Bounds);

			TestHelpers.AssertDriverContentsAre (
@"├-normal
│ ├─pink
│ └─normal
└─pink
", output);

			Assert.Same (n1, tv.GetObjectOnRow (0));
			Assert.Same (n1_1, tv.GetObjectOnRow (1));
			Assert.Same (n1_2, tv.GetObjectOnRow (2));
			Assert.Same (n2, tv.GetObjectOnRow (3));
			Assert.Null (tv.GetObjectOnRow (4));

			tv.Collapse (n1);

			tv.Redraw (tv.Bounds);

			TestHelpers.AssertDriverContentsAre (
@"├+normal
└─pink
", output);

			Assert.Same (n1, tv.GetObjectOnRow (0));
			Assert.Same (n2, tv.GetObjectOnRow (1));
			Assert.Null (tv.GetObjectOnRow (2));
			Assert.Null (tv.GetObjectOnRow (3));
			Assert.Null (tv.GetObjectOnRow (4));
		}

		[Fact, AutoInitShutdown]
		public void TestGetObjectRow ()
		{
			var tv = new TreeView { Width = 20, Height = 10 };

			var n1 = new TreeNode ("normal");
			var n1_1 = new TreeNode ("pink");
			var n1_2 = new TreeNode ("normal");
			n1.Children.Add (n1_1);
			n1.Children.Add (n1_2);

			var n2 = new TreeNode ("pink");
			tv.AddObject (n1);
			tv.AddObject (n2);
			tv.Expand (n1);

			tv.ColorScheme = new ColorScheme ();
			tv.LayoutSubviews ();
			tv.Redraw (tv.Bounds);

			TestHelpers.AssertDriverContentsAre (
@"├-normal
│ ├─pink
│ └─normal
└─pink
", output);

			Assert.Equal (0, tv.GetObjectRow (n1));
			Assert.Equal (1, tv.GetObjectRow (n1_1));
			Assert.Equal (2, tv.GetObjectRow (n1_2));
			Assert.Equal (3, tv.GetObjectRow (n2));

			tv.Collapse (n1);

			tv.LayoutSubviews ();
			tv.Redraw (tv.Bounds);

			TestHelpers.AssertDriverContentsAre (
@"├+normal
└─pink
", output);
			Assert.Equal (0, tv.GetObjectRow (n1));
			Assert.Null (tv.GetObjectRow (n1_1));
			Assert.Null (tv.GetObjectRow (n1_2));
			Assert.Equal (1, tv.GetObjectRow (n2));

			// scroll down 1
			tv.ScrollOffsetVertical = 1;

			tv.LayoutSubviews ();
			tv.Redraw (tv.Bounds);

			TestHelpers.AssertDriverContentsAre (
@"└─pink
", output);
			Assert.Equal (-1, tv.GetObjectRow (n1));
			Assert.Null (tv.GetObjectRow (n1_1));
			Assert.Null (tv.GetObjectRow (n1_2));
			Assert.Equal (0, tv.GetObjectRow (n2));
		}
		[Fact, AutoInitShutdown]
		public void TestTreeViewColor ()
		{
			var tv = new TreeView { Width = 20, Height = 10 };

			var n1 = new TreeNode ("normal");
			var n1_1 = new TreeNode ("pink");
			var n1_2 = new TreeNode ("normal");
			n1.Children.Add (n1_1);
			n1.Children.Add (n1_2);

			var n2 = new TreeNode ("pink");
			tv.AddObject (n1);
			tv.AddObject (n2);
			tv.Expand (n1);

			var pink = new Attribute (Color.Magenta, Color.Black);
			var hotpink = new Attribute (Color.BrightMagenta, Color.Black);

			tv.ColorScheme = new ColorScheme ();
			tv.LayoutSubviews ();
			tv.Redraw (tv.Bounds);

			// Normal drawing of the tree view
			TestHelpers.AssertDriverContentsAre (
@"├-normal
│ ├─pink
│ └─normal
└─pink
", output);
			// Should all be the same color
			TestHelpers.AssertDriverColorsAre (
@"00000000
00000000
0000000000
000000
",
				new [] { tv.ColorScheme.Normal, pink });

			// create a new color scheme
			var pinkScheme = new ColorScheme {
				Normal = pink,
				Focus = hotpink
			};

			// and a delegate that uses the pink color scheme 
			// for nodes "pink"
			tv.ColorGetter = (n) => n.Text.Equals ("pink") ? pinkScheme : null;

			// redraw now that the custom color
			// delegate is registered
			tv.Redraw (tv.Bounds);

			// Same text
			TestHelpers.AssertDriverContentsAre (
@"├-normal
│ ├─pink
│ └─normal
└─pink
", output);
			// but now the item (only not lines) appear
			// in pink when they are the word "pink"
			TestHelpers.AssertDriverColorsAre (
@"00000000
00001111
0000000000
001111
",
				new [] { tv.ColorScheme.Normal, pink });
		}

		[Fact, AutoInitShutdown]
		public void DesiredCursorVisibility_MultiSelect ()
		{
			var tv = new TreeView { Width = 20, Height = 10 };

			var n1 = new TreeNode ("normal");
			var n2 = new TreeNode ("pink");
			tv.AddObject (n1);
			tv.AddObject (n2);

			Application.Top.Add (tv);
			Application.Begin (Application.Top);

			Assert.True (tv.MultiSelect);
			Assert.True (tv.HasFocus);
			Assert.Equal (CursorVisibility.Invisible, tv.DesiredCursorVisibility);

			tv.SelectAll ();
			tv.DesiredCursorVisibility = CursorVisibility.Default;
			Application.Refresh ();
			Application.Driver.GetCursorVisibility (out CursorVisibility visibility);
			Assert.Equal (CursorVisibility.Default, tv.DesiredCursorVisibility);
			Assert.Equal (CursorVisibility.Default, visibility);
		}

		/// <summary>
		/// Test object which considers for equality only <see cref="Name"/>
		/// </summary>
		private class EqualityTestObject {
			public string Name { get; set; }
			public int Age { get; set; }

			public override int GetHashCode ()
			{
				return Name?.GetHashCode () ?? base.GetHashCode ();
			}
			public override bool Equals (object obj)
			{
				return obj is EqualityTestObject eto && Equals (Name, eto.Name);
			}
		}

		private void InitFakeDriver ()
		{
			var driver = new FakeDriver ();
			Application.Init (driver);
			driver.Init (() => { });
		}
	}
}
