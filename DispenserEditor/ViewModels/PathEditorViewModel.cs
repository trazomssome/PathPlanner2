using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DispenserEditor.Infrastructure;
using DispenserEditor.Models;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace DispenserEditor.ViewModels
{
    public class PathEditorViewModel : ObservableObject
    {
        private readonly Stack<string> _undoStack = new Stack<string>();
        private readonly Stack<string> _redoStack = new Stack<string>();
        private PathRecipe _recipe;
        private PathRecipeItem _selectedItem;
        private PathSegment _selectedSegment;
        private ImageSource _referenceImage;
        private double _imageWidth = 800;
        private double _imageHeight = 600;
        private string _statusMessage = string.Empty;
        private bool _isRestoring;

        public PathEditorViewModel()
        {
            LoadRecipe(new PathRecipe(), initializeHistory: true);
        }

        public PathRecipe Recipe
        {
            get => _recipe;
            private set
            {
                if (!ReferenceEquals(_recipe, value))
                {
                    _recipe = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(Items));
                    RaisePropertyChanged(nameof(Segments));
                }
            }
        }

        public ObservableCollection<PathRecipeItem> Items => Recipe?.Items;

        public ObservableCollection<PathSegment> Segments => SelectedItem?.Segments;

        public PathRecipeItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    if (Recipe != null && !ReferenceEquals(Recipe.SelectedItem, value))
                    {
                        Recipe.SelectedItem = value;
                    }

                    RaisePropertyChanged(nameof(Segments));
                    RaisePropertyChanged(nameof(AppliedCenterX));
                    RaisePropertyChanged(nameof(AppliedCenterY));
                    SelectedSegment = value?.Segments.FirstOrDefault();
                }
            }
        }

        public PathSegment SelectedSegment
        {
            get => _selectedSegment;
            set => SetProperty(ref _selectedSegment, value);
        }

        public ImageSource ReferenceImage
        {
            get => _referenceImage;
            private set => SetProperty(ref _referenceImage, value);
        }

        public double ImageWidth
        {
            get => _imageWidth;
            private set => SetProperty(ref _imageWidth, value);
        }

        public double ImageHeight
        {
            get => _imageHeight;
            private set => SetProperty(ref _imageHeight, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public double AppliedCenterX => SelectedItem?.CenterX ?? 0.0;

        public double AppliedCenterY => SelectedItem?.CenterY ?? 0.0;

        public bool CanUndo => _undoStack.Count > 1;

        public bool CanRedo => _redoStack.Count > 0;

        public void LoadRecipe(PathRecipe recipe)
        {
            LoadRecipe(recipe, initializeHistory: true);
        }

        public void AddRecipeItem(string name = null)
        {
            if (Recipe == null)
            {
                return;
            }

            var itemName = string.IsNullOrWhiteSpace(name) ? $"Recipe Item {Recipe.Items.Count + 1}" : name;
            var item = new PathRecipeItem
            {
                Name = itemName,
                CenterX = SelectedItem?.CenterX ?? 0.0,
                CenterY = SelectedItem?.CenterY ?? 0.0
            };

            Recipe.Items.Add(item);
            SelectedItem = item;
            SaveSnapshot();
            StatusMessage = $"Added {item.Name}.";
        }

        public void RemoveSelectedItem()
        {
            if (Recipe == null || SelectedItem == null)
            {
                return;
            }

            if (Recipe.Items.Count <= 1)
            {
                StatusMessage = "At least one recipe item must remain.";
                return;
            }

            var index = Recipe.Items.IndexOf(SelectedItem);
            if (index < 0)
            {
                return;
            }

            var removedName = SelectedItem.Name;
            Recipe.Items.RemoveAt(index);
            var nextIndex = Math.Min(index, Recipe.Items.Count - 1);
            SelectedItem = Recipe.Items.Count > 0 ? Recipe.Items[nextIndex] : null;
            SaveSnapshot();
            StatusMessage = $"Removed {removedName}.";
        }

        public void AddSegment(PathSegmentType segmentType)
        {
            if (Segments == null)
            {
                return;
            }

            var segment = new PathSegment
            {
                SegmentType = segmentType,
                X = SelectedItem?.CenterX ?? 0.0,
                Y = SelectedItem?.CenterY ?? 0.0
            };

            Segments.Add(segment);
            SelectedSegment = segment;
            SaveSnapshot();
            StatusMessage = $"Added {segmentType} segment.";
        }

        public void RemoveSelectedSegment()
        {
            if (Segments == null || SelectedSegment == null)
            {
                return;
            }

            var index = Segments.IndexOf(SelectedSegment);
            if (index < 0)
            {
                return;
            }

            Segments.RemoveAt(index);
            SelectedSegment = Segments.Count > 0 ? Segments[Math.Min(index, Segments.Count - 1)] : null;
            SaveSnapshot();
            StatusMessage = "Removed segment.";
        }

        public void ClearSegments()
        {
            if (Segments == null || Segments.Count == 0)
            {
                return;
            }

            Segments.Clear();
            SelectedSegment = null;
            SaveSnapshot();
            StatusMessage = "Cleared segments.";
        }

        public void ShiftAllSegments(double offsetX, double offsetY)
        {
            if (Segments == null)
            {
                return;
            }

            if (Math.Abs(offsetX) < double.Epsilon && Math.Abs(offsetY) < double.Epsilon)
            {
                return;
            }

            foreach (var segment in Segments)
            {
                segment.X += offsetX;
                segment.Y += offsetY;
            }
        }

        public void RegisterSnapshot()
        {
            SaveSnapshot();
        }

        public void Undo()
        {
            if (!CanUndo)
            {
                return;
            }

            var current = _undoStack.Pop();
            _redoStack.Push(current);
            var snapshot = _undoStack.Peek();
            RestoreFromJson(snapshot);
            RaisePropertyChanged(nameof(CanUndo));
            RaisePropertyChanged(nameof(CanRedo));
            StatusMessage = "Reverted to previous state.";
        }

        public void Redo()
        {
            if (!CanRedo)
            {
                return;
            }

            var snapshot = _redoStack.Pop();
            _undoStack.Push(snapshot);
            RestoreFromJson(snapshot);
            RaisePropertyChanged(nameof(CanUndo));
            RaisePropertyChanged(nameof(CanRedo));
            StatusMessage = "Restored next state.";
        }

        public void ImportRecipe()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Recipe (*.json)|*.json|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                var json = File.ReadAllText(dialog.FileName);
                RestoreFromJson(json);
                SaveSnapshot();
                StatusMessage = $"Imported recipe from {Path.GetFileName(dialog.FileName)}.";
            }
        }

        public void ExportRecipe()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Recipe (*.json)|*.json",
                FileName = "path_recipe.json"
            };

            if (dialog.ShowDialog() == true)
            {
                var json = JsonConvert.SerializeObject(Recipe, Formatting.Indented);
                File.WriteAllText(dialog.FileName, json);
                StatusMessage = $"Exported recipe to {Path.GetFileName(dialog.FileName)}.";
            }
        }

        public void LoadReferenceImage()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (dialog.ShowDialog() == true)
            {
                var image = new BitmapImage(new Uri(dialog.FileName));
                ReferenceImage = image;
                ImageWidth = image.PixelWidth;
                ImageHeight = image.PixelHeight;
                StatusMessage = $"Loaded {Path.GetFileName(dialog.FileName)}.";
            }
        }

        private void LoadRecipe(PathRecipe recipe, bool initializeHistory)
        {
            if (recipe == null)
            {
                throw new ArgumentNullException(nameof(recipe));
            }

            if (_recipe != null)
            {
                DetachRecipe(_recipe);
            }

            Recipe = recipe;
            AttachRecipe(Recipe);
            EnsureDefaultItem();
            SelectedItem = Recipe.SelectedItem ?? Recipe.Items.FirstOrDefault();

            if (initializeHistory)
            {
                _undoStack.Clear();
                _redoStack.Clear();
                SaveSnapshot();
            }
            else
            {
                RaisePropertyChanged(nameof(CanUndo));
                RaisePropertyChanged(nameof(CanRedo));
            }
        }

        private void EnsureDefaultItem()
        {
            if (Recipe == null)
            {
                return;
            }

            if (Recipe.Items.Count == 0)
            {
                var item = new PathRecipeItem { Name = $"Recipe Item {Recipe.Items.Count + 1}" };
                Recipe.Items.Add(item);
            }

            if (Recipe.SelectedItem == null)
            {
                Recipe.SelectedItem = Recipe.Items.FirstOrDefault();
            }
        }

        private void AttachRecipe(PathRecipe recipe)
        {
            if (recipe == null)
            {
                return;
            }

            recipe.PropertyChanged += OnRecipePropertyChanged;
            recipe.Items.CollectionChanged += OnItemsCollectionChanged;
            foreach (var item in recipe.Items)
            {
                AttachItem(item);
            }
        }

        private void DetachRecipe(PathRecipe recipe)
        {
            if (recipe == null)
            {
                return;
            }

            recipe.PropertyChanged -= OnRecipePropertyChanged;
            recipe.Items.CollectionChanged -= OnItemsCollectionChanged;
            foreach (var item in recipe.Items)
            {
                DetachItem(item);
            }
        }

        private void AttachItem(PathRecipeItem item)
        {
            if (item == null)
            {
                return;
            }

            item.PropertyChanged += OnItemPropertyChanged;
            item.Segments.CollectionChanged += OnSegmentsCollectionChanged;
            foreach (var segment in item.Segments)
            {
                segment.PropertyChanged += OnSegmentPropertyChanged;
            }
        }

        private void DetachItem(PathRecipeItem item)
        {
            if (item == null)
            {
                return;
            }

            item.PropertyChanged -= OnItemPropertyChanged;
            item.Segments.CollectionChanged -= OnSegmentsCollectionChanged;
            foreach (var segment in item.Segments)
            {
                segment.PropertyChanged -= OnSegmentPropertyChanged;
            }
        }

        private void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (PathRecipeItem item in e.OldItems)
                {
                    DetachItem(item);
                }
            }

            if (e.NewItems != null)
            {
                foreach (PathRecipeItem item in e.NewItems)
                {
                    AttachItem(item);
                }
            }

            RaisePropertyChanged(nameof(Items));
            SaveSnapshot();
        }

        private void OnSegmentsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (PathSegment segment in e.OldItems)
                {
                    segment.PropertyChanged -= OnSegmentPropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (PathSegment segment in e.NewItems)
                {
                    segment.PropertyChanged += OnSegmentPropertyChanged;
                }
            }

            RaisePropertyChanged(nameof(Segments));

            if (Segments != null && !Segments.Contains(SelectedSegment))
            {
                SelectedSegment = Segments.LastOrDefault();
            }

            SaveSnapshot();
        }

        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PathRecipeItem.CenterX))
            {
                RaisePropertyChanged(nameof(AppliedCenterX));
            }
            else if (e.PropertyName == nameof(PathRecipeItem.CenterY))
            {
                RaisePropertyChanged(nameof(AppliedCenterY));
            }

            if (!_isRestoring)
            {
                SaveSnapshot();
            }
        }

        private void OnSegmentPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isRestoring)
            {
                return;
            }

            SaveSnapshot();
        }

        private void OnRecipePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PathRecipe.SelectedItem))
            {
                if (!ReferenceEquals(SelectedItem, Recipe.SelectedItem))
                {
                    SelectedItem = Recipe.SelectedItem;
                }
            }
        }

        private void SaveSnapshot()
        {
            if (_isRestoring || Recipe == null)
            {
                return;
            }

            var json = JsonConvert.SerializeObject(Recipe, Formatting.Indented);
            if (_undoStack.Count == 0 || _undoStack.Peek() != json)
            {
                _undoStack.Push(json);
            }

            _redoStack.Clear();
            RaisePropertyChanged(nameof(CanUndo));
            RaisePropertyChanged(nameof(CanRedo));
        }

        private void RestoreFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            var recipe = JsonConvert.DeserializeObject<PathRecipe>(json);
            if (recipe == null)
            {
                return;
            }

            try
            {
                _isRestoring = true;
                LoadRecipe(recipe, initializeHistory: false);
            }
            finally
            {
                _isRestoring = false;
            }
        }
    }
}
