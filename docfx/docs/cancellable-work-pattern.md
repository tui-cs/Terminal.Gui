# Cancellable Work Pattern (CWP)

The *Cancellable Work Pattern* (CWP) is a design pattern for structuring workflows that can be executed in a default manner, modified by external code or subclasses, or cancelled entirely. Unlike traditional inheritance-based approaches that rely on overriding virtual methods (which often require subclasses to understand base class implementation details), CWP prioritizes **events for loose coupling**, supplemented by optional **virtual methods** for flexibility.

This document provides a conceptual definition of the pattern, its components, and goals, illustrated through generic examples applicable to any .NET framework or application.

> [!TIP]
> For Terminal.Gui-specific implementation details and practical guidance, see the [Events Deep Dive](events.md).

## Definition

The *Cancellable Work Pattern* is a design pattern for executing a structured workflow with one or more phases, where each phase can:

- **Proceed** in a default manner
- **Be modified** by external code or subclasses
- **Be cancelled** to halt further processing

The pattern uses events as the primary mechanism for notification and customization, supplemented by virtual methods for subclassing when needed. It is a specialization of the **Observer Pattern**, extended with structured workflows, explicit cancellation mechanisms, and context-aware notifications. It also incorporates elements of the **Template Method Pattern** (via virtual methods) and **Pipeline Pattern** (via sequential phases).

## Lexicon and Taxonomy

[!INCLUDE [Events Lexicon](~/includes/events-lexicon.md)]

## Core Concept

At its core, CWP defines a workflow as a sequence of one or more distinct **phases**, each representing a unit of work within a larger operation. For each phase, the pattern provides mechanisms to:

- **Execute Default Behavior**: A predefined implementation that executes if no external intervention occurs, ensuring the system remains functional out of the box.
- **Allow Customization**: Through event subscriptions or method overrides, external code or subclasses can inject custom logic to alter the phase's behavior without needing to replace the entire workflow.
- **Support Cancellation**: An explicit mechanism to halt the execution of a phase or the entire workflow, preventing further processing when certain conditions are met (e.g., user intervention, error states, or logical constraints).

This triadic structure—default execution, customization, and cancellation—distinguishes CWP from simpler event-driven or inheritance-based approaches. It ensures that workflows are both robust (via defaults) and flexible (via customization and cancellation).

### Structural Components

The Cancellable Work Pattern comprises the following components:

1. **Workflow Container**: The class that encapsulates the overall workflow, defining the sequence of phases and orchestrating their execution (e.g., a `DocumentProcessor` managing parsing, validation, and saving phases).

2. **Phases**: Individual steps within the workflow, each representing a discrete unit of work. Each phase has a default implementation and points for intervention (e.g., a "Validation" phase within a document processing workflow).

3. **Notification Mechanisms**: Events or callbacks that notify external observers of a phase's impending execution, allowing them to intervene. These are typically implemented as:
   - **Events**: `public event EventHandler<TEventArgs>?` for external subscribers
   - **Virtual Methods**: `protected virtual bool OnPhaseExecuting()` for subclasses

4. **Cancellation Flags**: Boolean indicators within event arguments that signal whether a phase or workflow should be halted (commonly named `Cancel` or `Handled`).

5. **Context Objects**: Data structures passed to notification handlers, providing relevant state or parameters about the phase, enabling informed decision-making by custom logic.

### Operational Flow

The operational flow of CWP follows a consistent pattern for each phase within a workflow:

1. **Pre-Phase Notification**: Before executing a phase's default behavior, the workflow container calls a virtual method and/or raises an event to notify potential observers or subclasses. This step allows for preemptive customization or cancellation.

2. **Cancellation Check**: If the notification mechanism indicates cancellation (e.g., a return value of `true` from a virtual method or a `Cancel` flag set in event args), the phase is aborted, and control may return or move to the next phase, depending on the workflow design.

3. **Default Execution**: If no cancellation occurs, the phase's default behavior is executed. This ensures the workflow progresses as intended in the absence of external intervention.

4. **Post-Phase Notification (Optional)**: In some implementations, a secondary notification occurs after the phase completes, informing observers of the outcome or updated state.

This flow repeats for each phase, allowing granular control over complex operations. Importantly, CWP decouples the workflow's structure from its customization, as external code can subscribe to events without needing to subclass or understand the container's internal logic.

