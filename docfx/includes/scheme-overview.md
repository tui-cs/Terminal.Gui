A Scheme is named a mapping from `VisualRole`s (e.g. `VisualRole.Focus`) to `Attribute`s, defining how a `View` should look based on its purpose (e.g. Menu or Dialog). @Terminal.Gui.SchemeManager.Schemes is a dictionary of `Scheme`s, indexed by name.

A Scheme defines how Views look based on their semantic purpose. The following schemes are supported:

| Scheme Name | Description |
|:-----|:--------|
| **Base** | The base scheme used for most Views. |
| **Dialog** | The dialog scheme; used for Dialog, MessageBox, and other views dialog-like views. |
| **Error** | The scheme for showing errors, such as in `ErrorQuery`. |
| **Menu** | The menu scheme; used for Terminal.Gui.Menu, MenuBar, and StatusBar. |
| **Accent** | The accent scheme; a secondary/alternate scheme for visual distinction. Used for panels, event logs, and any view that needs visual separation. |

@Terminal.Gui.SchemeManager manages the set of available schemes and provides a set of convenience methods for getting the current scheme and for overriding the default values for these schemes.

```csharp
Scheme dialogScheme = SchemeManager.GetScheme (Schemes.Dialog);
```

[TuiConfigurationBuilder](~/docs/config.md) can be used to override the default values for these schemes and add additional schemes. 
