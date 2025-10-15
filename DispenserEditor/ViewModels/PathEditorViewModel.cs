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
    public enum DrawingMode
    {
        Move,
        Point,
        Line
    }

    public enum CrosshairMoveMode
    {
        KeepPointsFixed,
        MovePointsWithCrosshair
    }

    public class PathEditorViewModel : ObservableObject
    {
        private readonly Stack<string> _undoStack = new Stack<string>();
        private readonly Stack<string> _redoStack = new Stack<string>();
        private PathRecipe _recipe;
        private PathRecipeItem _selectedItem = null;
        private PathFeature _selectedFeature = null;
        private PathPoint _selectedPoint = null;
        private FeatureListEntry _selectedEntry = null;
        private bool _synchronizingSelection;
        private bool _isUpdatingFeatureEntries;
        private DrawingMode _currentMode = DrawingMode.Move;
        private ImageSource _referenceImage = null;
        private double _imageWidth = 800;
        private double _imageHeight = 600;
        private string _statusMessage = string.Empty;
        private double _appliedCenterX;
        private double _appliedCenterY;
        private CrosshairMoveMode _crosshairMode = CrosshairMoveMode.KeepPointsFixed;
        private bool _showLinePoints = true;

        public PathEditorViewModel()
        {
            FeatureEntries = new ObservableCollection<FeatureListEntry>();
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
                    RaisePropertyChanged(nameof(Features));
                }
            }
        }

        public ObservableCollection<PathRecipeItem> Items => Recipe?.Items;

        public PathRecipeItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (ReferenceEquals(_selectedItem, value))
                {
                    return;
                }

                _selectedItem = value;

                _appliedCenterX = _selectedItem?.CenterX ?? 0;
                _appliedCenterY = _selectedItem?.CenterY ?? 0;

                if (Recipe != null && !ReferenceEquals(Recipe.SelectedItem, value))
                {
                    Recipe.SelectedItem = value;
                }

                RaisePropertyChanged(nameof(SelectedItem));
                RaisePropertyChanged(nameof(Features));
                RaisePropertyChanged(nameof(AppliedCenterX));
                RaisePropertyChanged(nameof(AppliedCenterY));

                UpdateFeatureEntries();
                SelectedFeature = SelectedItem?.Features.FirstOrDefault();
            }
        }

        public ObservableCollection<PathFeature> Features => SelectedItem?.Features;

        public ObservableCollection<FeatureListEntry> FeatureEntries { get; }

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
                CenterX = _appliedCenterX,
                CenterY = _appliedCenterY
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
            SelectedItem = Recipe.Items[nextIndex];
            SaveSnapshot();
            StatusMessage = $"Removed {removedName}.";
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

        private void LoadRecipe(PathRecipe recipe, bool initializeHistory)
        {
            if (recipe == null)
            {
                throw new ArgumentNullException(nameof(recipe));
            }

            if (ReferenceEquals(_recipe, recipe))
            {
                return;
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
        }

        public PathFeature SelectedFeature
        {
            get => _selectedFeature;
            set
            {
                if (SetProperty(ref _selectedFeature, value))
                {
                    UpdateSelection(value);
                    SynchronizeSelectedEntry(value);
                }
            }
        }

        public PathPoint SelectedPoint
        {
            get => _selectedPoint;
            set => SetProperty(ref _selectedPoint, value);
        }

        public FeatureListEntry SelectedEntry
        {
            get => _selectedEntry;
            set => UpdateSelectedEntry(value, true);
        }

        public DrawingMode CurrentMode
        {
            get => _currentMode;
            set => SetProperty(ref _currentMode, value);
        }

        public ImageSource ReferenceImage
        {
            get => _referenceImage;
            private set => SetProperty(ref _referenceImage, value);
        }

        public double AppliedCenterX => _appliedCenterX;

        public double AppliedCenterY => _appliedCenterY;

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

        public bool ShowLinePoints
        {
            get => _showLinePoints;
            set
            {
                if (SetProperty(ref _showLinePoints, value))
                {
                    StatusMessage = value ? "Line points visible." : "Line points hidden.";
                }
            }
        }

        public CrosshairMoveMode CrosshairMode
        {
            get => _crosshairMode;
            set => SetProperty(ref _crosshairMode, value);
        }

        public bool CanUndo => _undoStack.Count > 1;
        public bool CanRedo => _redoStack.Count > 0;

        public void SaveSnapshot()
        {
            var json = JsonConvert.SerializeObject(Recipe, Formatting.Indented);
            if (_undoStack.Count == 0 || _undoStack.Peek() != json)
            {
                _undoStack.Push(json);
            }
            _redoStack.Clear();
            RaisePropertyChanged(nameof(CanUndo));
            RaisePropertyChanged(nameof(CanRedo));
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
                StatusMessage = $"Imported recipe from {Path.GetFileName(dialog.FileName)}";
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
                StatusMessage = $"Exported recipe to {Path.GetFileName(dialog.FileName)}";
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
                StatusMessage = $"Loaded {Path.GetFileName(dialog.FileName)}";
            }
        }

        public void RemoveSelectedFeature()
        {
            if (Features == null || SelectedFeature == null)
            {
                return;
            }

            Features.Remove(SelectedFeature);
            SaveSnapshot();
            SelectedFeature = null;
        }

        public void EnsureFeatureNames()
        {
            if (Features == null)
            {
                return;
            }

            for (int i = 0; i < Features.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(Features[i].Name))
                {
                    Features[i].Name = $"Feature {i + 1}";
                }
            }
        }

        public void ShiftAllPoints(double offsetX, double offsetY)
        {
            if (Features == null)
            {
                return;
            }

            if (Math.Abs(offsetX) < double.Epsilon && Math.Abs(offsetY) < double.Epsilon)
            {
                return;
            }

            foreach (var feature in Features)
            {
                foreach (var point in feature.Points)
                {
                    point.X += offsetX;
                    point.Y += offsetY;
                }
            }
        }

        public void SetCenter(double x, double y, bool commit = true, bool updateStatus = true)
        {
            var deltaX = x - _appliedCenterX;
            var deltaY = y - _appliedCenterY;
            if (Math.Abs(deltaX) < double.Epsilon && Math.Abs(deltaY) < double.Epsilon)
            {
                return;
            }

            if (CrosshairMode == CrosshairMoveMode.KeepPointsFixed)
            {
                ShiftAllPoints(deltaX, deltaY);
            }

            if (SelectedItem == null)
            {
                return;
            }

            SelectedItem.CenterX = x;
            SelectedItem.CenterY = y;
            _appliedCenterX = SelectedItem.CenterX;
            _appliedCenterY = SelectedItem.CenterY;
            RaisePropertyChanged(nameof(AppliedCenterX));
            RaisePropertyChanged(nameof(AppliedCenterY));
            if (commit)
            {
                SaveSnapshot();
            }

            if (updateStatus)
            {
                StatusMessage = $"Center moved to ({x:F3}, {y:F3}).";
            }
        }

        public void UpdateMode(DrawingMode mode)
        {
            CurrentMode = mode;
            StatusMessage = $"Mode switched to {mode}";
        }

        public void RegisterSnapshot()
        {
            SaveSnapshot();
        }

        private void AttachRecipe(PathRecipe recipe)
        {
            recipe.Items.CollectionChanged += OnItemsCollectionChanged;
            foreach (var item in recipe.Items)
            {
                AttachItem(item);
            }
        }

        private void DetachRecipe(PathRecipe recipe)
        {
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
            item.Features.CollectionChanged += OnFeaturesCollectionChanged;
            foreach (var feature in item.Features)
            {
                AttachFeature(feature);
            }
        }

        private void DetachItem(PathRecipeItem item)
        {
            if (item == null)
            {
                return;
            }

            item.PropertyChanged -= OnItemPropertyChanged;
            item.Features.CollectionChanged -= OnFeaturesCollectionChanged;
            foreach (var feature in item.Features)
            {
                DetachFeature(feature);
            }
        }

        private void AttachFeature(PathFeature feature)
        {
            feature.PropertyChanged += OnFeaturePropertyChanged;
            feature.Points.CollectionChanged += OnFeaturePointsChanged;
            foreach (var point in feature.Points)
            {
                point.PropertyChanged += OnPointPropertyChanged;
            }
        }

        private void DetachFeature(PathFeature feature)
        {
            feature.PropertyChanged -= OnFeaturePropertyChanged;
            feature.Points.CollectionChanged -= OnFeaturePointsChanged;
            foreach (var point in feature.Points)
            {
                point.PropertyChanged -= OnPointPropertyChanged;
            }
        }

        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!ReferenceEquals(sender, SelectedItem))
            {
                return;
            }

            if (e.PropertyName == nameof(PathRecipeItem.CenterX) || e.PropertyName == nameof(PathRecipeItem.CenterY))
            {
                _appliedCenterX = SelectedItem.CenterX;
                _appliedCenterY = SelectedItem.CenterY;
                RaisePropertyChanged(nameof(AppliedCenterX));
                RaisePropertyChanged(nameof(AppliedCenterY));
            }
            else if (e.PropertyName == nameof(PathRecipeItem.OverlayOpacity) ||
                     e.PropertyName == nameof(PathRecipeItem.PixelsPerMillimetre))
            {
                UpdateFeatureEntries();
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

            EnsureDefaultItem();
            if (!Recipe.Items.Contains(SelectedItem))
            {
                var candidate = Recipe.SelectedItem;
                if (candidate == null || !Recipe.Items.Contains(candidate))
                {
                    candidate = Recipe.Items.FirstOrDefault();
                }

                SelectedItem = candidate;
            }

            RaisePropertyChanged(nameof(Items));
        }

        private void OnFeaturesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (PathFeature feature in e.OldItems)
                {
                    DetachFeature(feature);
                }
            }

            if (e.NewItems != null)
            {
                foreach (PathFeature feature in e.NewItems)
                {
                    AttachFeature(feature);
                }
            }

            if (ReferenceEquals(sender, SelectedItem?.Features))
            {
                RaisePropertyChanged(nameof(Features));
                UpdateFeatureEntries();
            }
        }

        private void OnFeaturePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateFeatureEntries();
        }

        private void OnFeaturePointsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (PathPoint point in e.OldItems)
                {
                    point.PropertyChanged -= OnPointPropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (PathPoint point in e.NewItems)
                {
                    point.PropertyChanged += OnPointPropertyChanged;
                }
            }

            UpdateFeatureEntries();
        }

        private void OnPointPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateFeatureEntries();
        }

        private void RestoreFromJson(string json)
        {
            var restored = JsonConvert.DeserializeObject<PathRecipe>(json);
            if (restored == null)
            {
                return;
            }

            var previousSelection = Recipe.SelectedItem;
            var previousIndex = previousSelection != null ? Recipe.Items.IndexOf(previousSelection) : -1;

            DetachRecipe(Recipe);

            Recipe.Name = restored.Name;
            Recipe.SchemaVersion = restored.SchemaVersion;

            Recipe.SelectedItem = null;

            Recipe.Items.Clear();
            foreach (var item in restored.Items)
            {
                Recipe.Items.Add(item);
            }

            AttachRecipe(Recipe);
            EnsureDefaultItem();

            PathRecipeItem targetSelection = null;
            if (previousSelection != null)
            {
                if (previousIndex >= 0 && previousIndex < Recipe.Items.Count)
                {
                    targetSelection = Recipe.Items[previousIndex];
                }

                if (targetSelection == null)
                {
                    targetSelection = Recipe.Items.FirstOrDefault(item => string.Equals(item.Name, previousSelection.Name, StringComparison.Ordinal));
                }
            }

            if (targetSelection == null)
            {
                targetSelection = Recipe.SelectedItem ?? Recipe.Items.FirstOrDefault();
            }

            if (_selectedItem != null)
            {
                SelectedItem = null;
            }

            SelectedItem = targetSelection ?? Recipe.Items.FirstOrDefault();

            if (SelectedItem != null)
            {
                _appliedCenterX = SelectedItem.CenterX;
                _appliedCenterY = SelectedItem.CenterY;
                RaisePropertyChanged(nameof(AppliedCenterX));
                RaisePropertyChanged(nameof(AppliedCenterY));
            }

            SelectedFeature = SelectedItem?.Features.FirstOrDefault();
            StatusMessage = "Recipe restored";
            RaisePropertyChanged(nameof(Items));
            RaisePropertyChanged(nameof(Features));
            UpdateFeatureEntries();
        }

        private void UpdateSelection(PathFeature feature)
        {
            if (Features == null)
            {
                SelectedPoint = null;
                return;
            }

            foreach (var f in Features)
            {
                f.IsSelected = f == feature;
            }

            if (feature != null)
            {
                SelectedPoint = feature.Points.FirstOrDefault();
            }
            else
            {
                SelectedPoint = null;
            }
        }

        private void UpdateFeatureEntries()
        {
            if (_isUpdatingFeatureEntries)
            {
                return;
            }

            _isUpdatingFeatureEntries = true;

            var previousEntry = _selectedEntry;

            try
            {
                foreach (var entry in FeatureEntries)
                {
                    entry.Dispose();
                }
                FeatureEntries.Clear();

                if (Features == null)
                {
                    UpdateSelectedEntry(null, false);
                    return;
                }

                foreach (var feature in Features)
                {
                    if (feature.Type == PathFeatureType.Line)
                    {
                        for (int i = 0; i < feature.Points.Count; i++)
                        {
                            var point = feature.Points[i];
                            FeatureEntries.Add(FeatureListEntry.ForLinePoint(feature, point, i));
                        }
                    }
                    else
                    {
                        var point = feature.Points.FirstOrDefault();
                        FeatureEntries.Add(FeatureListEntry.ForPoint(feature, point));
                    }
                }
            
                FeatureListEntry target = null;
                if (previousEntry != null)
                {
                    target = FeatureEntries.FirstOrDefault(entry =>
                        ReferenceEquals(entry.Feature, previousEntry.Feature) &&
                        entry.SegmentIndex == previousEntry.SegmentIndex &&
                        ReferenceEquals(entry.Point, previousEntry.Point));
                }
                if (target == null && SelectedFeature != null)
                {
                    target = FeatureEntries.FirstOrDefault(entry => ReferenceEquals(entry.Feature, SelectedFeature));
                }

                if (target == null && FeatureEntries.Count > 0)
                {
                    target = FeatureEntries.First();
                }

                UpdateSelectedEntry(target, false);
            }
            finally
            {
                _isUpdatingFeatureEntries = false;
            }
        }

        private void UpdateSelectedEntry(FeatureListEntry entry, bool updateFeature)
        {
            if (ReferenceEquals(_selectedEntry, entry))
            {
                return;
            }

            _selectedEntry = entry;
            RaisePropertyChanged(nameof(SelectedEntry));

            if (updateFeature && !_synchronizingSelection)
            {
                try
                {
                    _synchronizingSelection = true;
                    SelectedFeature = entry?.Feature;
                }
                finally
                {
                    _synchronizingSelection = false;
                }
            }
        }

        private void SynchronizeSelectedEntry(PathFeature feature)
        {
            if (_synchronizingSelection)
            {
                return;
            }

            try
            {
                _synchronizingSelection = true;
                var entry = FeatureEntries.FirstOrDefault(item => ReferenceEquals(item.Feature, feature));
                UpdateSelectedEntry(entry, false);
            }
            finally
            {
                _synchronizingSelection = false;
            }
        }
    }
}