```
┌─────────────────────────────────────────────────────────────┐
│                    CWP Operational Flow                     │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────────────┐                                   │
│  │ 1. Call Virtual      │──► returns true? ──► CANCELLED    │
│  │    OnXxxExecuting()  │                                   │
│  └──────────┬───────────┘                                   │
│             │ returns false                                 │
│             ▼                                               │
│  ┌──────────────────────┐                                   │
│  │ 2. Raise Event       │──► args.Cancel? ──► CANCELLED     │
│  │    XxxExecuting      │                                   │
│  └──────────┬───────────┘                                   │
│             │ not cancelled                                 │
│             ▼                                               │
│  ┌──────────────────────┐                                   │
│  │ 3. Execute Default   │                                   │
│  │    Behavior          │                                   │
│  └──────────┬───────────┘                                   │
│             │                                               │
│             ▼                                               │
│  ┌──────────────────────┐                                   │
│  │ 4. Call Virtual      │                                   │
│  │    OnXxxExecuted()   │                                   │
│  └──────────┬───────────┘                                   │
│             │                                               │
│             ▼                                               │
│  ┌──────────────────────┐                                   │
│  │ 5. Raise Event       │                                   │
│  │    XxxExecuted       │                                   │
│  └──────────────────────┘                                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Advantages

- **Flexibility**: Developers can modify specific phases without altering the entire workflow, supporting a wide range of use cases from minor tweaks to complete overrides.

- **Decoupling**: By prioritizing events over inheritance, CWP reduces tight coupling between base and derived classes, adhering to principles of loose coupling in software design.

- **Robustness**: Default behaviors ensure the system remains operational even if no customization is provided, reducing the risk of incomplete implementations.

- **Control**: Cancellation mechanisms provide precise control over workflow execution, critical in interactive systems where user input or state changes may necessitate halting operations.

## Limitations

- **Complexity**: Multi-phase workflows can become intricate, especially when numerous events and cancellation points are involved, potentially leading to debugging challenges.

- **Performance Overhead**: Raising events and checking cancellation flags for each phase introduces minor performance costs, which may accumulate in high-frequency operations.

- **Learning Curve**: Understanding the pattern's structure and knowing when to use events versus overrides requires familiarity, which may pose a barrier to novice developers.

## Applicability

CWP is particularly suited to domains where workflows must balance standardization with adaptability, such as:

- **User interface frameworks** (handling input, rendering, commands)
- **Document processing systems** (parsing, validation, transformation)
- **Game engines** (update loops, collision handling, rendering)
- **Workflow automation systems** (approval processes, state machines)

It excels in scenarios where operations are inherently interruptible—such as responding to user input, processing documents, or managing state transitions—and where multiple components or developers need to collaborate on the same process without tight dependencies.

## Implementation Examples

The following examples demonstrate CWP in different contexts using generic, fictional classes.

### Example 1: Workflow Phase (Document Processing)

A `DocumentProcessor` class processes documents through multiple phases. Each phase can be customized or cancelled.

```csharp
public class DocumentProcessor
{
    // Event for external subscribers
    public event EventHandler<ProcessingEventArgs>? Validating;

    // Virtual method for subclasses
    protected virtual bool OnValidating(ProcessingEventArgs args)
    {
        // Return true to cancel validation
        return false;
    }

    private void DoValidation(Document document)
    {
        ProcessingEventArgs args = new (document);

        // Step 1: Call virtual method (subclass gets first chance)
        if (OnValidating(args))
        {
            return; // Cancelled by subclass
        }

        // Step 2: Raise event (external subscribers get a chance)
        Validating?.Invoke(this, args);
        if (args.Cancel)
        {
            return; // Cancelled by event subscriber
        }

        // Step 3: Execute default behavior
        ValidateDocument(document);
    }

    private void ValidateDocument(Document document)
    {
        // Default validation logic
    }
}
```

**Key Points:**
- Virtual method `OnValidating` is called first, giving subclasses priority
- Event `Validating` is raised second, allowing external customization
- Either can cancel by returning `true` or setting `args.Cancel = true`
- Default behavior `ValidateDocument` only runs if not cancelled

### Example 2: Input Handling

An `InputHandler` class processes input events, allowing customization of how input is handled.

```csharp
public class InputHandler
{
    public event EventHandler<InputEventArgs>? InputReceived;

    protected virtual bool OnInputReceived(InputEventArgs args)
    {
        return false;
    }

    public bool ProcessInput(InputEventArgs args)
    {
        // Virtual method for subclasses
        if (OnInputReceived(args))
        {
            return true; // Handled by subclass
        }

        // Event for external subscribers
        InputReceived?.Invoke(this, args);
        if (args.Handled)
        {
            return true; // Handled by subscriber
        }

        // Default behavior: dispatch to appropriate handler
        return DispatchToDefaultHandler(args);
    }

