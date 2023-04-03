# V2 Spec for View refactor

IMPORTANT: I am critical of the existing codebase below. Do not take any of this personally. It is about the code, not the amazing people who wrote the code.

ALSO IMPORTANT: I've written this to encourage and drive DEBATE. My style is to "Have strong opinions, weakly held." If you read something here you don't understand or don't agree with, SAY SO. Tell me why. Take a stand. 

This covers my thinking on how we will refactor `View` and the classes in the `View` heirarchy(inclidng `Responder`). It does not cover 
  * Text formatting which will be covered in another spec. 
  * TrueColor support which will be covered separately.
  * ConsoleDriver refactor.

## Terminal.Gui v2 View-related Lexicon & Taxonomy

  * *View* - The most basic visual element in Terminal.Gui. Implemented in the `View` base-class. 
  * *SubView* - A View that is contained in antoher view and will be rendered as part of the containing view's *ContentArea*. SubViews are added to another view via the `View.Add` method. A View may only be a SubView of a single View. 
  * *SuperView* - The View that is a container for SubViews. Referrs to the View another View was was added to as *SubView*. 
  * *Child View* - A view that is held by another view in a parent/child relationshiop, but is NOT a SubView. Examples of this are sub-menus of `MenuBar`. 
  * *Parent View* - A view that holds a reference to another view in a parent/child relationship, but is NOT a SuperView of the child. 
  * *Thickness* - Describes how thick a rectangle is on each of the rectangle's four sides. Valid thickness values are >= 0. 
  * *Margin* - Means the Thickness that separtes a View from other SubViews of the same SUperView. 
  * *Title* - Means text that is displayed for the View that describes the View to users. For most Views the Title is displayed at the top-left, overlaying the Border. 
  * *Border* - Means the Thickness where a visual border (drawn using line-drawing glyphs) and the Title are drawn. The Border expands inward; in other words if `Border.Thickness.Top == 2` the border & title will take up the first row and the second row will be filled with spaces. 
  * *Adornments* - The Thickness between the Margin and Padding. The Adornments property of `View` is a `View`-subclass that hosts SubViews that are not part of the View's content and are rendered within the Adornment Thickness. Examples of Adornments:
    * A `TitleBar` renders the View's `Title` and a horizontal line defining the top of the View. Adds thickness to the top of Adornments. 
    * One or more `LineView`s that render the View's border (NOTE: The magic of `LineCanvas` lets us automatically have the right joins for these and `TitleBar`!).
    * A `Vertical Scrollbar` adds thickness to `Adornments.Right` (or `.Left` when right-to-left language support is added). 
    * A `Horizontal Scrollbar` adds thickness to `Adornments.Bottom` when enabled.
    * A `MenuBar` adds thickness to `Adornments.Top` (NOTE: This is a change from v1 where `subview.Y = 1` is required).
    * A `StatusBar` adds thickness ot `Adornments.Bottom` and is rendered at the bottom of Padding.
    * NOTE: The use of `View.Add` in v1 to add adornments to Views is the cause of much code complexity. Changing the API such that `View.Add` is ONLY for subviews and adding a `View.Adornments.Add` API for menu, statusbar, scroll bar... will enable us to signficantly simplify the codebase.
  * *Padding* - Means the Thickness inside of an element that offsets the `Content` from the Border. (NOTE: in v1 `Padding` is OUTSIDE of the `Border`). Padding is `{0, 0, 0, 0}` by default.
  * *Frame* - Means the `Rect` that defines the location and size of the `View` including all of the margin, border, padding, and content area. The coordinates are relative to the SuperView of the View (or, in the case of `Application.Top`, `ConsoleDriver.Row == 0; ConsoleDriver.Col == 0`). The Frame's location and size are controlled by either `Absolute` or `Computed` positioning via the `.X`, `.Y`, `.Height`, and `.Width` properties of the View. 
  * *VisibleArea* - Means the area inside of the Margin + Border (Title) + Padding. `VisibleArea.Location` is always `{0, 0}`. `VisibleArea.Size` is the `View.Frame.Size` shrunk by Margin + Border + Padding. 
  * *ContentArea* - The `Rect` that describes the location and size of the View's content, relative to `VisibleRect`. If `ContentArea.Location` is negative, anything drawn there will be clipped and any subview positioned in the negative area will cause (optional) scrollbars to appear (making the Thickness of Padding thicker on the appropriate sides). If `ContentArea.Size` is changed such that the dimensions fall outside of `Frame.Size shrunk by Margin + Border + Padding`, drawning will be clipped and (optional) scrollbars will appear.
  * *Bounds* - Synomous with *VisibleArea*. (Debate: Do we rename `Bounds` to `VisbleArea` in v2?)
  * *ClipArea* - Means the currently vislble portion of the *Content*. This is defined as a`Rect` in coordinates relative to *ContentArea* (NOT *VisibleArea*) (e.g. `ClipArea {X = 0, Y = 0} == ContentArea {X = 0, Y = 0}`). This `Rect` is passed to `View.Redraw` (and should be named "clipArea" not "bounds"). It defines the clip-region the caller desires the `Redraw` implementation to clip itself to (see notes on clipping below).
  * *Modal* - The term used when describing a View that was created using the `Application.Run(view)` or `Application.Run<T>` APIs. When a View is running as a modal, user input is restricted to just that View until `Application.Run` exits. 
  * *TopLevel* - The term used to describe a view that is both Modal and can have a MenuBar and/or StatusBar. 
  * *Window* - A View that is 
  * *Tile*, *Tiled*, *Tiling* - Refers to a form of layout where the SubViews of a View are visually arranged such that their Frames abut each other and do not overlap. In a Tiled view arragnement there is no Z-ordering.
  * *Overlap*, *Overlapped*, *Overlapping* - Refers to a form of layout where the SubViews of a View are visually arranged such that their Frames can overlap. In Overlap view arragements there is a Z-axis (Z-order) in addition to the X and Y dimension. The Z-order indicates which Views are shown above other views.


  ### Questions

  * @bdisp - Why does `TopLevel.Activate/Deactivate` exist? Why is this just not `Focus`?
  * @bdisp - is the "Mdi" concept, really about "non-modal views that have z-order and can overlap"? "Not Mdi" means "non-modal views that have the same-zorder and are tiled"


        * `View.MouseEvent` etc... should always use `ContentBounds`-relative coordinates and should constrained by `GetClipRect`.
  *  
