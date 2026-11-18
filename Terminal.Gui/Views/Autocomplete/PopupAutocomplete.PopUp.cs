
namespace Terminal.Gui.Views;

public abstract partial class PopupAutocomplete
{
    private sealed class Popup : View
    {
        public Popup (PopupAutocomplete autoComplete)
        {
            _autoComplete = autoComplete;
            CanFocus = true;
            TabStop = TabBehavior.NoStop;
            MousePositionTracking = true;
        }

        private readonly PopupAutocomplete _autoComplete;

        protected override bool OnDrawingContent (DrawContext? context)
        {
            if (!_autoComplete.LastPopupPos.HasValue)
            {
                return true;
            }

            _autoComplete.RenderOverlay (_autoComplete.LastPopupPos.Value);

            return true;
        }

        protected override bool OnMouseEvent (Mouse mouse) { return _autoComplete.OnMouseEvent (mouse); }
    }
}
