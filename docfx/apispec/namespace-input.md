---
uid: Terminal.Gui.Input
summary: Keyboard, mouse, and command processing.
---

The `Input` namespace provides input handling for keyboard events, mouse interactions, and the command execution framework.

## Key Types

- **Key** - Keyboard input with modifier support (Ctrl, Alt, Shift)
- **Mouse** - Mouse event data (position, buttons, modifiers)
- **Command** - Standard application commands enum
- **KeyBinding** / **MouseBinding** - Associates input with commands
- **Responder** - Base class for input handling

## Command Pattern

Views handle input through commands:

```csharp
// 1. Add command handlers
AddCommand (Command.Accept, ctx => { /* handle */ return true; });
AddCommand (Command.Cancel, ctx => { /* handle */ return true; });

// 2. Bind keys to commands
KeyBindings.Add (Key.Enter, Command.Accept);
KeyBindings.Add (Key.Esc, Command.Cancel);

// 3. Bind mouse to commands
MouseBindings.Add (MouseFlags.Button1Clicked, Command.Accept);
```

## Key Modifiers

```csharp
Key.A.WithCtrl      // Ctrl+A
Key.A.WithAlt       // Alt+A
Key.A.WithShift     // Shift+A (uppercase)
Key.F1.WithCtrl.WithShift  // Ctrl+Shift+F1
```

## See Also

- [Keyboard Deep Dive](~/docs/keyboard.md)
- [Mouse Deep Dive](~/docs/mouse.md)
- [Command Deep Dive](~/docs/command.md)

