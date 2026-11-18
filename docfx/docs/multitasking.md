# Multitasking and Background Operations

_See also [Cross-platform Driver Model](drivers.md)_

Terminal.Gui applications run on a single main thread with an event loop that processes keyboard, mouse, and system events. This document explains how to properly handle background work, timers, and asynchronous operations while keeping your UI responsive.

## Threading Model

Terminal.Gui follows the standard UI toolkit pattern where **all UI operations must happen on the main thread**. Attempting to modify views or their properties from background threads will result in undefined behavior and potential crashes.

### The Golden Rule
> Always use `App?.Invoke ()` (from within a View), `app.Invoke ()`, or `app.InvokeAsync ()` to update the UI from background threads.

## Background Operations

### Await a UI update owned by a session

To wait until a background result has been applied to the UI, call `app.InvokeAsync`. Pass the session token when the callback references views owned by a runnable. The token can be obtained from `app.Begin (...)` or from `app.SessionBegun` after `app.Run (...)` starts the session.

```csharp
// Called while the window's session is running. It may have ended before this lookup.
SessionToken? owner = app.SessionStack?.FirstOrDefault (token => ReferenceEquals (token.Runnable, window));
if (owner is null)
{
    return;
}

try
{
    await Task.Run (async () =>
    {
        string result = await LoadDataAsync ().ConfigureAwait (false);
        await app.InvokeAsync (owner, () => statusLabel.Text = result).ConfigureAwait (false);
    });
}
catch (OperationCanceledException)
{
    // The session or request ended before the UI update could run.
}
catch (NotInitializedException)
{
    // The application shut down before the UI update was requested.
}
```

`InvokeAsync` completes after the callback runs on the UI thread. Queued dispatches are independent of user timers, so `TimedEvents.Remove` and `TimedEvents.StopAll` do not discard them. A canceled token, an ended owner session, the end of the final session, or application disposal cancels a callback that has not started. An `OperationCanceledException` carrying the canceled caller token also cancels the task when thrown by a running callback. Other callback exceptions fault the returned task; they do not reach the main-loop error handler, so await or inspect the task. A UI-thread call during a running session executes immediately and returns an already completed, canceled, or faulted task; an owned call whose session has not started running yet (for example, from `SessionBegun`) runs after that session starts. To cancel a specific request, pass its `CancellationToken` as the final argument. Custom `IApplication` implementations can support these extensions by implementing `IApplicationAsyncDispatcher`. The existing `Invoke` overloads remain available for calls that do not need completion or cancellation.

Calls accepted before shutdown return tasks that are canceled if still pending. After the final session ends or disposal begins, an `await` continuation that has not yet run on the UI loop, including one queued just before, resumes on a thread-pool thread because the loop is no longer pumping. Check cancellation before accessing views after such an await. Calls after shutdown begins throw `NotInitializedException`. Dispatches made between two completed sessions are canceled; to queue work for the next session, start that session first. Dispatches queued after `Init` but before the first session wait for that first session.

`Run` installs the application's synchronization context for the duration of the loop, so an `await` started on the UI thread resumes on the UI thread, and restores the caller's context when `Run` returns. `Begin` alone neither starts a loop nor installs a context: continuations resume through whatever context was already current (usually the thread pool), so use `Invoke` before touching views after an `await`. Work queued with `Invoke` or `InvokeAsync` from other threads runs only when the UI thread iterates, so a Begin-only session that never iterates will not run it.

### Using async/await (Recommended)

The preferred way to handle background work is using C#'s async/await pattern:

```csharp
private async void LoadDataButton_Clicked()
{
    loadButton.Enabled = false;
    statusLabel.Text = "Loading...";
    
    try
    {
        // This runs on a background thread
        var data = await FetchDataFromApiAsync();
        
        // This automatically returns to the main thread
        dataView.LoadData(data);
        statusLabel.Text = $"Loaded {data.Count} items";
    }
    catch (Exception ex)
    {
        statusLabel.Text = $"Error: {ex.Message}";
    }
    finally
    {
        loadButton.Enabled = true;
    }
}
```

### Using Application.Invoke()

When working with traditional threading APIs or when async/await isn't suitable:

**From within a View (recommended):**
```csharp
private void StartBackgroundWork()
{
    Task.Run(() =>
    {
        // This code runs on a background thread
        for (int i = 0; i <= 100; i++)
        {
            Thread.Sleep(50); // Simulate work
            
            // Marshal back to main thread for UI updates
            App?.Invoke(() =>
            {
                progressBar.Fraction = i / 100f;
                statusLabel.Text = $"Progress: {i}%";
            });
        }
        
        App?.Invoke(() =>
        {
            statusLabel.Text = "Complete!";
        });
    });
}
```

**Using IApplication instance:**
```csharp
private void StartBackgroundWork(IApplication app)
{
    Task.Run(() =>
    {
        // This code runs on a background thread
        for (int i = 0; i <= 100; i++)
        {
            Thread.Sleep(50); // Simulate work
            
            // Marshal back to main thread for UI updates
            app.Invoke(() =>
            {
                progressBar.Fraction = i / 100f;
                statusLabel.Text = $"Progress: {i}%";
            });
        }
        
        app.Invoke(() =>
        {
            statusLabel.Text = "Complete!";
        });
    });
}
```

