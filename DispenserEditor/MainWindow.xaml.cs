
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using DispenserEditor.Models;
using DispenserEditor.ViewModels;

namespace DispenserEditor
{

    public partial class MainWindow : Window
    {
        private bool _isDragging;

        private MainViewModel ViewModel => (MainViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void DrawingCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(DrawingCanvas);
            switch (ViewModel.Mode)
            {
                case EditorMode.Line:
                    ViewModel.AddCoordinateFromPixel(CoordinateType.Line, position);
                    break;
                case EditorMode.Point:
                    ViewModel.AddCoordinateFromPixel(CoordinateType.Point, position);
                    break;
                case EditorMode.Center:
                    ViewModel.SetCenterFromPixel(position);
                    break;
                default:
                    ViewModel.SelectedCoordinate = null;
                    break;
            }
        }

        private void DrawingCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.Mode == EditorMode.Line)
            {
                ViewModel.CompleteLine();
                e.Handled = true;
            }
        }

        private void CoordinateThumb_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Thumb thumb && thumb.DataContext is PathCoordinateViewModel coordinate)
            {
                ViewModel.SelectedCoordinate = coordinate;
                e.Handled = true;
            }
        }

        private void CoordinateThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            if (ViewModel.Mode != EditorMode.Move)
            {
                _isDragging = false;
                return;
            }

            _isDragging = true;
            ViewModel.BeginInteractiveChange();
        }

        private void CoordinateThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (!_isDragging)
            {
                return;
            }

            if (sender is Thumb thumb && thumb.DataContext is PathCoordinateViewModel coordinate)
            {
                var newPoint = new Point(coordinate.PixelX + e.HorizontalChange, coordinate.PixelY + e.VerticalChange);
                ViewModel.UpdateCoordinateFromPixel(coordinate, newPoint);
            }
        }

        private void CoordinateThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (_isDragging)
            {
                ViewModel.EndInteractiveChange();
                _isDragging = false;
            }
        }

        private void ResetCenter_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.UpdateCenterToImageMiddle();
        }
    }
}

