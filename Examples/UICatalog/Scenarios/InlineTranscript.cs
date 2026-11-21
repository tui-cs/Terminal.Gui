#nullable enable
using TgAttribute = Terminal.Gui.Drawing.Attribute;

namespace UICatalog.Scenarios;

/// <summary>Demonstrates awaited styled output above a composer that grows and shrinks with a live preview.</summary>
[ScenarioMetadata ("Inline Transcript", "Print completed messages above a live composer with a collapsible preview.")]
[ScenarioCategory ("AppModel")]
[ScenarioCategory ("Threading")]
public sealed class InlineTranscript : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.AppModel = AppModel.Inline;
        app.Mouse.IsMouseDisabled = true;
        app.Init (DriverRegistry.Names.ANSI);
        using CancellationTokenSource lifetime = new ();
        using Runnable composer = new () { Width = Dim.Fill (), Height = Dim.Auto (DimAutoStyle.Content, minimumContentDim: 3) };
        Label title = new () { Text = "Inline transcript · Enter sends · F2 mouse · F5 preview · F6 commit · Esc exits", Width = Dim.Fill (), Height = 1 };
        TextField input = new () { Y = 1, Width = Dim.Fill () };
        Label status = new () { Y = 2, Text = "Mouse off: native select/copy. F2: UI mouse.", Width = Dim.Fill (), Height = 1 };
        using Label preview = new () { Y = 3, Text = "LIVE PREVIEW row 1\nLIVE PREVIEW row 2\nLIVE PREVIEW row 3\nLIVE PREVIEW row 4", Width = Dim.Fill () };
        composer.Add (title, input, status);
        Task producer = Task.CompletedTask;
        Task latestSubmission = Task.CompletedTask;
        SessionToken? owner = null;
        composer.KeyDown += (_, key) =>
        {
            if (key == Key.F5)
            {
                if (preview.SuperView is { })
                {
                    composer.Remove (preview);
                    key.Handled = true;
                    return;
                }

                composer.Add (preview);
                key.Handled = true;
                return;
            }

            if (key == Key.F6 && owner is { } && preview.SuperView is { })
            {
                composer.Remove (preview);
                latestSubmission = SubmitAsync (owner, [new InlineOutputLine ([new InlineOutputSpan ("preview: completed preview")])]);
                key.Handled = true;
                return;
            }

            if (key != Key.F2)
            {
                return;
            }

            app.Mouse.IsMouseDisabled = !app.Mouse.IsMouseDisabled;
            status.Text = app.Mouse.IsMouseDisabled
                              ? "Mouse off: native select/copy. F2: UI mouse."
                              : "Mouse on: UI handles mouse. F2: native select.";
            key.Handled = true;
        };

        EventHandler<SessionTokenEventArgs> sessionBegun = (_, e) =>
        {
            owner = e.State;
            producer = ProduceAsync (owner);
        };

        app.SessionBegun += sessionBegun;
        input.Accepted += (_, _) =>
        {
            if (owner is null || input.Text.Length == 0)
            {
                return;
            }

            string message = input.Text;
            input.Text = string.Empty;
            latestSubmission = SubmitAsync (owner, [new InlineOutputLine ([new InlineOutputSpan ("you: ", new TgAttribute (Color.Cyan, Color.None)), new InlineOutputSpan (message)])]);
        };

        try
        {
            app.Run (composer);
        }
        finally
        {
            app.SessionBegun -= sessionBegun;
            composer.Remove (preview);
            lifetime.Cancel ();
            Task.WhenAll (producer, latestSubmission).GetAwaiter ().GetResult ();
        }

        async Task SubmitAsync (SessionToken session, IReadOnlyList<InlineOutputLine> lines)
        {
            try
            {
                await app.PrintAboveAsync (session, lines, lifetime.Token).ConfigureAwait (false);
            }
            catch (OperationCanceledException)
            {
                // Ending the owning session cancels output that never reached a render boundary.
            }
            catch (Exception error)
            {
                await ReportAndStopAsync (session, error).ConfigureAwait (false);
            }
        }

        async Task ProduceAsync (SessionToken session)
        {
            try
            {
                for (var i = 0; i < 24; i++)
                {
                    await app.PrintAboveAsync (session,
                                              [new InlineOutputLine ([new InlineOutputSpan ($"message {i:D3}: ", new TgAttribute (Color.Green, Color.None, TextStyle.Bold)),
                                                                      new InlineOutputSpan ("completed output")])],
                                              lifetime.Token).ConfigureAwait (false);
                    await Task.Delay (150, lifetime.Token).ConfigureAwait (false);
                }
            }
            catch (OperationCanceledException)
            {
                // The caller can close the composer while a producer is awaiting its next insertion.
            }
            catch (Exception error)
            {
                await ReportAndStopAsync (session, error).ConfigureAwait (false);
            }
        }

        async Task ReportAndStopAsync (SessionToken session, Exception error)
        {
            try
            {
                await app.InvokeAsync (session, () =>
                {
                    status.Text = error.Message;
                    app.RequestStop ();
                }, lifetime.Token).ConfigureAwait (false);
            }
            catch (OperationCanceledException)
            {
                // Teardown already owns pending UI work.
            }
            catch (NotInitializedException)
            {
                // The application was disposed before the diagnostic could be dispatched.
            }
        }
    }
}