## Timers

Use timers for periodic updates like clocks, status refreshes, or animations:

```csharp
public class ClockView : View
{
    private Label timeLabel;
    private object timerToken;
    
    public ClockView()
    {
        timeLabel = new Label { Text = DateTime.Now.ToString("HH:mm:ss") };
        Add(timeLabel);
        
        // Update every second using the View's App property
        timerToken = App?.AddTimeout(
            TimeSpan.FromSeconds(1), 
            UpdateTime
        );
    }
    
    private bool UpdateTime()
    {
        timeLabel.Text = DateTime.Now.ToString("HH:mm:ss");
        return true; // Continue timer
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing && timerToken != null)
        {
            App?.RemoveTimeout(timerToken);
        }
        base.Dispose(disposing);
    }
}
```

### Timer Best Practices

- **Always remove timers** when disposing views to prevent memory leaks
- **Return `true`** from timer callbacks to continue, `false` to stop
- **Keep timer callbacks fast** - the application loop runs them on the main thread, while direct `RunTimers` calls execute on the calling thread
- **Use appropriate intervals** - too frequent updates can impact performance


## Common Patterns

### Progress Reporting

```csharp
private async void ProcessFiles()
{
    var files = Directory.GetFiles(folderPath);
    progressBar.Fraction = 0;
    
    for (int i = 0; i < files.Length; i++)
    {
        await ProcessFileAsync(files[i]);
        
        // Update progress on main thread
        progressBar.Fraction = (float)(i + 1) / files.Length;
        statusLabel.Text = $"Processed {i + 1} of {files.Length} files";
        
        // Allow UI to update
        await Task.Yield();
    }
}
```

### Cancellation Support

```csharp
private CancellationTokenSource cancellationSource;

private async void StartLongOperation()
{
    cancellationSource = new CancellationTokenSource();
    cancelButton.Enabled = true;
    
    try
    {
        await LongRunningOperationAsync(cancellationSource.Token);
        statusLabel.Text = "Operation completed";
    }
    catch (OperationCanceledException)
    {
        statusLabel.Text = "Operation cancelled";
    }
    finally
    {
        cancelButton.Enabled = false;
    }
}

private void CancelButton_Clicked()
{
    cancellationSource?.Cancel();
}
```

### Responsive UI During Blocking Operations

```csharp
private async void ProcessLargeDataset()
{
    var data = GetLargeDataset();
    var batchSize = 100;
    
    for (int i = 0; i < data.Count; i += batchSize)
    {
        // Process a batch
        var batch = data.Skip(i).Take(batchSize);
        ProcessBatch(batch);
        
        // Update UI and yield control
        progressBar.Fraction = (float)i / data.Count;
        await Task.Yield(); // Allows UI events to process
    }
}
```

## Common Mistakes to Avoid

### ❌ Don't: Update UI from background threads
```csharp
Task.Run(() =>
{
    label.Text = "This will crash!"; // Wrong!
});
```

### ✅ Do: Use App.Invoke() or app.Invoke()
```csharp
Task.Run(() =>
{
    // From within a View:
    App?.Invoke(() =>
    {
        label.Text = "This is safe!"; // Correct!
    });
    
    // Or with IApplication instance:
    // app.Invoke(() => { label.Text = "This is safe!"; });
});
```

### ❌ Don't: Forget to clean up timers
```csharp
// Memory leak - timer keeps running after view is disposed
// From within a View:
App?.AddTimeout(TimeSpan.FromSeconds(1), UpdateStatus);

// Or with IApplication instance:
app.AddTimeout(TimeSpan.FromSeconds(1), UpdateStatus);
```

### ✅ Do: Remove timers in Dispose
```csharp
protected override void Dispose(bool disposing)
{
    if (disposing && timerToken != null)
    {
        // From within a View, use App property
        App?.RemoveTimeout(timerToken);
        
        // Or with IApplication instance:
        // app.RemoveTimeout(timerToken);
    }
    base.Dispose(disposing);
}
```

## Performance Considerations

- **Batch UI updates** when possible instead of updating individual elements
- **Use appropriate timer intervals** - 100ms is usually the maximum useful rate
- **Yield control** in long-running operations with `await Task.Yield()`
- **Consider using `ConfigureAwait(false)`** for non-UI async operations
- **Profile your application** to identify performance bottlenecks

## See Also

- [Events](events.md) - Event handling patterns
- [Keyboard Input](keyboard.md) - Keyboard event processing
- [Mouse Input](mouse.md) - Mouse event handling
- [Configuration Management](config.md) - Application settings and state

## Output above a live inline region

To print completed styled text above an inline UI, await `app.PrintAboveAsync(...)`.
See [Printing above an inline application](inline-output.md) for ordering, cancellation,
supported output, and partial-write behavior.
