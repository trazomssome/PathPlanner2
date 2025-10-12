using System;
using System.Windows;
using System.Windows.Forms.Integration;
using DispensePath;

namespace DispenserEditor.Controls
{
    public class DispensePathEditorHost : WindowsFormsHost, IDispensePathEditor
    {
        private readonly EditDispensePath _editor;
        private readonly EventHandler<PointAddedEventArgs> _pointAddedHandler;
        private readonly EventHandler<EventArgs> _recipeChangedHandler;
        private bool _disposed;

        public DispensePathEditorHost()
        {
            _editor = new EditDispensePath("DispensePath-WPF");
            Child = _editor;

            _pointAddedHandler = (s, e) => PointAdded?.Invoke(this, e);
            _recipeChangedHandler = (s, e) => RecipeChanged?.Invoke(this, EventArgs.Empty);

            _editor.PointAdded += _pointAddedHandler;
            _editor.RecipeChanged += _recipeChangedHandler;
            _editor.ViewOrder = true;
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            _editor?.Focus();
        }

        public Recipe_DispensingPoints Recipe => _editor.Recipe_DispPoints ?? (_editor.Recipe_DispPoints = new Recipe_DispensingPoints());

        public void SetRecipe(Recipe_DispensingPoints recipe)
        {
            _editor.Recipe_DispPoints = recipe ?? new Recipe_DispensingPoints();
            _editor.RedrawComposite();
        }

        public void UpdateBackgroundImage() => _editor.UpdateBackgroundImage();

        public void Redraw() => _editor.Invalidate();

        public void ZoomIn() => _editor.ZoomIn();

        public void ZoomOut() => _editor.ZoomOut();

        public void ZoomFit() => _editor.ZoomFit();

        public void SetTool(EditDispensePath.DrawToolType tool) => _editor.ActiveTool = tool;

        public EditDispensePath.DrawToolType ActiveTool => _editor.ActiveTool;

        public event EventHandler<PointAddedEventArgs> PointAdded;

        public event EventHandler RecipeChanged;

        public EditDispensePath UnderlyingEditor => _editor;

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                base.Dispose(disposing);
                return;
            }

            if (disposing)
            {
                _editor.PointAdded -= _pointAddedHandler;
                _editor.RecipeChanged -= _recipeChangedHandler;
                _editor.Dispose();
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
