using System;
using System.Collections.Generic;
using System.Data;
using Terminal.Gui;
using System.Linq;
using System.Globalization;
using static Terminal.Gui.TableView;
using System.Diagnostics;

namespace UICatalog.Scenarios {

	[ScenarioMetadata (Name: "ProcessTable", Description: "Demonstrates TableView with the currently running processes.")]
	[ScenarioCategory ("TableView")]
	public class ProcessTable : Scenario {
		TableView tableView;
		public override void Setup ()
		{
			Win.Title = this.GetName ();
			Win.Y = 1; // menu
			Win.Height = Dim.Fill (1); // status bar
			Application.Top.LayoutSubviews ();

			this.tableView = new TableView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (1),
			};

			// First time
			CreateProcessTable ();

			// Then every second
			Application.MainLoop.AddTimeout (TimeSpan.FromSeconds (1),
				(s) => {
					CreateProcessTable ();
					return true;
				});

			Win.Add (tableView);

		}

		private void CreateProcessTable ()
		{
			var ro = tableView.RowOffset;
			var co = tableView.ColumnOffset;
			tableView.Table = new EnumerableTableSource<Process> (Process.GetProcesses (),
				new Dictionary<string, Func<Process, object>>() {
					{ "ID",(p)=>p.Id},
					{ "Name",(p)=>p.ProcessName},
					{ "Threads",(p)=>p.Threads.Count},
					{ "Virtual Memory",(p)=>p.VirtualMemorySize64},
					{ "Working Memory",(p)=>p.WorkingSet64},
				});

			tableView.RowOffset = ro;
			tableView.ColumnOffset = co;
			tableView.EnsureValidScrollOffsets ();
		}
	}
}
