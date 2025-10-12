using System.Windows;
using System.Windows.Controls;
using DispenserEditor.Controls;
using DispenserEditor.ViewModels;

namespace DispenserEditor.Views
{
    public partial class DispensePathView : UserControl
    {
        public DispensePathView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            DataContextChanged += OnDataContextChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is DispensePathViewModel vm && EditorHost != null)
            {
                vm.AttachEditor((IDispensePathEditor)EditorHost);
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsLoaded && DataContext is DispensePathViewModel vm && EditorHost != null)
            {
                vm.AttachEditor((IDispensePathEditor)EditorHost);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (EditorHost is IDispensePathEditor editor)
            {
                editor.Dispose();
            }
        }
    }
}
