| Term | Meaning |
|:-----|:--------|
| **Arrange Mode** | Interactive mode activated via `Ctrl+F5` (configurable via `Application.DefaultKeyBindings` for `Command.Arrange`) that displays indicators on arrangeable views and allows keyboard-based arrangement. |
| **Arrangement** | The feature of [Layout](~/docs/layout.md) which controls how the user can use the mouse and keyboard to arrange views and enables either **Tiled** or **Overlapped** layouts. |
| **Modal** | A view run as an "application" via [Application.Run](~/api/Terminal.Gui.App.Application.yml) where `Modal == true`. Has constrained z-order with modal view at z-order 1. |
| **Movable** | A View that can be moved by the user using keyboard or mouse. Enabled by setting [ViewArrangement.Movable](~/api/Terminal.Gui.ViewBase.ViewArrangement.yml) flag. |
| **Overlapped** | Layout where SubViews have overlapping Frames with Z-order determining visual stacking. Enabled by [ViewArrangement.Overlapped](~/api/Terminal.Gui.ViewBase.ViewArrangement.yml) flag. |
| **Resizable** | A View that can be resized by the user using keyboard or mouse. Enabled by setting [ViewArrangement.Resizable](~/api/Terminal.Gui.ViewBase.ViewArrangement.yml) flag. |
| **Runnable** | A view where `Application.Run(Toplevel)` is called. Each non-modal Runnable view has its own `RunState` and operates as a self-contained application. |
| **Tiled** | Layout where SubViews typically do not overlap, with no z-order. Default layout mode set to [ViewArrangement.Fixed](~/api/Terminal.Gui.ViewBase.ViewArrangement.yml). |
