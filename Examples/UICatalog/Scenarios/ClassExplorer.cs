#nullable enable

using System.Reflection;
using System.Text;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Class Explorer", "Tree view explorer for classes by namespace based on TreeView.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("TreeView")]
public class ClassExplorer : Scenario
{
    private CheckBox? _highlightModelTextOnlyCheckBox;
    private CheckBox? _showPrivateCheckBox;
    private Code? _codeView;
    private TreeView<object>? _treeView;

    private Window? _win;

    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window win = new ()
        {
            Title = GetName (),
            BorderStyle = LineStyle.None
        };
        _win = win;

        // MenuBar
        MenuBar menuBar = new ();

        // Search controls
        Label lblSearch = new ()
        {
            Y = Pos.Bottom (menuBar),
            Title = "Search:"
        };

        TextField tfSearch = new ()
        {
            Y = Pos.Top (lblSearch),
            X = Pos.Right (lblSearch) + 1,
            Width = 20
        };

        // TreeView
        _treeView = new ()
        {
            Y = Pos.Bottom (lblSearch),
            Width = Dim.Percent (50),
            Height = Dim.Fill ()
        };

        TreeViewTextFilter<object> filter = new (_treeView);
        _treeView.Filter = filter;

        tfSearch.TextChanged += (_, _) =>
                                {
                                    filter.Text = tfSearch.Text;

                                    if (_treeView.SelectedObject is not null)
                                    {
                                        _treeView.EnsureVisible (_treeView.SelectedObject);
                                    }
                                };

        _treeView.AddObjects (AppDomain.CurrentDomain.GetAssemblies ());
        _treeView.AspectGetter = GetRepresentation;
        _treeView.TreeBuilder = new DelegateTreeBuilder<object> (ChildGetter, CanExpand);
        _treeView.SelectionChanged += TreeView_SelectionChanged;

        // Code view for details
        _codeView = new ()
        {
            X = Pos.Right (_treeView),
            Y = Pos.Top (_treeView),
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            SyntaxHighlighter = null
        };

        // Menu setup
        _showPrivateCheckBox = new ()
        {
            Title = "_Include Private"
        };
        _showPrivateCheckBox.ValueChanged += (_, _) => ShowPrivate ();

        _highlightModelTextOnlyCheckBox = new ()
        {
            Title = "_Highlight Model Text Only"
        };
        _highlightModelTextOnlyCheckBox.ValueChanged += (_, _) => OnCheckHighlightModelTextOnly ();

        menuBar.Add (
                     new MenuBarItem (
                                      Strings.menuFile,
                                      [
                                          new MenuItem
                                          {
                                              Title = Strings.cmdQuit,
                                              Action = Quit
                                          }
                                      ]
                                     )
                    );

        menuBar.Add (
                     new MenuBarItem (
                                      "_View",
                                      [
                                          new MenuItem
                                          {
                                              CommandView = _showPrivateCheckBox
                                          },
                                          new MenuItem
                                          {
                                              Title = "_Expand All",
                                              Action = () => _treeView?.ExpandAll ()
                                          },
                                          new MenuItem
                                          {
                                              Title = "_Collapse All",
                                              Action = () => _treeView?.CollapseAll ()
                                          }
                                      ]
                                     )
                    );

        menuBar.Add (
                     new MenuBarItem (
                                      "_Style",
                                      [
                                          new MenuItem
                                          {
                                              CommandView = _highlightModelTextOnlyCheckBox
                                          }
                                      ]
                                     )
                    );

        // Add views in order of visual appearance
        _win.Add (menuBar, lblSearch, tfSearch, _treeView, _codeView);