* After many fits and starts we have clipping working well. But the logic is convoluted and full of spaghetti code. We should simplfiy this by being super clear on how clipping works. 
  * `View.Redraw(clipRect)` specifies a `clipRect` that is outside of `View.GetClipRect` it has no impact (only a `clipRect` where `View.ClipRect.Union(clipRect)` makes the rect smaller does anything). 
  * Changing `Driver.ClipRect` from within a `Draw` implementation to draw outside of the `ContentBounds` should be disallowed (see non-ContentBounds drawing below).
* Border (margin, padding, frame, title, etc...) is confusing and incorrect.
  * The only reason FrameView exists is because the original architecture didn't support offsetting `View.Bounds` 
  such that a border could be drawn and the interior content would clip correctly. Thus Miguel (or someone) built
  FrameView with nested `ContentView` that was at `new Rect(+1, +1, -2, -2)`. 
    * `Border` was added later, but couldn't be retrofitted into `View` such that if `View.Border ~= null` just worked like `FrameView`
    * Thus devs are forced to use the clunky `FrameView` instead of just setting `View.Border`.
  * It's not possilbe for a `View` to utilize `Border.BorderBrush`.
  * `Border` has a bunch of confusing concepts that don't match other systems (esp the Web/HTML)
    * `Margin` on the web means the space between elements - `Border` doesn't have a margin property, but does has the confusing `DrawMarginFrame` property.
    * `Border` on the web means the space where a border is drawn. The current implementaiton confuses the term `Frame` and `Border`. `BorderThickness` is provided. In the new world, 
    but because of the confusion between `Padding` and `Margin` it doesn't work as expectedc.
    * `Padding` on the web means the padding inside of an element between the `Border` and `Content`. In the current implementation `Padding` is actually OUTSIDE of the `Border`. This means it's not possible for a view to offset internally by simply changing `Bounds`. 
    * `Content` on the web means the area inside of the Margin + Border + Padding. `View` does not currently have a concept of this (but `FrameView` and `Window` do via thier private `ContentView`s.
    * `Border` has a `Title` property. If `View` had a standard `Title` property could this be simplified (or should views that implement their own Title property just leverage `Border.Title`?).
    * It is not possilble for a class drived from View to orverride the drawing of the "Border" (frame, title, padding, etc...). Multiple devs have asked to be able to have the border frame to be drawn with a different color than `View.ColorScheme`.
* API should explicitly enable devs to override the drawing of `Border` independently of the `View.Draw` method. See how `WM_NCDRAW` works in wWindows (Draw non-client). It should be easy to do this from within a `View` sub-class (e.g. override `OnDrawBorder`) and externally (e.g. `DrawBorder += () => ...`. 

* `AutoSize` mostly works, but only because of heroic special-casing logic all over the place by @bdisp. This should be massively simplified.
* `FrameView` is superlufous and should be removed from the heirarchy (instead devs should just be able to manipulate `View.Border` to achieve what `FrameView` provides). The internal `FrameView.ContentView` is a bug-farm and un-needed if `View.Border` worked correctly. 
* `TopLevel` is currently built around several concepts that are muddled:
  * Views that host a Menu and StatusBar. It is not clear why this is and if it's needed as a concept. 
  * Views that can be run via `Application.Run<TopLevel>`. It is not clear why ANY VIEW can't be run this way
  * Views that can be used as a pop-up (modal) (e.g. `Dialog`). As proven by `Wizard`, it is possible to build a View that works well both ways. But it's way too hard to do this today.
  * Views that can be moved by the user.
  * If `View` class is REALLY required for enabling one more of the above concepts (e.g. `Window`) it should be as thin and simple as possilbe (e.g.  should inherit from `FrameView` (or just use `View.Border` effecively assuming `FrameView` is nuked) instead of containing duplicate code).
* The `MdiContainer` stuff is complex, perhaps overly so. It's also mis-named because Terminal.Gui doesn't actually support "documents" nor does it have a full "MDI" system like Windows (did). It seems to represent features useful in overlapping Views, but it is super confusing on how this works, and the naming doesn't help. This all can be refactored to support specific scenarios and thus be simplified.
* There is no facility for users' resizing of Views. @tznind's awesome work on `LineCanvas` and `TileView` combined with @tig's experiments show it could be done in a great way for both modal (overlapping) and tiled Views. 
* `DrawFrame` and `DrawTitle` are implemented in `ConsoleDriver` and can be replaced by a combination of `LineCanvas` and `Border`.
* Colors - 
  * As noted above each of Margin, Border, Padding, and Content should support independent colors.
  * Many View sub-classes bastardize the exiting ColorSchemes to get look/feel that works (e.g. `TextView` and `Wizard`). Separately we should revamp ColorSchemes to enable more scenarios. 
* `Responder` is supposed to be where all common, non-visual-related, code goes. We should ensure this is the case.
* `View` should have default support for scroll bars. e.g. assume in the new world `View.ContentBounds` is the clip area (defined by `VIew.Frame` minus `Margin` + `Border` + `Padding`) then if any view is added with `View.Add` that has Frame coordinates outside of `ContentBounds` the appropriate scroll bars show up automatgically (optioally of course). Without any code, scrolling just works. 
* We have many requests to support non-full-screen apps. We need to ensure the `View` class heirachy suppports this in a simple, understandable way. In a world with non-full-screen (where screen is defined as the visible terminal view) apps, the idea that `Frame` is "screen relative" is broken. Although we COULD just define "screen" as "the area that bounds the Terminal.GUI app.".  
  * Question related to this: If `View.Border` works correctly (margin, border, padding, content) and if non-full-screen apps are supported, what happens if the margin of `Application.Top` is not zero (e.g. `Border.Margin = new Thickness(1,1)`). It feels more pure that such a margin would make the top-left corner of `Application.Top`'s border be att `ConsoleDriver.Row = 1, Column = 1`). If this is thw path, then "screen" means `Application.Top.Frame`). This is my preference. 

## Thoughts on Built-in Views
* `LineView` can be replaced by `LineCanvas`?
* `Button` and `Label` can be merged. 
* `StatusBar` and `Menu` could be combined. If not, then at least made more consistent (e.g. in how hotkeys are specified).

## Design

* `Responder`("Responder base class implemented by objects that want to participate on keyboard and mouse input.") remains mostly unchanged, with minor changes:
   * Methods that take `View` parametsrs (e.g. `OnEnter`) change to take `Responder` (bad OO design).
   * Nuke `IsOverriden` (bad OO design)
   * Move `View.Data` to `Responder` (primitive)
   * Move `Command` and `KeyBinding` stuff from `View`.
   * Move generic mouse and keyboard stuff from `View` (e.g. `WantMousePositionReports`)


## Example of creating Adornments
```cs
// ends up looking just like the v1 default Window with a menu & status bar
// and a vertical scrollbar. In v2 the Window class would do all of this automatically.
var top = new TitleBar() {
    X = 0, Y = 0,
    Width = Dim.Fill(),
    Height = 1
    LineStyle = LineStyle.Single
};
var left = new LineView() {
    X = 0, Y = 0,
    Width = 1,
    Height = Dim.Fill(),
    LineStyle = LineStyle.Single
};
var right = new LineView() {
    X = Pos.AnchorEnd(), Y = 0,
    Width = 1,
    Height = Dim.Fill(),
    LineStyle = LineStyle.Single
};
var bottom = new LineView() {
    X = 0, Y = Pos.AnchorEnd(),
    Width = Dim.Fill(),
    Height = 1,
    LineStyle = LineStyle.Single
};

var menu = new MenuBar() { 
    X = Pos.Right(left), Y = Pos.Bottom(top)
};
var status = new StatusBar () {
    X = Pos.Right(left), Y = Pos.Top(bottom)
};
var vscroll = new ScrollBarView () {
    X = Pos.Left(right),
    Y = Dim.Fill(2) // for menu & status bar
};

Adornments.Add(titleBar);
Adornments.Add(left);
Adornments.Add(right);
Adornments.Add(bottom);
Adornments.Add(vscroll);

var treeView = new TreeView () {
    X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill()
};
Add (treeView);
```
