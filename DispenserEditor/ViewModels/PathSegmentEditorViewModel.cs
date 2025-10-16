using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public class PathSegmentEditorViewModel : ObservableObject
    {
        private readonly Stack<string> _undoStack = new Stack<string>();
        private readonly Stack<string> _redoStack = new Stack<string>();
        private PathRecipe _recipe;
        private PathRecipeItem _selectedItem;
        private PathSegment _selectedSegment;
        private DrawingMode _currentMode = DrawingMode.Move;
        private ImageSource _referenceImage;
        private double _imageWidth = 800;
        private double _imageHeight = 600;
        private string _statusMessage = string.Empty;
        private double _appliedCenterX;
        private double _appliedCenterY;
        private CrosshairMoveMode _crosshairMode = CrosshairMoveMode.KeepPointsFixed;
        private bool _showLinePoints = true;

        public PathSegmentEditorViewModel()
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

                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Segments));
                RaisePropertyChanged(nameof(AppliedCenterX));
                RaisePropertyChanged(nameof(AppliedCenterY));

                SelectedSegment = Segments?.FirstOrDefault();
            }
        }

        public ObservableCollection<PathSegment> Segments => SelectedItem?.Segments;

        public PathSegment SelectedSegment
        {
            get => _selectedSegment;
            set
            {
                if (ReferenceEquals(_selectedSegment, value))
                {
                    return;
                }

                _selectedSegment = value;
                RaisePropertyChanged();
            }
        }

        public DrawingMode CurrentMode
        {
            get => _currentMode;
            set
            {
                if (_currentMode != value)
                {
                    _currentMode = value;
                    RaisePropertyChanged();
                }
            }
        }

        public ImageSource ReferenceImage
        {
            get => _referenceImage;
            private set
            {
                if (!Equals(_referenceImage, value))
                {
                    _referenceImage = value;
                    RaisePropertyChanged();
                }
            }
        }

        public double ImageWidth
        {
            get => _imageWidth;
            private set
            {
                if (!Equals(_imageWidth, value))
                {
                    _imageWidth = value;
                    RaisePropertyChanged();
                }
            }
        }

        public double ImageHeight
        {
            get => _imageHeight;
            private set
            {
                if (!Equals(_imageHeight, value))
                {
                    _imageHeight = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    RaisePropertyChanged();
                }
            }
        }

        public double AppliedCenterX
        {
            get => _appliedCenterX;
            set
            {
                if (!Equals(_appliedCenterX, value))
                {
                    _appliedCenterX = value;
                    RaisePropertyChanged();
                }
            }
        }

        public double AppliedCenterY
        {
            get => _appliedCenterY;
            set
            {
                if (!Equals(_appliedCenterY, value))
                {
                    _appliedCenterY = value;
                    RaisePropertyChanged();
                }
            }
        }

        public CrosshairMoveMode CrosshairMode
        {
            get => _crosshairMode;
            set
            {
                if (_crosshairMode != value)
                {
                    _crosshairMode = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool ShowLinePoints
        {
            get => _showLinePoints;
            set
            {
                if (_showLinePoints != value)
                {
                    _showLinePoints = value;
                    RaisePropertyChanged();
                }
            }
        }

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

        public void AddSegment(PathSegment segment = null)
        {
            if (Segments == null)
            {
                return;
            }

            var newSegment = segment ?? new PathSegment
            {
                SegmentType = CurrentMode == DrawingMode.Line ? SegmentType.Line : SegmentType.Point,
                LineGroup = DetermineLineGroup(),
                X = 0,
                Y = 0
            };

            Segments.Add(newSegment);
            SelectedSegment = newSegment;
            SaveSnapshot();
            StatusMessage = $"Added {newSegment.SegmentType} segment.";
        }

        public void InsertSegment(int index, PathSegment segment)
        {
            if (Segments == null)
            {
                return;
            }

            if (index < 0 || index > Segments.Count)
            {
                index = Segments.Count;
            }

            Segments.Insert(index, segment);
            SelectedSegment = segment;
            SaveSnapshot();
            StatusMessage = $"Inserted {segment.SegmentType} segment.";
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

            var removedType = SelectedSegment.SegmentType;
            Segments.RemoveAt(index);
            SelectedSegment = Segments.ElementAtOrDefault(Math.Min(index, Segments.Count - 1));
            SaveSnapshot();
            StatusMessage = $"Removed {removedType} segment.";
        }

        public void DuplicateSelectedSegment()
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

            var clone = CloneSegment(SelectedSegment);
            Segments.Insert(index + 1, clone);
            SelectedSegment = clone;
            SaveSnapshot();
            StatusMessage = "Duplicated segment.";
        }

        public void MoveSegment(int oldIndex, int newIndex)
        {
            if (Segments == null)
            {
                return;
            }

            if (oldIndex < 0 || oldIndex >= Segments.Count)
            {
                return;
            }

            if (newIndex < 0 || newIndex >= Segments.Count)
            {
                newIndex = Math.Max(0, Math.Min(newIndex, Segments.Count - 1));
            }

            if (oldIndex == newIndex)
            {
                return;
            }

            var segment = Segments[oldIndex];
            Segments.RemoveAt(oldIndex);
            Segments.Insert(newIndex, segment);
            SelectedSegment = segment;
            SaveSnapshot();
        }

        public void LoadImageFromDisk()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp|All Files|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                LoadImage(dialog.FileName);
            }
        }

        public void LoadImage(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                StatusMessage = "Image file not found.";
                return;
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path);
                bitmap.EndInit();
                bitmap.Freeze();

                ReferenceImage = bitmap;
                ImageWidth = bitmap.PixelWidth;
                ImageHeight = bitmap.PixelHeight;
                StatusMessage = $"Loaded image '{Path.GetFileName(path)}'.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load image: {ex.Message}";
            }
        }

        public void SaveSnapshot()
        {
            if (Recipe == null)
            {
                return;
            }

            var json = JsonConvert.SerializeObject(Recipe);
            _undoStack.Push(json);
            _redoStack.Clear();
        }

        public void Undo()
        {
            if (_undoStack.Count <= 1)
            {
                return;
            }

            var current = _undoStack.Pop();
            _redoStack.Push(current);
            var previous = _undoStack.Peek();
            RestoreFromJson(previous);
            StatusMessage = "Undo performed.";
        }

        public void Redo()
        {
            if (_redoStack.Count == 0)
            {
                return;
            }

            var next = _redoStack.Pop();
            _undoStack.Push(next);
            RestoreFromJson(next);
            StatusMessage = "Redo performed.";
        }

        public void ImportRecipeFromFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Path Recipe (*.json)|*.json|All Files|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                LoadRecipeFromFile(dialog.FileName);
            }
        }

        public void LoadRecipeFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                StatusMessage = "Recipe file not found.";
                return;
            }

            try
            {
                var json = File.ReadAllText(path);
                var recipe = JsonConvert.DeserializeObject<PathRecipe>(json);
                LoadRecipe(recipe ?? new PathRecipe());
                StatusMessage = $"Loaded recipe '{Path.GetFileName(path)}'.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load recipe: {ex.Message}";
            }
        }

        public void ExportRecipeToFile()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Path Recipe (*.json)|*.json|All Files|*.*",
                FileName = "recipe.json"
            };

            if (dialog.ShowDialog() == true)
            {
                SaveRecipeToFile(dialog.FileName);
            }
        }

        public void SaveRecipeToFile(string path)
        {
            if (Recipe == null || string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                var json = JsonConvert.SerializeObject(Recipe, Formatting.Indented);
                File.WriteAllText(path, json);
                StatusMessage = $"Saved recipe to '{Path.GetFileName(path)}'.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to save recipe: {ex.Message}";
            }
        }

        private void LoadRecipe(PathRecipe recipe, bool initializeHistory)
        {
            if (recipe == null)
            {
                throw new ArgumentNullException(nameof(recipe));
            }

            Recipe = recipe;
            EnsureDefaultItem();
            SelectedItem = Recipe.SelectedItem ?? Recipe.Items.FirstOrDefault();

            if (initializeHistory)
            {
                _undoStack.Clear();
                _redoStack.Clear();
                SaveSnapshot();
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

        private int DetermineLineGroup()
        {
            if (Segments == null || Segments.Count == 0)
            {
                return 0;
            }

            return Segments.Max(s => s.LineGroup);
        }

        private static PathSegment CloneSegment(PathSegment segment)
        {
            var json = JsonConvert.SerializeObject(segment);
            return JsonConvert.DeserializeObject<PathSegment>(json);
        }

        private void RestoreFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            var recipe = JsonConvert.DeserializeObject<PathRecipe>(json);
            LoadRecipe(recipe ?? new PathRecipe(), initializeHistory: false);
        }
    }
}
