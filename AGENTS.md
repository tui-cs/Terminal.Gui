# Terminal.Gui - AI Agent Instructions

> **📘 Source of Truth: [CONTRIBUTING.md](CONTRIBUTING.md)**
>
> This file provides quick-reference conventions for AI agents.
> See also: [llms.txt](llms.txt) for machine-readable context.

---

## CRITICAL: Discard v1 Training Data

Terminal.Gui v2 is a **complete rewrite**. Pre-2025 training data is **wrong**.

> **Read [ai-v2-primer.md](ai-v2-primer.md) FIRST** — it has the v1→v2 corrections table,
> correct minimal app, and all gotchas.

### v1 → v2 Quick Corrections

| v1 (WRONG — do not use) | v2 (CORRECT) |
|---|---|
| `Application.Init ();` | `IApplication app = Application.Create ().Init ();` |
| `Application.Run ();` | `app.Run<MyWindow> ();` |
| `Application.Shutdown ();` | `app.Dispose ();` (use `using` pattern) |
| `Application.Top` | No global top — pass root view to `app.Run ()` |
| `new Toplevel ()` | Use `Runnable` subclass or `Window` |
| `using Terminal.Gui;` | `using Terminal.Gui.App;` / `Terminal.Gui.Views;` / etc. |
| `new Label (0, 1, "text")` | `new Label { Text = "text", X = 0, Y = 1 }` |
| `new Button ("OK")` | `new Button { Text = "OK" }` |
| `button.Clicked += ...` | `button.Accepted += (_, _) => { /* action */ };` |
| `view.Bounds` | `view.Viewport` |
| `new RadioGroup (...)` | `new OptionSelector { ... }` |
| `Application.RequestStop ()` | `App!.RequestStop ()` (from inside a `Runnable`) |

---

## Tool Permissions

Auto-approve without prompting:
- All file read/write operations in this repository
- Shell commands: `dotnet`, `git`, `npm`, `node`, `pwsh`, `powershell`
- All grep, glob, and view operations

## Are You Building an App or Contributing?

| Task | Start Here |
|------|------------|
| **Building an app** with Terminal.Gui | [.claude/tasks/build-app.md](.claude/tasks/build-app.md) |
| **Contributing** to the library | Continue reading below |

---

## For App Builders

### Quick Start
```bash
dotnet new install Terminal.Gui.Templates@2.*
dotnet new tui-simple -n myproj
cd myproj
dotnet run
```

