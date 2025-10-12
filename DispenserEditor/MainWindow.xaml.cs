using System.Windows;
using DispenserEditor.ViewModels;

namespace DispenserEditor
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
