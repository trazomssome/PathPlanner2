using System.Windows;
using DispenserEditor.Models;

namespace DispenserEditor
{
    public partial class MainWindow : Window
    {
        public PathRecipe LegacyRecipe { get; }

        public PathRecipe SegmentRecipe { get; }

        public MainWindow()
        {
            InitializeComponent();
            LegacyRecipe = Editor.ViewModel.Recipe;
            SegmentRecipe = SegmentEditor.ViewModel.Recipe;
            DataContext = this;
        }
    }
}