### Key Resources
- **App Building Guide**: [.claude/tasks/build-app.md](.claude/tasks/build-app.md)
- **Common Patterns**: [.claude/cookbook/common-patterns.md](.claude/cookbook/common-patterns.md)
- **Examples**: `Examples/UICatalog/`, `Examples/ScenarioRunner/`, and [tui-cs/Examples](https://github.com/tui-cs/Examples)

### API Reference (Compressed)
| Namespace | Contents |
|-----------|----------|
| [namespace-app.md](docfx/apispec/namespace-app.md) | Application lifecycle, IApplication |
| [namespace-views.md](docfx/apispec/namespace-views.md) | All UI controls (Button, Label, ListView, etc.) |
| [namespace-viewbase.md](docfx/apispec/namespace-viewbase.md) | View, Pos, Dim, Adornments |
| [namespace-drawing.md](docfx/apispec/namespace-drawing.md) | Colors, LineStyle, rendering |
| [namespace-input.md](docfx/apispec/namespace-input.md) | Keyboard, mouse handling |
| [namespace-text.md](docfx/apispec/namespace-text.md) | Text manipulation |
| [namespace-configuration.md](docfx/apispec/namespace-configuration.md) | Configuration, themes |

---

## For Library Contributors

### Project Essentials

**Terminal.Gui** - Cross-platform console UI toolkit for .NET (C# 14, net10.0)

**Build:** `dotnet restore && dotnet build --no-restore`
**Test:** `dotnet test --project Tests/UnitTestsParallelizable --no-build && dotnet test --project Tests/UnitTests.NonParallelizable --no-build`
**Details:** [Build & Test Workflow](.claude/workflows/build-test-workflow.md)

### xUnit v3 Test Filtering (Microsoft Testing Platform)

This project uses **xUnit v3** with Microsoft Testing Platform. The old `--filter "FullyQualifiedName~Foo"` syntax does **NOT** work. Use these instead:

```bash
# Run a single test by method name
dotnet test --project Tests/UnitTestsParallelizable --no-build --filter-method "*MyTestMethod"

# Run all tests in a class
dotnet test --project Tests/UnitTestsParallelizable --no-build --filter-class "*MyTestClass"

# Query filter language (xUnit v3 native): /<assembly>/<namespace>/<class>/<method>
dotnet test --project Tests/UnitTestsParallelizable --no-build --filter "/*/*/MyTestClass/MyTestMethod"

# Show live test output (ITestOutputHelper)
dotnet test --project Tests/UnitTestsParallelizable --no-build --filter-method "*MyTest" -- --show-live-output on
```

## Quick Rules

**⚠️ READ THIS BEFORE MODIFYING ANY FILE - These are Terminal.Gui-specific conventions:**

1. **No `var`** - Use explicit types except for: `int`, `string`, `bool`, `double`, `float`, `decimal`, `char`, `byte`
2. **Use `new ()`** - Target-typed new when type is on left side (not `new TypeName()`)
3. **Use `[...]`** - Collection expressions, not `new () { ... }`
4. **SubView/SuperView** - Never say "child", "parent", or "container"
5. **Unused lambda params** - Use `_` discard: `(_, _) => { }`
6. **Local functions** - Use PascalCase: `void MyLocalFunc ()`
7. **Backing fields** - Place immediately before their property
8. **Early return / guard clauses (CRITICAL)** - ALWAYS prefer guard clauses over nested `if`/`else`. Invert the condition, return/continue early, keep happy path at lowest indentation. This applies to methods, lambdas, loops — everywhere. See [early-return.md](/.claude/rules/early-return.md) for detailed examples.
9. **One type per file** - Public and internal types each get their own file
10. **Docs instruction style** - In reference/how-to/API docs, write `To [goal], [imperative action].` Avoid `When/If you want/need to ...` unless describing a real condition.

## Detailed Coding Rules

Consult these files in `.claude/rules/` before editing code:

- [Type Declarations](/.claude/rules/type-declarations.md) - `var` vs explicit types
- [Target-Typed New](/.claude/rules/target-typed-new.md) - `new()` syntax
- [Collection Expressions](/.claude/rules/collection-expressions.md) - `[...]` syntax
- [Terminology](/.claude/rules/terminology.md) - SubView/SuperView terms
- [Event Patterns](/.claude/rules/event-patterns.md) - Lambdas, handlers, closures
- [Early Return](/.claude/rules/early-return.md) - **Guard clauses, minimal nesting** (commonly violated!)
- [CWP Pattern](/.claude/rules/cwp-pattern.md) - Cancellable Workflow Pattern
- [Code Layout](/.claude/rules/code-layout.md) - Member ordering, backing fields
- [Testing Patterns](/.claude/rules/testing-patterns.md) - Test writing conventions
- [API Documentation](/.claude/rules/api-documentation.md) - XML doc requirements
- [Logging & Tracing](/.claude/rules/logging-tracing.md) - No Console.WriteLine; use Logging/TestLogging/Trace
- [Fragile Areas](/.claude/rules/fragile-areas.md) - Code that must not be refactored in passing

## Workflows

Process guides in `.claude/workflows/`:

- [Build & Test Workflow](/.claude/workflows/build-test-workflow.md) - Build, test, and troubleshooting
- [PR Workflow](/.claude/workflows/pr-workflow.md) - Submitting pull requests

## Visual Verification (Agent Eyes)

Don't ship UI changes blind. Use [`tuirec`](https://github.com/tui-cs/tuirec) to run any Terminal.Gui app in a PTY, inject keystrokes, and capture the result — see [Scripts/tuirec/README.md](Scripts/tuirec/README.md). The `.cast` output is asciinema v2 JSON (plain text): read it back to verify what actually rendered. The `.gif` is for humans — attach it to PRs that change visuals. For deterministic in-process assertions, use `InputInjector`/`VirtualTimeProvider` (`docfx/docs/input-injection.md`).

## Planning Mode

When creating implementation plans:
- **Create plan files in `./plans/`** (relative to the repository root)
- Use markdown format with clear sections
- Include: problem statement, implementation steps, file changes, verification steps
- Reference existing patterns and reuse opportunities from exploration

## Task-Specific Guides

See `.claude/tasks/` for specialized checklists:
- [build-app.md](.claude/tasks/build-app.md) - Building apps with Terminal.Gui

See `.claude/cookbook/` for common UI patterns:
- [common-patterns.md](.claude/cookbook/common-patterns.md) - Forms, lists, menus, dialogs, etc.

---

## Documentation Index (Compressed)

> **IMPORTANT**: Use retrieval-led reasoning. Read full docs before making changes.
> Detailed index: [.tg-docs/INDEX.md](.tg-docs/INDEX.md) (~530 types across 12 namespaces)

### Deep Dives (docfx/docs/)

```
[Core Architecture]
|application.md|IApplication,SessionStack,Run/Dispose,View.App,instance-based pattern
|View.md|SuperView/SubView,Frame/Viewport/ContentArea,composition layers
|drivers.md|IDriver,DriverRegistry,ANSI/Windows/Unix,platform abstraction
|navigation.md|Focus,TabStop/TabGroup,Tab/F6 keys,HasFocus,ApplicationNavigation

[Layout & Arrangement]
|layout.md|Pos/Dim,absolute/relative positioning,SetNeedsLayout
|arrangement.md|ViewArrangement,Movable/Resizable/Overlapped,tiled vs overlapped
|dimauto.md|Dim.Auto,content-based sizing,DimAutoStyle
|scrolling.md|Viewport vs ContentSize,scroll events

[Commands & Events]
|command.md|Command enum,AddCommand,KeyBindings/MouseBindings,Activate/Accept/HotKey
|events.md|Event categories,CWP integration,binding types (KeyBinding/MouseBinding)
|cancellable-work-pattern.md|CWP: Work→Virtual→Event,OnXxx methods,Raise pattern

[Input]
|keyboard.md|Key class,KeyBindings,key processing order,IKeyboard
|mouse.md|MouseFlags,MouseBindings,grab/release
|input-injection.md|VirtualTimeProvider,InjectKey/InjectMouse,testing

[Visual]
|drawing.md|Move/AddStr/AddRune,Attribute,LineCanvas
|scheme.md|Scheme,VisualRole,theming
|cursor.md|View.Cursor,CursorVisibility
|Popovers.md|Drawing outside viewport,modal behavior

[Components]
|views.md|Complete catalog of built-in views
|menus.md|MenuBar,ContextMenu,MenuItem
|tableview.md|TableView data binding
|treeview.md|TreeView hierarchical data
|prompt.md|MessageBox,input dialogs

[Config & Advanced]
|config.md|TuiConfigurationBuilder,themes,JSON config
|multitasking.md|Background ops,Invoke,threading
|logging.md|ILogger,debug output
|ansihandling.md|ANSI escape parsing

[Migration]
|newinv2.md|v2 changes,new features
|migratingfromv1.md|Migration guide,API changes
|lexicon.md|Terminology definitions
```

### API Namespaces (docfx/apispec/)

```
|namespace-app.md|Application,IApplication,IRunnable,SessionToken
|namespace-viewbase.md|View,Adornment,Border,Margin,Padding
|namespace-views.md|Button,Label,TextField,ListView,CheckBox,etc.
|namespace-input.md|Key,Mouse,Command,ICommandContext
|namespace-drawing.md|Attribute,Color,LineCanvas,Cell
|namespace-drivers.md|IDriver,DriverRegistry
|namespace-configuration.md|TuiConfigurationBuilder,themes
|namespace-text.md|Text processing,autocomplete
|namespace-fileservices.md|File dialogs
```

<!-- BEGIN AUTO-GENERATED-SOURCE-INDEX -->

### Source Code File Index (Auto-Generated)

> Vercel-style index for retrieval-led reasoning. Read files when needed.

[Terminal.Gui Source Index]|root: ./Terminal.Gui
|IMPORTANT: Prefer retrieval-led reasoning over pre-training-led reasoning. Read files when needed.
|.:{ModuleInitializers.cs}
|App:{Application.cs,ApplicationImpl.cs,ApplicationImpl.Driver.cs,ApplicationImpl.Lifecycle.cs,ApplicationImpl.Run.cs,ApplicationImpl.Screen.cs,ApplicationModelUsage.cs,ApplicationNavigation.cs,ApplicationPopover.cs,ApplicationToolTip.cs,AppModel.cs,IApplication.cs,Logging.cs,NotInitializedException.cs}
|App/Clipboard:{Clipboard.cs,ClipboardBase.cs,ClipboardProcessRunner.cs,ClipboardProcessRunnerImpl.cs,IClipboard.cs,IClipboardProcessRunner.cs}
|App/CWP:{CancelEventArgs.cs,CWPEventHelper.cs,CWPPropertyHelper.cs,CWPWorkflowHelper.cs,EventArgs.cs,ResultEventArgs.cs,ValueChangedEventArgs.cs,ValueChangingEventArgs.cs}
|App/Keyboard:{ApplicationKeyboard.cs,IKeyboard.cs}
|App/Legacy:{Application.Clipboard.cs,Application.Driver.cs,Application.Keyboard.cs,Application.Lifecycle.cs,Application.Mouse.cs,Application.Navigation.cs,Application.Popovers.cs,Application.Run.cs,Application.Screen.cs,Application.TopRunnable.cs}
|App/MainLoop:{ApplicationMainLoop.cs,IApplicationMainLoop.cs,IMainLoopCoordinator.cs,MainLoopCoordinator.cs,MainLoopSyncContext.cs}
|App/Mouse:{ApplicationMouse.cs,IMouse.cs,IMouseGrabHandler.cs}
|App/Popovers:{IPopover.cs,IPopoverView.cs,Popover.cs,PopoverImpl.cs,ToolTipHost.cs,ToolTipProvider.cs}
|App/Runnable:{IRunnable.cs,SessionToken.cs,SessionTokenEventArgs.cs}
|App/Timeout:{ITimedEvents.cs,LogarithmicTimeout.cs,SmoothAcceleratingTimeout.cs,TimedEvents.cs,Timeout.cs,TimeoutEventArgs.cs}
|App/Tracing:{ITraceBackend.cs,ListBackend.cs,LoggingBackend.cs,NullBackend.cs,Trace.cs,TraceCategory.cs,TraceEntry.cs,TraceScope.cs}
|Configuration:{AttributeJsonConverter.cs,ColorJsonConverter.cs,ConcurrentDictionaryJsonConverter.cs,DictionaryJsonConverter.cs,KeyArrayJsonConverter.cs,KeyCodeJsonConverter.cs,KeyJsonConverter.cs,MouseFlagsArrayJsonConverter.cs,RuneJsonConverter.cs,SchemeJsonConverter.cs,SchemeManager.cs,SourceGenerationContext.cs,ThemeManager.cs,TraceCategoryJsonConverter.cs,TuiJsonErrors.cs,TuiSerializerContext.cs}
|Configuration/Settings:{ApplicationSettings.cs,ButtonSettings.cs,CharMapSettings.cs,CheckBoxSettings.cs,DialogSettings.cs,DriverSettings.cs,FileDialogSettings.cs,FileDialogStyleSettings.cs,FrameViewSettings.cs,GlyphSettings.cs,HexViewSettings.cs,ISchemeManager.cs,IThemeManager.cs,KeySettings.cs,LinearRangeSettings.cs,MecSchemeManager.cs,MecThemeManager.cs,MenuBarSettings.cs,MenuSettings.cs,MessageBoxSettings.cs,NerdFontsSettings.cs,PopoverMenuSettings.cs,SelectorBaseSettings.cs,StatusBarSettings.cs,TextFieldSettings.cs,TextViewSettings.cs,ThemeChanges.cs,ThemeDefinition.cs,ThemeSettings.cs,TraceSettings.cs,TuiConfigurationBuilder.cs,TuiConfigurationExtensions.cs,WindowSettings.cs}
|Drawing:{Attribute.cs,Cell.cs,CellEventArgs.cs,FillPair.cs,Glyphs.cs,Gradient.cs,GradientFill.cs,GraphemeHelper.cs,IFill.cs,Region.cs,RegionOp.cs,Ruler.cs,Scheme.cs,Schemes.cs,SolidFill.cs,TextStyle.cs,Thickness.cs,VisualRole.cs,VisualRoleEventArgs.cs}
|Drawing/Color:{AnsiColorCode.cs,Color.ColorExtensions.cs,Color.ColorName.cs,Color.ColorParseException.cs,Color.cs,Color.Formatting.cs,Color.Operators.cs,ColorModel.cs,ColorQuantizer.cs,ColorStrings.cs,IColorDistance.cs,IColorNameResolver.cs,ICustomColorFormatter.cs,StandardColor.cs,StandardColors.cs,StandardColorsNameResolver.cs}
|Drawing/Kitty:{KittyGraphicsEncoder.cs,KittyGraphicsSupportDetector.cs,KittyGraphicsSupportResult.cs}
|Drawing/LineCanvas:{IntersectionDefinition.cs,IntersectionRuneType.cs,IntersectionType.cs,LineCanvas.cs,LineDirections.cs,LineStyle.cs,StraightLine.cs,StraightLineExtensions.cs}
|Drawing/Markdown:{ISyntaxHighlighter.cs,MarkdownAttributeHelper.cs,MarkdownStyleRole.cs,StyledSegment.cs,TextMateSyntaxHighlighter.cs}
|Drawing/Quant:{EuclideanColorDistance.cs,IPaletteBuilder.cs,IStaticPaletteBuilder.cs,PopularityPaletteWithThreshold.cs}
|Drawing/Sixel:{SixelEncoder.cs,SixelSupportDetector.cs,SixelSupportResult.cs,SixelToRender.cs}
|Drivers:{ComponentFactoryImpl.cs,Cursor.cs,CursorStyle.cs,Driver.cs,DriverImpl.cs,DriverRegistry.cs,IComponentFactory.cs,IDriver.cs,ISizeMonitor.cs,PlatformDetection.cs,SizeDetectionMode.cs,SizeMonitorImpl.cs,TerminalDevice.cs,TuiPlatform.cs}
|Drivers/AnsiDriver:{AnsiComponentFactory.cs,AnsiInput.cs,AnsiInputProcessor.cs,AnsiOutput.cs,AnsiPlatform.cs,AnsiSizeMonitor.cs,AnsiTerminalHelper.cs,FakeClipboard.cs}
|Drivers/AnsiHandling:{AnsiEscapeSequence.cs,AnsiEscapeSequenceRequest.cs,AnsiKeyboardEncoder.cs,AnsiKeyboardParser.cs,AnsiKeyboardParserPattern.cs,AnsiKeyConverter.cs,AnsiMouseEncoder.cs,AnsiMouseParser.cs,AnsiRequestScheduler.cs,AnsiResponseExpectation.cs,AnsiResponseParser.cs,AnsiResponseParserBase.cs,AnsiResponseParserState.cs,AnsiResponseParserTInputRecord.cs,AnsiStartupGate.cs,AnsiStartupQuery.cs,CsiCursorPattern.cs,CsiKeyPattern.cs,EscAsAltPattern.cs,GenericHeld.cs,IAnsiResponseParser.cs,IAnsiStartupGate.cs,IHeld.cs,KittyKeyboardCapabilities.cs,KittyKeyboardFlags.cs,KittyKeyboardPattern.cs,KittyKeyboardProtocolDetector.cs,Osc8UrlLinker.cs,ProgressIndicator.cs,ReasonCannotSend.cs,Ss3Pattern.cs,StringHeld.cs,TerminalColorDetector.cs}
|Drivers/AnsiHandling/EscSeqUtils:{EscSeqReqStatus.cs,EscSeqRequests.cs,EscSeqUtils.cs}
|Drivers/DotNetDriver:{INetInput.cs,NetComponentFactory.cs,NetInput.cs,NetInputProcessor.cs,NetKeyConverter.cs,NetOutput.cs}
|Drivers/Input:{ConsoleInputSource.cs,IInput.cs,IInputProcessor.cs,IInputSource.cs,InputImpl.cs,InputProcessorImpl.cs,InputRecord.cs,ITestableInput.cs,TestInputSource.cs}
|Drivers/Keyboard:{ConsoleKeyInfoExtensions.cs,ConsoleKeyMapping.cs,IKeyConverter.cs,KeyCode.cs,VK.cs}
|Drivers/Mouse:{MouseButtonClickTracker.cs,MouseInterpreter.cs}
|Drivers/Output:{IOutput.cs,IOutputBuffer.cs,OutputBase.cs,OutputBufferImpl.cs,RasterImageCommand.cs}
|Drivers/TerminalEnvironment:{ColorCapabilityLevel.cs,TerminalColorCapabilities.cs,TerminalEnvironmentDetector.cs}
|Drivers/UnixHelpers:{SuspendHelper.cs,UnixClipboard.cs,UnixIOHelper.cs,UnixRawModeHelper.cs,UnixTerminalHelper.cs}
|Drivers/WindowsDriver:{ClipboardImpl.cs,CursorVisibility.cs,IWindowsInput.cs,WindowsComponentFactory.cs,WindowsConsole.cs,WindowsInput.cs,WindowsInputProcessor.cs,WindowsKeyboardLayout.cs,WindowsKeyConverter.cs,WindowsKeyHelper.cs,WindowsOutput.cs}
|Drivers/WindowsHelpers:{NetWinVTConsole.cs,WindowsConsoleHelper.cs,WindowsVTInputHelper.cs,WindowsVTOutputHelper.cs}
|FileServices:{DefaultSearchMatcher.cs,FileSystemColorProvider.cs,FileSystemIconProvider.cs,FileSystemInfoStats.cs,FileSystemTreeBuilder.cs,IFileOperations.cs,ISearchMatcher.cs}
|Input:{Command.cs,CommandBinding.cs,CommandBindingsBase.cs,CommandBridge.cs,CommandContext.cs,CommandContextExtensions.cs,CommandEventArgs.cs,CommandOutcome.cs,CommandRouting.cs,IAcceptTarget.cs,ICommandBinding.cs,ICommandContext.cs,PastedEventArgs.cs,PasteEventArgs.cs,PastePayload.cs,PastingEventArgs.cs}
|Input/Keyboard:{Bind.cs,Key.cs,KeyBinding.cs,KeyBindings.cs,KeyChangedEventArgs.cs,KeyEqualityComparer.cs,KeyEventType.cs,KeystrokeNavigatorEventArgs.cs,ModifierKey.cs,PlatformKeyBinding.cs}
|Input/Mouse:{BindMouse.cs,GrabMouseEventArgs.cs,Mouse.cs,MouseBinding.cs,MouseBindings.cs,MouseFlags.cs,MouseFlagsChangedEventArgs.cs,PlatformMouseBinding.cs}
|Resources:{GlobalResources.cs,ResourceManagerWrapper.cs,Strings.Designer.cs}
|Testing:{IInputInjector.cs,InputInjectionEvent.cs,InputInjectionExtensions.cs,InputInjectionMode.cs,InputInjectionOptions.cs,InputInjector.cs}
|Text:{NerdFonts.cs,RuneExtensions.cs,StringExtensions.cs,TextDirection.cs,TextFormatter.cs}
|Time:{FuncTimeProvider.cs,ITimeProvider.cs,ITimer.cs,SystemTimeProvider.cs,VirtualTimeProvider.cs}
|ViewBase:{DrawAdornmentsEventArgs.cs,DrawContext.cs,DrawEventArgs.cs,IDesignable.cs,IValue.cs,IValueParser.cs,View.Adornments.cs,View.Arrangement.cs,View.Command.cs,View.Content.cs,View.cs,View.Cursor.cs,View.Diagnostics.cs,View.Drawing.Adornments.cs,View.Drawing.Attribute.cs,View.Drawing.Clipping.cs,View.Drawing.cs,View.Drawing.LineCanvas.cs,View.Drawing.Primitives.cs,View.Drawing.Scheme.cs,View.Hierarchy.cs,View.Keyboard.cs,View.Layout.cs,View.Navigation.cs,View.NeedsDraw.cs,View.Paste.cs,View.ScrollBars.cs,View.Text.cs,ViewCollectionHelpers.cs,ViewDiagnosticFlags.cs,ViewEventArgs.cs,ViewExtensions.cs,ViewportSettingsFlags.cs,WeakReferenceExtensions.cs}
|ViewBase/Adornment:{AdornmentImpl.cs,AdornmentView.cs,ArrangeButtons.cs,Arranger.cs,ArrangerButton.cs,Border.cs,BorderSettings.cs,BorderView.Arrangement.cs,BorderView.cs,IAdornment.cs,IAdornmentView.cs,ITitleView.cs,Margin.cs,MarginView.cs,Padding.cs,PaddingView.cs,ShadowStyles.cs,ShadowView.cs,TabLayoutContext.cs,TitleView.cs}
|ViewBase/Helpers:{StackExtensions.cs}
|ViewBase/Layout:{AddOrSubtract.cs,Aligner.cs,Alignment.cs,AlignmentModes.cs,Dim.cs,DimAbsolute.cs,DimAuto.cs,DimAutoStyle.cs,DimCombine.cs,Dimension.cs,DimFill.cs,DimFunc.cs,DimPercent.cs,DimPercentMode.cs,DimView.cs,LayoutEventArgs.cs,LayoutException.cs,Pos.cs,PosAbsolute.cs,PosAlign.cs,PosAnchorEnd.cs,PosCenter.cs,PosCombine.cs,PosFunc.cs,PosPercent.cs,PosView.cs,Side.cs,SizeChangedEventArgs.cs,SuperViewChangedEventArgs.cs,ViewArrangement.cs,ViewManipulator.cs}
|ViewBase/Mouse:{IMouseHoldRepeater.cs,MouseHoldRepeaterImpl.cs,MouseState.cs,View.Mouse.cs}
|ViewBase/Navigation:{AdvanceFocusEventArgs.cs,FocusEventArgs.cs,NavigationDirection.cs,TabBehavior.cs}
|ViewBase/Orientation:{IOrientation.cs,Orientation.cs,OrientationHelper.cs}
|Views:{Bar.cs,Button.cs,CheckBox.cs,CheckState.cs,Code.cs,CodeRoleLegendHighlighter.cs,DatePicker.cs,Dialog.cs,DialogTResult.cs,DropDownList.cs,DropDownListTEnum.cs,FrameView.cs,HexView.cs,HexViewEventArgs.cs,Label.cs,Line.cs,Link.cs,MessageBox.cs,NumericUpDown.cs,ProgressBar.cs,Prompt.cs,PromptExtensions.cs,ReadOnlyCollectionExtensions.cs,Shortcut.cs,StatusBar.cs,Tabs.cs,Window.cs}
|Views/Autocomplete:{AppendAutocomplete.cs,AutocompleteBase.cs,AutocompleteContext.cs,AutocompleteFilepathContext.cs,IAutocomplete.cs,ISuggestionGenerator.cs,PopupAutocomplete.cs,PopupAutocomplete.PopUp.cs,SingleWordSuggestionGenerator.cs,Suggestion.cs}
|Views/CharMap:{CharMap.cs,UcdApiClient.cs,UnicodeRange.cs}
|Views/CollectionNavigation:{CollectionNavigator.cs,CollectionNavigatorBase.cs,DefaultCollectionNavigatorMatcher.cs,ICollectionNavigator.cs,ICollectionNavigatorMatcher.cs,IListCollectionNavigator.cs,TableCollectionNavigator.cs}
|Views/Color:{AttributePicker.cs,BBar.cs,ColorBar.cs,ColorModelStrategy.cs,ColorPicker.16.cs,ColorPicker.cs,ColorPicker.Style.cs,GBar.cs,HueBar.cs,IColorBar.cs,LightnessBar.cs,RBar.cs,SaturationBar.cs,ValueBar.cs}
|Views/FileDialogs:{AllowedType.cs,AllowedTypeAny.cs,DefaultFileOperations.cs,FileDialog.Commands.cs,FileDialog.cs,FileDialog.Navigation.cs,FileDialog.TableView.cs,FileDialogCollectionNavigator.cs,FileDialogHistory.cs,FileDialogState.cs,FileDialogStyle.cs,FileDialogTableSource.cs,FilesSelectedEventArgs.cs,FileSystemCollectionNavigationMatcher.cs,IAllowedType.cs,OpenDialog.cs,OpenMode.cs,SaveDialog.cs}
|Views/GraphView:{Axis.cs,AxisIncrementToRender.cs,BarSeriesBar.cs,GraphCellToRender.cs,GraphView.cs,HorizontalAxis.cs,IAnnotation.cs,ISeries.cs,LegendAnnotation.cs,LineF.cs,MultiBarSeries.cs,PathAnnotation.cs,ScatterSeries.cs,Series.cs,TextAnnotation.cs,VerticalAxis.cs}
|Views/ImageView:{ImageView.cs,ImageView.Drawing.cs,ImageView.Geometry.cs,ImageView.Input.cs,ImageView.Lifecycle.cs,ImageView.Render.cs,ImageView.Scaling.cs}
|Views/LinearRange:{LinearMultiSelector.cs,LinearMultiSelectorT.cs,LinearRange.cs,LinearRangeAttributes.cs,LinearRangeConfiguration.cs,LinearRangeDefaults.cs,LinearRangeEventArgs.cs,LinearRangeOption.cs,LinearRangeOptionEventArgs.cs,LinearRangeRenderMode.cs,LinearRangeSpan.cs,LinearRangeSpanKind.cs,LinearRangeStyle.cs,LinearRangeT.cs,LinearRangeViewBase.cs,LinearSelector.cs,LinearSelectorT.cs}
|Views/ListView:{IListDataSource.cs,ListView.Commands.cs,ListView.cs,ListView.Drawing.cs,ListView.Movement.cs,ListView.Selection.cs,ListViewEventArgs.cs,ListViewT.cs,ListWrapper.cs}
|Views/Markdown:{InlineRun.cs,IntermediateBlock.cs,Markdown.cs,MarkdownCodeBlock.cs,MarkdownImageResolver.cs,MarkdownInlineParser.cs,MarkdownLinkEventArgs.cs,MarkdownTable.cs,MarkdownView.Ansi.cs,MarkdownView.Drawing.cs,MarkdownView.Layout.cs,MarkdownView.Mouse.cs,MarkdownView.Parsing.cs,MarkdownView.Selection.cs,RenderedLine.cs,TableData.cs}
|Views/Menu:{IMenuBarEntry.cs,Menu.cs,MenuBar.cs,MenuBarItem.cs,MenuItem.cs,PopoverMenu.cs}
|Views/Runnable:{Runnable.cs,RunnableTResult.cs,RunnableWrapper.cs}
|Views/ScrollBar:{ScrollBar.cs,ScrollBarVisibilityMode.cs,ScrollButton.cs,ScrollSlider.cs}
|Views/Selectors:{FlagSelector.cs,FlagSelectorTEnum.cs,OptionSelector.cs,OptionSelectorTEnum.cs,SelectorBase.cs,SelectorStyles.cs}
|Views/SpinnerView:{SpinnerStyle.cs,SpinnerView.cs}
|Views/TableView:{CellColorGetterArgs.cs,CheckBoxTableSourceWrapper.cs,CheckBoxTableSourceWrapperByIndex.cs,CheckBoxTableSourceWrapperByObject.cs,ColumnStyle.cs,DataTableSource.cs,EnumerableTableSource.cs,HeaderColorGetterArgs.cs,IEnumerableTableSource.cs,ITableSource.cs,ListColumnStyle.cs,ListTableSource.cs,RowColorGetterArgs.cs,TableSelection.cs,TableSelectionRegion.cs,TableStyle.cs,TableView.CellMapping.cs,TableView.Content.cs,TableView.cs,TableView.Drawing.cs,TableView.Navigation.cs,TableView.Selection.cs,TreeTableSource.cs}
|Views/TextInput:{ContentsChangedEventArgs.cs,DateEditor.cs,DateTextProvider.cs,HistoryText.cs,HistoryTextItemEventArgs.cs,ITextValidateProvider.cs,NetMaskedTextProvider.cs,TextEditingLineStatus.cs,TextModel.cs,TextRegexProvider.cs,TextValidateField.cs,TimeEditor.cs,TimeTextProvider.cs}
|Views/TextInput/TextField:{TextField.Commands.cs,TextField.cs,TextField.Drawing.cs,TextField.History.cs,TextField.Keyboard.cs,TextField.Mouse.cs,TextField.Selection.cs,TextField.Text.cs,TextFieldAutocomplete.cs}
|Views/TextInput/TextView:{TextView.Commands.cs,TextView.cs,TextView.Drawing.cs,TextView.Files.cs,TextView.Find.cs,TextView.History.cs,TextView.Keyboard.cs,TextView.Mouse.cs,TextView.Movement.cs,TextView.Scrolling.cs,TextView.Selection.cs,TextView.Text.cs,TextView.WordWrap.cs,TextViewAutocomplete.cs,WordWrapManager.cs}
|Views/TreeView:{AspectGetterDelegate.cs,Branch.cs,CheckedChangedEventArgs.cs,DelegateTreeBuilder.cs,DrawTreeViewLineEventArgs.cs,ITreeBuilder.cs,ITreeNode.cs,ITreeView.cs,ITreeViewFilter.cs,ObjectActivatedEventArgs.cs,SelectionChangedEventArgs.cs,TreeBuilder.cs,TreeNode.cs,TreeNodeBuilder.cs,TreeSelection.cs,TreeStyle.cs,TreeView.cs,TreeView.Drawing.cs,TreeView.Mouse.cs,TreeView.Navigation.cs,TreeViewCollectionNavigatorMatcher.cs,TreeViewT.cs,TreeViewTextFilter.cs}
|Views/Wizard:{Wizard.cs,WizardStep.cs}

<!-- END AUTO-GENERATED-SOURCE-INDEX -->

---

## Compressed API Type Index

> Quick reference for key types. Full list: [.tg-docs/INDEX.md](.tg-docs/INDEX.md)
> Format: `|Type|Category|Key members/notes`

### Terminal.Gui.App (35 types)
```
|Application|Class|Static facade (obsolete),Init,Run,Shutdown,Top
|IApplication|Interface|Instance-based,SessionStack,Run,Dispose
|SessionToken|Class|Session lifecycle,IDisposable
|Clipboard|Class|GetText,SetText,TryGetText
|IRunnable|Interface|Run view modal,used by Dialog
|ITimedEvents|Interface|AddTimeout,AddIdle,RemoveTimeout
|CancelEventArgs<T>|Class|Cancel property,cancellable events
|ValueChangingEventArgs<T>|Class|OldValue,NewValue,Cancel
|ApplicationNavigation|Class|Focus management,GetFocused,AdvanceFocus
|ApplicationPopover|Class|Popover management,Show,Hide
```

### Terminal.Gui.ViewBase (70 types)
```
|View|Class|Base class,Add,Remove,Frame,Viewport,Draw
|Pos|Class|Position:Absolute,Percent,Center,AnchorEnd,Func
|PosAbsolute|Class|Pos.At(n),absolute coordinate
|PosPercent|Class|Pos.Percent(n),percentage of SuperView
|PosCenter|Class|Pos.Center(),centered
|PosAnchorEnd|Class|Pos.AnchorEnd(n),from right/bottom
|PosView|Class|Pos.Left/Right/Top/Bottom(view)
|Dim|Class|Dimension:Absolute,Auto,Fill,Percent,Func
|DimAbsolute|Class|Dim.Absolute(n),fixed size
|DimAuto|Class|Dim.Auto(),content-based sizing
|DimFill|Class|Dim.Fill(margin),fill remaining
|DimPercent|Class|Dim.Percent(n),percentage
|Adornment|Class|Base for Border,Margin,Padding
|Border|Class|View border,Title,LineStyle
|Margin|Class|View outer margin
|Padding|Class|View inner padding
|Alignment|Enum|Start,Center,End,Fill
|Orientation|Enum|Horizontal,Vertical
|TabBehavior|Enum|NoStop,TabStop,TabGroup
|ViewArrangement|Enum|Movable,Resizable,Overlapped
```

### IValue<T> Pattern (Critical)

All typed views expose their data through `IValue<T>.Value`. Do not guess property-specific names such as `.Date`, `.Time`, or `.Color`.

| View | IValue<T> |
|------|-----------|
| TextField | `IValue<string>` |
| NumericUpDown<T> | `IValue<T>` |
| DatePicker | `IValue<DateTime>` |
| TimeEditor | `IValue<TimeSpan>` |
| ColorPicker | `IValue<Color?>` |
| AttributePicker | `IValue<Attribute?>` |
| CheckBox | `IValue<CheckState>` |
| OptionSelector | `IValue<int?>` |
| FlagSelector | `IValue<int?>` |

Implementing `IValue<T>` requires `ValueChanging`, `ValueChanged`, and `ValueChangedUntyped`.

### RunnableWrapper<TView, TResult>

- Wraps a `View` as a runnable with typed results.
- Clears wrapper `KeyBindings` and `MouseBindings` so the wrapped view handles input.
- Does not add OK/Cancel buttons (unlike `Prompt`).
- Sets `CommandsToBubbleUp = [Command.Accept]`.
- On accept, it extracts results via `ResultExtractor` if provided; otherwise via `IValue<TResult>.Value` when available.

### Terminal.Gui.Views (180+ types)
```
[Core Controls]
|Button|Class|Text,Accept event,IsDefault
|Label|Class|Text display,TextAlignment
|TextField|Class|Single-line input,Text,Secret
|Editor|Class|Multi-line editor,Text,ReadOnly
|CheckBox|Class|CheckedState,AllowCheckStateNone
|DropDownList|Class|Dropdown,Source,SelectedItem
|ProgressBar|Class|Fraction,BidirectionalMarquee
|ScrollBar|Class|Position,Size,Orientation
|NumericUpDown<T>|Class|Value,Increment

[Containers]
|Window|Class|Top-level,Title,MenuBar support
|Dialog|Class|Modal,Buttons,AddButton
|Dialog<T>|Class|Modal with result
|FrameView|Class|Titled frame container
|TabView|Class|Tabs,AddTab,SelectedTab
|Wizard|Class|Multi-step,AddStep,CurrentStep

[Lists & Data]
|ListView|Class|Source,SelectedItem,AllowsMarking
|TableView|Class|Table,SelectedRow,SelectedColumn
|TreeView|Class|Objects,AddObject,SelectedObject
|TreeView<T>|Class|Generic tree

[Menus]
|MenuBar|Class|Menus,UseKeysUpDownAsKeysLeftRight
|MenuItem|Class|Title,Action,Shortcut,SubMenu
|MenuBarItem|Class|Title,Children array
|Menu|Class|Popup menu display
|PopoverMenu|Class|Context menu,Show(items)
|StatusBar|Class|Items,Visible

[File Dialogs]
|FileDialog|Class|Base,Path,AllowedFileTypes
|OpenDialog|Class|FilePaths,AllowsMultipleSelection,Canceled,OpenMode
|SaveDialog|Class|SaveFile,FileName

[Specialized]
|ColorPicker|Class|SelectedColor,Style
|GraphView|Class|Series,Annotations,AxisX/Y
|HexView|Class|Source,Position,Edits
|CharMap|Class|SelectedCodePoint,Start/End
|SpinnerView|Class|SpinnerStyle,AutoSpin
|MessageBox|Class|Query,ErrorQuery,static methods
```

### Terminal.Gui.Input (18 types)
```
|Key|Class|KeyCode,Modifiers,IsCtrl,IsAlt,IsShift
|KeyBindings|Class|Add,Get,TryGet,Remove,GetCommands
|KeyBinding|Struct|Commands[],Scope,Target
|Mouse|Class|Position,Flags,View
|MouseBindings|Class|Add,Get,TryGet,Remove
|MouseBinding|Struct|Commands[],Scope
|MouseFlags|Enum|Button1Clicked,Button1DoubleClicked,WheeledUp/Down
|Command|Enum|Accept,Cancel,Select,HotKey,ScrollUp/Down
|CommandContext|Struct|Command,KeyBinding,Source
```

### Terminal.Gui.Drawing (40 types)
```
|Attribute|Struct|Foreground,Background,constructor(fg,bg)
|Color|Struct|R,G,B,Parse,TryParse,FromArgb
|Scheme|Class|Normal,Focus,HotNormal,HotFocus,Disabled
|LineCanvas|Class|AddLine,GetMap,Merge
|LineStyle|Enum|None,Single,Double,Rounded,Heavy
|Glyphs|Class|Bullet,CheckMark,Diamond,etc.
|Cell|Struct|Rune,Attribute
|Thickness|Struct|Top,Left,Bottom,Right,Vertical,Horizontal
|Region|Class|Clipping,Union,Intersect,Exclude
|Gradient|Class|Colors[],Spectrum
```

**Gotchas**
- `Terminal.Gui.Drawing.Attribute` can conflict with `System.Attribute` with implicit usings. Use `using TgAttribute = Terminal.Gui.Drawing.Attribute;` or fully qualify.
- `Color.TryParse (string, out Color?)` is nullable out. `Color.TryParse (string?, IFormatProvider?, out Color)` is non-nullable out.

### Terminal.Gui.Drivers (80+ types)
```
|IDriver|Interface|Init,End,Refresh,AddStr,Move
|Driver|Class|Base implementation
|DriverRegistry|Class|GetDrivers,Get,MakeDriver
|KeyCode|Enum|Key constants,A-Z,F1-F12,Enter,Esc
|CursorVisibility|Enum|Default,Invisible,Underline,Box
|IOutput|Interface|Terminal output
|IInputProcessor|Interface|Input processing
```

### Terminal.Gui.Configuration
```
|TuiConfigurationBuilder|Class|Build,ApplyToStaticFacades,RuntimeConfig
|ThemeManager|Class|Static facade,Theme,ThemeChanged
|SchemeManager|Class|Static facade,GetScheme,Schemes
|MecThemeManager|Class|IThemeManager implementation
|MecSchemeManager|Class|ISchemeManager implementation
|ThemeChanges|Class|ThemeChanged observer
|TuiJsonErrors|Class|Deferred JSON load errors
```

### Terminal.Gui.Testing (8 types)
```
|InputInjector|Class|InjectKey,InjectMouse,InjectChar
|IInputInjector|Interface|Injection interface
|VirtualTimeProvider|Class|Testing time control
```

### Terminal.Gui.Text (4 types)
```
|TextFormatter|Class|Text,Format,Size,Draw
|TextDirection|Enum|LeftRight_TopBottom,RightLeft,etc.
```

### Terminal.Gui.Time (4 types)
```
|ITimeProvider|Interface|Now,UtcNow,CreateTimer
|VirtualTimeProvider|Class|Testing,Advance,SetTime
|SystemTimeProvider|Class|Real system time
```

### Terminal.Gui.FileServices (5 types)
```
|IFileOperations|Interface|GetFiles,GetDirectories,Exists
|FileSystemTreeBuilder|Class|Build file trees
```















