        app.Run (_win);
        _win.Dispose ();
    }

    private bool CanExpand (object arg) => arg is Assembly or Type or ShowForType;

    private IEnumerable<object> ChildGetter (object arg)
    {
        try
        {
            return arg switch
                   {
                       Assembly assembly => assembly.GetTypes (),
                       Type type => Enum.GetValues (typeof (Showable))
                                        .Cast<Showable> ()
                                        .Select (v => new ShowForType (v, type)),
                       ShowForType show => show.ToShow switch
                                           {
                                               Showable.Properties => show.Type.GetProperties (GetFlags ()),
                                               Showable.Constructors => show.Type.GetConstructors (GetFlags ()),
                                               Showable.Events => show.Type.GetEvents (GetFlags ()),
                                               Showable.Fields => show.Type.GetFields (GetFlags ()),
                                               Showable.Methods => show.Type.GetMethods (GetFlags ()),
                                               _ => Enumerable.Empty<object> ()
                                           },
                       _ => Enumerable.Empty<object> ()
                   };
        }
        catch (Exception)
        {
            return Enumerable.Empty<object> ();
        }
    }

    private BindingFlags GetFlags () =>
        _showPrivateCheckBox?.Value == CheckState.Checked
            ? BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            : BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;

    private string GetRepresentation (object model)
    {
        try
        {
            return model switch
                   {
                       Assembly assembly => assembly.GetName ().Name ?? string.Empty,
                       PropertyInfo propertyInfo => propertyInfo.Name,
                       FieldInfo fieldInfo => fieldInfo.Name,
                       EventInfo eventInfo => eventInfo.Name,
                       _ => model.ToString () ?? string.Empty
                   };
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    private void OnCheckHighlightModelTextOnly ()
    {
        if (_treeView is null)
        {
            return;
        }

        _treeView.Style.HighlightModelTextOnly = _highlightModelTextOnlyCheckBox?.Value == CheckState.Checked;
        _treeView.SetNeedsDraw ();
    }

    private void Quit () { _win?.RequestStop (); }

    private void ShowPrivate ()
    {
        _treeView?.RebuildTree ();
        _treeView?.SetFocus ();
    }

    private void TreeView_SelectionChanged (object? sender, SelectionChangedEventArgs<object> e)
    {
        if (_treeView is null || _codeView is null)
        {
            return;
        }

        object? val = e.NewValue;
        object [] all = _treeView.GetAllSelectedObjects ().ToArray ();

        if (val is null or ShowForType)
        {
            return;
        }

        try
        {
            if (all.Length > 1)
            {
                _codeView.Text = $"{all.Length} Objects";
            }
            else
            {
                StringBuilder sb = new ();

                sb.AppendLine (e.NewValue?.GetType ().Name ?? string.Empty);

                switch (val)
                {
                    case Assembly assembly:
                        sb.AppendLine ($"Location:{assembly.Location}");
                        sb.AppendLine ($"FullName:{assembly.FullName}");

                        break;

                    case PropertyInfo propertyInfo:
                        sb.AppendLine ($"Name:{propertyInfo.Name}");
                        sb.AppendLine ($"Type:{propertyInfo.PropertyType}");
                        sb.AppendLine ($"CanWrite:{propertyInfo.CanWrite}");
                        sb.AppendLine ($"CanRead:{propertyInfo.CanRead}");

                        break;

                    case FieldInfo fieldInfo:
                        sb.AppendLine ($"Name:{fieldInfo.Name}");
                        sb.AppendLine ($"Type:{fieldInfo.FieldType}");

                        break;

                    case EventInfo eventInfo:
                        sb.AppendLine ($"Name:{eventInfo.Name}");
                        sb.AppendLine ("Parameters:");

                        if (eventInfo.EventHandlerType?.GetMethod ("Invoke") is { } invokeMethod)
                        {
                            foreach (ParameterInfo parameter in invokeMethod.GetParameters ())
                            {
                                sb.AppendLine ($"  {parameter.ParameterType} {parameter.Name}");
                            }
                        }

                        break;

                    case MethodInfo methodInfo:
                        sb.AppendLine ($"Name:{methodInfo.Name}");
                        sb.AppendLine ($"IsPublic:{methodInfo.IsPublic}");
                        sb.AppendLine ($"IsStatic:{methodInfo.IsStatic}");
                        sb.AppendLine ($"Parameters:{(methodInfo.GetParameters ().Length > 0 ? string.Empty : "None")}");

                        foreach (ParameterInfo parameter in methodInfo.GetParameters ())
                        {
                            sb.AppendLine ($"  {parameter.ParameterType} {parameter.Name}");
                        }

                        break;

                    case ConstructorInfo constructorInfo:
                        sb.AppendLine ($"Name:{constructorInfo.Name}");
                        sb.AppendLine ($"Parameters:{(constructorInfo.GetParameters ().Length > 0 ? string.Empty : "None")}");

                        foreach (ParameterInfo parameter in constructorInfo.GetParameters ())
                        {
                            sb.AppendLine ($"  {parameter.ParameterType} {parameter.Name}");
                        }

                        break;
                }

                _codeView.Text = sb.ToString ().Replace ("\r\n", "\n");
            }
        }
        catch (Exception ex)
        {
            _codeView.Text = ex.Message;
        }

        _codeView.SetNeedsDraw ();
    }

    private enum Showable
    {
        Properties,
        Fields,
        Events,
        Constructors,
        Methods
    }

    private sealed class ShowForType
    {
        public ShowForType (Showable toShow, Type type)
        {
            ToShow = toShow;
            Type = type;
        }

        public Showable ToShow { get; }
        public Type Type { get; }

        public override bool Equals (object? obj) =>
            obj is ShowForType type && EqualityComparer<Type>.Default.Equals (Type, type.Type) && ToShow == type.ToShow;

        public override int GetHashCode () => HashCode.Combine (Type, ToShow);

        public override string ToString () => ToShow.ToString ();
    }
}
