using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DispenserEditor.ViewModels;

namespace DispenserEditor.Views
{
    public partial class DispensePathView : UserControl
    {
        public DispensePathView()
        {
            InitializeComponent();
        }

        private DispensePathViewModel ViewModel => DataContext as DispensePathViewModel;

        private void OnCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var vm = ViewModel;
            if (vm == null)
            {
                return;
            }

            Point position = e.GetPosition(DrawingCanvas);

            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                vm.AddAnchorNode(position);
            }
            else
            {
                bool insertAfterSelected = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                vm.AddPointAtPosition(position, insertAfterSelected);
            }

            e.Handled = true;
        }

        private void OnPointEllipseMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var vm = ViewModel;
            if (vm == null)
            {
                return;
            }

            if (sender is FrameworkElement element && element.DataContext is DispensePointNodeViewModel node)
            {
                vm.SelectedPoint = node;
                e.Handled = true;
            }
        }

        private void OnAnchorNodeMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var vm = ViewModel;
            if (vm == null)
            {
                return;
            }

            if (sender is FrameworkElement element && element.DataContext is DispensePointNodeViewModel node)
            {
                vm.SelectedAnchorNode = node;
                e.Handled = true;
            }
        }
    }
}