    private bool DispatchToDefaultHandler(InputEventArgs args)
    {
        // Default input handling logic
        return false;
    }
}
```

**Key Points:**
- Uses `Handled` property (common for input events) instead of `Cancel`
- Returns `bool` to indicate whether the input was processed
- Subclasses can override `OnInputReceived` for custom behavior

### Example 3: Command Execution

A `CommandExecutor` class executes commands with cancellable pre-execution hooks.

```csharp
public class CommandExecutor
{
    public event EventHandler<CommandEventArgs>? Executing;

    protected virtual bool OnExecuting(CommandEventArgs args)
    {
        return false;
    }

    public bool? Execute(Command command)
    {
        CommandEventArgs args = new () { Command = command };

        // Pre-execution notification
        if (OnExecuting(args) || args.Handled)
        {
            return true; // Cancelled or handled by subclass
        }

        Executing?.Invoke(this, args);
        if (args.Handled)
        {
            return true; // Handled by subscriber
        }

        // Default execution
        return command.Execute();
    }
}
```

**Key Points:**
- Combines virtual method return value with `args.Handled` check
- Returns `bool?` to distinguish between handled (`true`), not handled (`false`), and no subscribers (`null`)

### Example 4: Property Changes (Cancellable)

A settings facade manages configuration properties with cancellable change notifications.

```csharp
public class ThemeSettingsExample
{
    private string _theme = "Default";

    // Pre-change event (cancellable)
    public event EventHandler<ValueChangingEventArgs<string>>? ThemeChanging;
    // Post-change event (notification only)
    public event EventHandler<ValueChangedEventArgs<string>>? ThemeChanged;

    protected virtual bool OnThemeChanging(ValueChangingEventArgs<string> args)
    {
        return false; // Return true to cancel
    }

    protected virtual void OnThemeChanged(ValueChangedEventArgs<string> args)
    {
        // React to the completed change
    }

    public string Theme
    {
        get => _theme;
        set
        {
            if (_theme == value)
            {
                return; // No change
            }

            string currentValue = _theme;
            ValueChangingEventArgs<string> changingArgs = new (currentValue, value);

            // Pre-change: virtual method
            if (OnThemeChanging(changingArgs) || changingArgs.Handled)
            {
                return; // Cancelled by subclass
            }

            // Pre-change: event
            ThemeChanging?.Invoke(this, changingArgs);
            if (changingArgs.Handled)
            {
                return; // Cancelled by subscriber
            }

            // Apply the change (may use modified value from args)
            _theme = changingArgs.NewValue;

            // Post-change notifications
            ValueChangedEventArgs<string> changedArgs = new (currentValue, _theme);
            OnThemeChanged(changedArgs);
            ThemeChanged?.Invoke(this, changedArgs);
        }
    }
}
```

**Key Points:**
- Two events: `ThemeChanging` (cancellable, pre-change) and `ThemeChanged` (notification, post-change)
- `ValueChangingEventArgs` allows subscribers to modify the new value via `args.NewValue`
- `ValueChangedEventArgs` provides old and new values for notification
- Post-change virtual method `OnThemeChanged` returns `void` (not cancellable)

## Naming Conventions

Consistent naming is critical for CWP implementations:

| Element | Convention | Example |
|---------|------------|---------|
| Pre-change/execution event | `<Action>ing` | `Validating`, `ThemeChanging` |
| Post-change/execution event | `<Action>ed` | `Validated`, `ThemeChanged` |
| Pre-change virtual method | `On<Action>ing` | `OnValidating`, `OnThemeChanging` |
| Post-change virtual method | `On<Action>ed` | `OnValidated`, `OnThemeChanged` |
| Handled property | `Handled` | `args.Handled = true` |

## Event Arguments

### For Cancellable Operations

```csharp
public class ValueChangingEventArgs<T> : EventArgs
{
    public T CurrentValue { get; }
    public T NewValue { get; set; } // Can be modified
    public bool Handled { get; set; }

    public ValueChangingEventArgs(T currentValue, T newValue)
    {
        CurrentValue = currentValue;
        NewValue = newValue;
    }
}
```

### For Post-Change Notifications

```csharp
public class ValueChangedEventArgs<T> : EventArgs
{
    public T OldValue { get; }
    public T NewValue { get; }

    public ValueChangedEventArgs(T oldValue, T newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }
}
```

### For Input/Command Handling

```csharp
public class InputEventArgs : EventArgs
{
    public bool Handled { get; set; }
    // Additional context properties...
}
```

## Implementation in Terminal.Gui

Terminal.Gui implements CWP consistently across the framework. For detailed, Terminal.Gui-specific guidance including:

- Property changes with `CWPPropertyHelper`
- Workflow execution with `CWPWorkflowHelper`
- View rendering, keyboard input, and command handling
- Practical code templates and recipes

See the [Events Deep Dive](events.md).

