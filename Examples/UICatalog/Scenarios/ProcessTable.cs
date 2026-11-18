using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ProcessTable", "Demonstrates TableView with the currently running processes.")]
[ScenarioCategory ("TableView")]
public class ProcessTable : Scenario
{
    private TableView _tableView;

    public override void Main ()
    {

        using IApplication app = Application.Create ();
        app.Init ();

        using Window win = new ()
        {
            Title = GetName (),
            Y = 1, // menu
            Height = Dim.Fill (1) // status bar
        };

        _tableView = new TableView { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill (1) };

        // First time
        CreateProcessTable ();

        // Then every second
        app.AddTimeout (
                                TimeSpan.FromSeconds (1),
                                () =>
                                {
                                    CreateProcessTable ();

                                    return true;
                                }
                               );

        win.Add (_tableView);

        app.Run (win);
    }

    private void CreateProcessTable ()
    {
        int ro = _tableView.RowOffset;
        int co = _tableView.ColumnOffset;

        _tableView.Table = new EnumerableTableSource<Process> (
                                                              Process.GetProcesses (),
                                                              new Dictionary<string, Func<Process, object>>
                                                              {
                                                                  { "ID", p => p.Id },
                                                                  { "Name", p => p.ProcessName },
                                                                  { "Threads", p => p.Threads.Count },
                                                                  { "Virtual Memory", p => p.VirtualMemorySize64 },
                                                                  { "Working Memory", p => p.WorkingSet64 }
                                                              }
                                                             );

        _tableView.RowOffset = ro;
        _tableView.ColumnOffset = co;
        _tableView.EnsureValidScrollOffsets ();
    }
}
