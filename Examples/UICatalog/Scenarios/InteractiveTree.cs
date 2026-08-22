#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Interactive Tree", "Create nodes and child nodes in TreeView.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("TreeView")]
public class InteractiveTree : Scenario
{
    private IApplication? _app;
    private TreeView? _treeView;
    private Window? _appWindow;

    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();
        _app = app;

        _appWindow = new ()
        {
            Title = GetName (),
            BorderStyle = LineStyle.None
        };

        // MenuBar
        MenuBar menu = new ();

        menu.Add (
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

        _treeView = new ()
        {
            X = 0,
            Y = Pos.Bottom (menu),
            Width = Dim.Fill (),
            Height = Dim.Fill (1)
        };
        _treeView.KeyDown += treeView_KeyPress;

        // StatusBar
        StatusBar statusBar = new (
                                   [
                                       new (Application.GetDefaultKey (Command.Quit), "Quit", Quit),
                                       new (Key.C.WithCtrl, "Add Child", AddChildNode),
                                       new (Key.T.WithCtrl, "Add Root", AddRootNode),
                                       new (Key.R.WithCtrl, "Rename Node", RenameNode)
                                   ]
                                  );

        _appWindow.Add (menu, _treeView, statusBar);

        app.Run (_appWindow);
        _appWindow.Dispose ();
    }

    private void AddChildNode ()
    {
        if (_treeView is null)
        {
            return;
        }

        ITreeNode? node = _treeView.SelectedObject;

        if (node is not null)
        {
            if (GetText ("Text", "Enter text for node:", "", out string entered))
            {
                node.Children.Add (new TreeNode () { Text = entered });
                _treeView.RefreshObject (node);
            }
        }
    }

    private void AddRootNode ()
    {
        if (_treeView is null)
        {
            return;
        }

        if (GetText ("Text", "Enter text for node:", "", out string entered))
        {
            _treeView.AddObject (new TreeNode () { Text = entered });
        }
    }

    private bool GetText (string title, string label, string initialText, out string enteredText)
    {
        bool okPressed = false;

        Dialog d = new ()
        {
            Title = title,
            Buttons = [new () { Title = Strings.btnCancel }, new () { Title = Strings.btnOk }]
        };

        Label lbl = new () { X = 0, Y = 1, Text = label };

        TextField tf = new () { Text = initialText, X = 0, Y = 2, Width = Dim.Fill (0, minimumContentDim: 50) };

        d.Add (lbl, tf);
        tf.SetFocus ();

        _app?.Run (d);
        okPressed = d.Result is 1;

        d.Dispose ();

        enteredText = okPressed ? tf.Text : string.Empty;

        return okPressed;
    }

    private void Quit () { _appWindow?.RequestStop (); }

    private void RenameNode ()
    {
        if (_treeView is null)
        {
            return;
        }

        ITreeNode? node = _treeView.SelectedObject;

        if (node is not null)
        {
            if (GetText ("Text", "Enter text for node:", node.Text, out string entered))
            {
                node.Text = entered;
                _treeView.RefreshObject (node);
            }
        }
    }

    private void treeView_KeyPress (object? sender, Key obj)
    {
        if (_treeView is null)
        {
            return;
        }

        if (obj.KeyCode == Key.Delete)
        {
            ITreeNode? toDelete = _treeView.SelectedObject;

            if (toDelete is null)
            {
                return;
            }

            obj.Handled = true;

            // if it is a root object remove it
            if (_treeView.Objects is { } && _treeView.Objects.Contains (toDelete))
            {
                _treeView.Remove (toDelete);
            }
            else
            {
                ITreeNode? parent = _treeView.GetParent (toDelete);

                if (parent is null)
                {
                    MessageBox.ErrorQuery (_app!,
                                           "Could not delete",
                                           $"Parent of '{toDelete}' was unexpectedly null",
                                           "Ok"
                                          );
                }
                else
                {
                    //update the model
                    parent.Children.Remove (toDelete);

                    //refresh the tree
                    _treeView.RefreshObject (parent);
                }
            }
        }
    }
}
