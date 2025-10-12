using System;
using DispensePath;

namespace DispenserEditor.Controls
{
    public interface IDispensePathEditor : IDisposable
    {
        Recipe_DispensingPoints Recipe { get; }
        void SetRecipe(Recipe_DispensingPoints recipe);
        void UpdateBackgroundImage();
        void Redraw();
        void ZoomIn();
        void ZoomOut();
        void ZoomFit();
        void SetTool(EditDispensePath.DrawToolType tool);
        EditDispensePath.DrawToolType ActiveTool { get; }
        event EventHandler<PointAddedEventArgs> PointAdded;
        event EventHandler RecipeChanged;
        EditDispensePath UnderlyingEditor { get; }
    }
}
