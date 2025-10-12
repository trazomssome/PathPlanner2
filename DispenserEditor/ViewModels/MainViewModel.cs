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
    public enum DrawingMode
    {
        Move,
        Point,
        Line
    }

    public class MainViewModel : ObservableObject
    {
        private readonly Stack<string> _undoStack = new Stack<string>();
        private readonly Stack<string> _redoStack = new Stack<string>();
        private PathFeature _selectedFeature = null;
        private PathPoint _selectedPoint = null;
        private DrawingMode _currentMode = DrawingMode.Move;
        private ImageSource _referenceImage = null;
        private double _imageWidth = 800;
        private double _imageHeight = 600;
        private string _statusMessage = string.Empty;

        public MainViewModel()
        {
            Recipe = new PathRecipe();
            Recipe.Features.CollectionChanged += (_, __) => RaisePropertyChanged(nameof(Features));
            SaveSnapshot();
        }

        public PathRecipe Recipe { get; }

        public ObservableCollection<PathFeature> Features => Recipe.Features;

        public PathFeature SelectedFeature
        {
            get => _selectedFeature;
            set
            {
                if (SetProperty(ref _selectedFeature, value))
                {
                    UpdateSelection(value);
                }
            }
        }

        public PathPoint SelectedPoint
        {
            get => _selectedPoint;
            set => SetProperty(ref _selectedPoint, value);
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
            StatusMessage = "이전 상태로 되돌렸습니다.";
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
            StatusMessage = "다시 실행했습니다.";
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
            if (SelectedFeature == null)
            {
                return;
            }

            Features.Remove(SelectedFeature);
            SaveSnapshot();
            SelectedFeature = null;
        }

        public void EnsureFeatureNames()
        {
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
            if (Math.Abs(offsetX) < double.Epsilon && Math.Abs(offsetY) < double.Epsilon)
            {
                return;
            }

            foreach (var feature in Features)
            {
                foreach (var point in feature.Points)
                {
                    point.X -= offsetX;
                    point.Y -= offsetY;
                }
            }
        }

        public void SetCenter(double x, double y)
        {
            var deltaX = x - Recipe.CenterX;
            var deltaY = y - Recipe.CenterY;
            if (Math.Abs(deltaX) < double.Epsilon && Math.Abs(deltaY) < double.Epsilon)
            {
                return;
            }

            ShiftAllPoints(deltaX, deltaY);
            Recipe.CenterX = x;
            Recipe.CenterY = y;
            SaveSnapshot();
            StatusMessage = $"센터를 ({x:F3}, {y:F3})로 이동했습니다.";
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

        private void RestoreFromJson(string json)
        {
            var restored = JsonConvert.DeserializeObject<PathRecipe>(json);
            if (restored == null)
            {
                return;
            }

            Recipe.DefaultSpeed = restored.DefaultSpeed;
            Recipe.OverlayOpacity = restored.OverlayOpacity;
            Recipe.PixelsPerMillimetre = restored.PixelsPerMillimetre;
            Recipe.CenterX = restored.CenterX;
            Recipe.CenterY = restored.CenterY;
            Recipe.SchemaVersion = restored.SchemaVersion;

            Recipe.Features.Clear();
            foreach (var feature in restored.Features)
            {
                Recipe.Features.Add(feature);
            }

            SelectedFeature = Recipe.Features.FirstOrDefault();
            StatusMessage = "Recipe restored";
        }

        private void UpdateSelection(PathFeature feature)
        {
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
    }
}
