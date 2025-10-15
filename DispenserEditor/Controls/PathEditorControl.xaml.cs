using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using DispenserEditor.Models;
using DispenserEditor.ViewModels;

namespace DispenserEditor.Controls
{
    public partial class PathEditorControl : UserControl
    {
        public static readonly DependencyProperty RecipeProperty = DependencyProperty.Register(
            nameof(Recipe),
            typeof(PathRecipe),
            typeof(PathEditorControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRecipeChanged));

        private readonly PathEditorViewModel _viewModel;

        public PathEditorViewModel ViewModel => _viewModel;

        public PathEditorControl()
        {
            InitializeComponent();
            _viewModel = new PathEditorViewModel();
            DataContext = _viewModel;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            SetCurrentValue(RecipeProperty, _viewModel.Recipe);
        }

        public PathRecipe Recipe
        {
            get => (PathRecipe)GetValue(RecipeProperty);
            set => SetValue(RecipeProperty, value);
        }

        private static void OnRecipeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PathEditorControl control)
            {
                control.OnRecipeChanged(e.NewValue as PathRecipe);
            }
        }

        private void OnRecipeChanged(PathRecipe recipe)
        {
            if (recipe == null)
            {
                var replacement = new PathRecipe();
                _viewModel.LoadRecipe(replacement);
                SetCurrentValue(RecipeProperty, _viewModel.Recipe);
                return;
            }

            if (ReferenceEquals(_viewModel.Recipe, recipe))
            {
                return;
            }

            _viewModel.LoadRecipe(recipe);
            if (!ReferenceEquals(Recipe, _viewModel.Recipe))
            {
                SetCurrentValue(RecipeProperty, _viewModel.Recipe);
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PathEditorViewModel.Recipe))
            {
                if (!ReferenceEquals(Recipe, _viewModel.Recipe))
                {
                    SetCurrentValue(RecipeProperty, _viewModel.Recipe);
                }
            }
        }

        private void OnAddItem(object sender, RoutedEventArgs e)
        {
            _viewModel.AddRecipeItem();
        }

        private void OnRemoveItem(object sender, RoutedEventArgs e)
        {
            _viewModel.RemoveSelectedItem();
        }

        private void OnAddPointSegment(object sender, RoutedEventArgs e)
        {
            _viewModel.AddSegment(PathSegmentType.Point);
        }

        private void OnAddLineSegment(object sender, RoutedEventArgs e)
        {
            _viewModel.AddSegment(PathSegmentType.Line);
        }

        private void OnRemoveSegment(object sender, RoutedEventArgs e)
        {
            _viewModel.RemoveSelectedSegment();
        }

        private void OnClearSegments(object sender, RoutedEventArgs e)
        {
            _viewModel.ClearSegments();
        }

        private void OnUndo(object sender, RoutedEventArgs e)
        {
            _viewModel.Undo();
        }

        private void OnRedo(object sender, RoutedEventArgs e)
        {
            _viewModel.Redo();
        }

        private void OnImport(object sender, RoutedEventArgs e)
        {
            _viewModel.ImportRecipe();
        }

        private void OnExport(object sender, RoutedEventArgs e)
        {
            _viewModel.ExportRecipe();
        }

        private void OnLoadImage(object sender, RoutedEventArgs e)
        {
            _viewModel.LoadReferenceImage();
        }
    }
}
