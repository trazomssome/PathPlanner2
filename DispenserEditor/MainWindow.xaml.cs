using System.Windows;

namespace DispenserEditor
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = Editor.ViewModel;
        }
    }
}
