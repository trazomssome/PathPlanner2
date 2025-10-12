using System.Windows.Controls;
using DispenserEditor.ViewModels;

namespace DispenserEditor.Views
{
    public partial class DispensePathView : UserControl
    {
        public DispensePathView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            DataContextChanged += OnDataContextChanged;
        }

        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            AttachEditor();
        }

        private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            AttachEditor();
        }

        private void AttachEditor()
        {
            if (DataContext is DispensePathViewModel vm && EditorHost.Child != vm.GraphicEditor)
            {
                EditorHost.Child = vm.GraphicEditor;
                vm.RefreshGraphicGrid();
            }
        }
    }
}
