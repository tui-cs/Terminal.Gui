using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace UICatalog.Scenarios {

	[ScenarioMetadata (Name: "Class Explorer", Description: "Tree view explorer for classes by namespace based on TreeView.")]
	[ScenarioCategory ("Controls"), ScenarioCategory ("TreeView")]
	public class ClassExplorer : Scenario {
		private TreeView<object> treeView;
		private TextView textView;
		private MenuItem miShowPrivate;

		private enum Showable {
			Properties,
			Fields,
			Events,
			Constructors,
			Methods,
		}

		private class ShowForType {
			public ShowForType (Showable toShow, Type type)
			{
				ToShow = toShow;
				Type = type;
			}

			public Type Type { get; set; }
			public Showable ToShow { get; set; }

			// Make sure to implement Equals methods on your objects if you intend to return new instances every time in ChildGetter
			public override bool Equals (object obj)
			{
				return obj is ShowForType type &&
				       EqualityComparer<Type>.Default.Equals (Type, type.Type) &&
				       ToShow == type.ToShow;
			}

			public override int GetHashCode ()
			{
				return HashCode.Combine (Type, ToShow);
			}

			public override string ToString ()
			{
				return ToShow.ToString ();
			}
		}

		MenuItem highlightModelTextOnly;

		public override void Setup ()
		{
			Win.Title = this.GetName ();
			Win.Y = 1; // menu
			Win.Height = Dim.Fill (1); // status bar
			Application.Top.LayoutSubviews ();

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_Quit", "", () => Quit()),
				}),
				new MenuBarItem ("_View", new MenuItem [] {
					miShowPrivate = new MenuItem ("_Include Private", "", () => ShowPrivate()){
						Checked = false,
						CheckType = MenuItemCheckStyle.Checked
					},
					new MenuItem ("_Expand All", "", () => treeView.ExpandAll()),
					new MenuItem ("_Collapse All", "", () => treeView.CollapseAll())
				}),
				new MenuBarItem ("_Style", new MenuItem [] {
					highlightModelTextOnly = new MenuItem ("_Highlight Model Text Only", "", () => OnCheckHighlightModelTextOnly()) {
						CheckType = MenuItemCheckStyle.Checked
					},
				})
			});
			Application.Top.Add (menu);

			treeView = new TreeView<object> () {
				X = 0,
				Y = 0,
				Width = Dim.Percent (50),
				Height = Dim.Fill (),
			};

			treeView.AddObjects (AppDomain.CurrentDomain.GetAssemblies ());
			treeView.AspectGetter = GetRepresentation;
			treeView.TreeBuilder = new DelegateTreeBuilder<object> (ChildGetter, CanExpand);
			treeView.SelectionChanged += TreeView_SelectionChanged;

			Win.Add (treeView);

			textView = new TextView () {
				X = Pos.Right (treeView),
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};

			Win.Add (textView);
		}

		private void OnCheckHighlightModelTextOnly ()
		{
			treeView.Style.HighlightModelTextOnly = !treeView.Style.HighlightModelTextOnly;
			highlightModelTextOnly.Checked = treeView.Style.HighlightModelTextOnly;
			treeView.SetNeedsDisplay ();
		}

		private void ShowPrivate ()
		{
			miShowPrivate.Checked = !miShowPrivate.Checked;
			treeView.RebuildTree ();
			treeView.SetFocus ();
		}

		private BindingFlags GetFlags ()
		{
			if (miShowPrivate.Checked == true) {
				return BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			}

			return BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
		}

		private void TreeView_SelectionChanged (object sender, SelectionChangedEventArgs<object> e)
		{
			var val = e.NewValue;
			var all = treeView.GetAllSelectedObjects ().ToArray ();

			if (val == null || val is ShowForType) {
				return;
			}

			try {

				if (all.Length > 1) {

					textView.Text = all.Length + " Objects";
				} else {
					StringBuilder sb = new StringBuilder ();

					// tell the user about the currently selected tree node
					sb.AppendLine (e.NewValue.GetType ().Name);

					if (val is Assembly ass) {
						sb.AppendLine ($"Location:{ass.Location}");
						sb.AppendLine ($"FullName:{ass.FullName}");
					}

					if (val is PropertyInfo p) {
						sb.AppendLine ($"Name:{p.Name}");
						sb.AppendLine ($"Type:{p.PropertyType}");
						sb.AppendLine ($"CanWrite:{p.CanWrite}");
						sb.AppendLine ($"CanRead:{p.CanRead}");
					}

					if (val is FieldInfo f) {
						sb.AppendLine ($"Name:{f.Name}");
						sb.AppendLine ($"Type:{f.FieldType}");
					}

					if (val is EventInfo ev) {
						sb.AppendLine ($"Name:{ev.Name}");
						sb.AppendLine ($"Parameters:");
						foreach (var parameter in ev.EventHandlerType.GetMethod ("Invoke").GetParameters ()) {
							sb.AppendLine ($"  {parameter.ParameterType} {parameter.Name}");
						}
					}

					if (val is MethodInfo method) {
						sb.AppendLine ($"Name:{method.Name}");
						sb.AppendLine ($"IsPublic:{method.IsPublic}");
						sb.AppendLine ($"IsStatic:{method.IsStatic}");
						sb.AppendLine ($"Parameters:{(method.GetParameters ().Any () ? "" : "None")}");
						foreach (var parameter in method.GetParameters ()) {
							sb.AppendLine ($"  {parameter.ParameterType} {parameter.Name}");
						}
					}

					if (val is ConstructorInfo ctor) {
						sb.AppendLine ($"Name:{ctor.Name}");
						sb.AppendLine ($"Parameters:{(ctor.GetParameters ().Any () ? "" : "None")}");
						foreach (var parameter in ctor.GetParameters ()) {
							sb.AppendLine ($"  {parameter.ParameterType} {parameter.Name}");
						}
					}

					textView.Text = sb.ToString ().Replace ("\r\n", "\n");
				}

			} catch (Exception ex) {

				textView.Text = ex.Message;
			}
			textView.SetNeedsDisplay ();
		}

		private bool CanExpand (object arg)
		{
			return arg is Assembly || arg is Type || arg is ShowForType;
		}

		private IEnumerable<object> ChildGetter (object arg)
		{
			try {
				if (arg is Assembly a) {
					return a.GetTypes ();
				}

				if (arg is Type t) {
					// Note that here we cannot simply return the enum values as the same object cannot appear under multiple branches
					return Enum.GetValues (typeof (Showable))
						.Cast<Showable> ()
						// Although we new the Type every time the delegate is called state is preserved because the class has appropriate equality members
						.Select (v => new ShowForType (v, t));
				}

				if (arg is ShowForType show) {
					switch (show.ToShow) {
					case Showable.Properties:
						return show.Type.GetProperties (GetFlags ());
					case Showable.Constructors:
						return show.Type.GetConstructors (GetFlags ());
					case Showable.Events:
						return show.Type.GetEvents (GetFlags ());
					case Showable.Fields:
						return show.Type.GetFields (GetFlags ());
					case Showable.Methods:
						return show.Type.GetMethods (GetFlags ());
					}
				}

			} catch (Exception) {
				return Enumerable.Empty<object> ();
			}
			return Enumerable.Empty<object> ();
		}

		private string GetRepresentation (object model)
		{
			try {
				if (model is Assembly ass) {
					return ass.GetName ().Name;
				}

				if (model is PropertyInfo p) {
					return p.Name;
				}

				if (model is FieldInfo f) {
					return f.Name;
				}

				if (model is EventInfo ei) {
					return ei.Name;
				}


			} catch (Exception ex) {

				return ex.Message;
			}

			return model.ToString ();
		}
		private void Quit ()
		{
			Application.RequestStop ();
		}
	}
}
