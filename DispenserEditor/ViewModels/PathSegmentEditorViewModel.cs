using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DispenserEditor.Infrastructure;
using DispenserEditor.Models;
using Microsoft.Win32;

namespace DispenserEditor.ViewModels
{
    public class PathSegmentEditorViewModel : ObservableObject
    {
        private PathSegmentRecipe _recipe;
        private PathSegmentRecipeItem _selectedItem;
        private PathSegment _selectedSegment;
        private ImageSource _referenceImage;
        private double _imageWidth = 800;
        private double _imageHeight = 600;
        private double _appliedCenterX;
        private double _appliedCenterY;
        private string _statusMessage = string.Empty;

        public PathSegmentEditorViewModel()
        {
            LoadRecipe(new PathSegmentRecipe());
        }

        public PathSegmentRecipe Recipe
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

        public ObservableCollection<PathSegmentRecipeItem> Items => Recipe?.Items;

        public PathSegmentRecipeItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (ReferenceEquals(_selectedItem, value))
                {
                    return;
                }

                if (_selectedItem != null)
                {
                    _selectedItem.PropertyChanged -= OnItemPropertyChanged;
                    _selectedItem.Segments.CollectionChanged -= OnSegmentsChanged;
                }

                _selectedItem = value;

                if (_selectedItem != null)
                {
                    _selectedItem.PropertyChanged += OnItemPropertyChanged;
                    _selectedItem.Segments.CollectionChanged += OnSegmentsChanged;
                    _appliedCenterX = _selectedItem.CenterX;
                    _appliedCenterY = _selectedItem.CenterY;
                }

                if (Recipe != null && !ReferenceEquals(Recipe.SelectedItem, value))
                {
                    Recipe.SelectedItem = value;
                }

                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Segments));
                RaisePropertyChanged(nameof(OverlayOpacity));
                RaisePropertyChanged(nameof(PixelsPerMillimetre));
                RaisePropertyChanged(nameof(AppliedCenterX));
                RaisePropertyChanged(nameof(AppliedCenterY));
            }
        }

        public ObservableCollection<PathSegment> Segments => SelectedItem?.Segments;

        public PathSegment SelectedSegment
        {
            get => _selectedSegment;
            set
            {
                if (!ReferenceEquals(_selectedSegment, value))
                {
                    _selectedSegment = value;
                    RaisePropertyChanged();
                }
            }
        }

        public double OverlayOpacity => SelectedItem?.OverlayOpacity ?? 1.0;

        public double PixelsPerMillimetre => SelectedItem?.PixelsPerMillimetre ?? 1.0;

        public double AppliedCenterX => _appliedCenterX;

        public double AppliedCenterY => _appliedCenterY;

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
                if (Math.Abs(_imageWidth - value) > double.Epsilon)
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
                if (Math.Abs(_imageHeight - value) > double.Epsilon)
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

        public void LoadRecipe(PathSegmentRecipe recipe)
        {
            if (recipe == null)
            {
                throw new ArgumentNullException(nameof(recipe));
            }

            if (!ReferenceEquals(_recipe, recipe))
            {
                if (_recipe != null)
                {
                    foreach (var item in _recipe.Items)
                    {
                        DetachItem(item);
                    }
                }

                Recipe = recipe;

                foreach (var item in recipe.Items)
                {
                    AttachItem(item);
                }
            }

            EnsureDefaultItem();
            SelectedItem = Recipe.SelectedItem ?? Recipe.Items.FirstOrDefault();
        }

        public void AddRecipeItem(string name = null)
        {
            if (Recipe == null)
            {
                return;
            }

            var itemName = string.IsNullOrWhiteSpace(name) ? $"Recipe Item {Recipe.Items.Count + 1}" : name;
            var item = new PathSegmentRecipeItem
            {
                Name = itemName,
                CenterX = _appliedCenterX,
                CenterY = _appliedCenterY
            };

            Recipe.Items.Add(item);
            SelectedItem = item;
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
            StatusMessage = $"Removed {removedName}.";
        }

        public void AddSegment(SegmentType type)
        {
            if (SelectedItem == null)
            {
                return;
            }

            var segment = new PathSegment
            {
                SegmentType = type,
                LineGroup = type == SegmentType.Line && SelectedSegment != null ? SelectedSegment.LineGroup : 0,
                X = SelectedSegment?.X ?? 0,
                Y = SelectedSegment?.Y ?? 0,
                OnDispensing = SelectedSegment?.OnDispensing ?? true,
                Speed = SelectedSegment?.Speed ?? SelectedItem.DefaultSpeed
            };

            SelectedItem.Segments.Add(segment);
            SelectedSegment = segment;
            StatusMessage = $"Added {type} segment.";
        }

        public void RemoveSelectedSegment()
        {
            if (SelectedItem == null || SelectedSegment == null)
            {
                return;
            }

            var index = SelectedItem.Segments.IndexOf(SelectedSegment);
            if (index < 0)
            {
                return;
            }

            SelectedItem.Segments.RemoveAt(index);
            if (SelectedItem.Segments.Count == 0)
            {
                SelectedSegment = null;
            }
            else
            {
                SelectedSegment = SelectedItem.Segments[Math.Min(index, SelectedItem.Segments.Count - 1)];
            }

            StatusMessage = "Removed segment.";
        }

        public void SetCenter(double centerX, double centerY)
        {
            _appliedCenterX = Math.Round(centerX, 3);
            _appliedCenterY = Math.Round(centerY, 3);

            if (SelectedItem != null)
            {
                SelectedItem.CenterX = _appliedCenterX;
                SelectedItem.CenterY = _appliedCenterY;
            }

            RaisePropertyChanged(nameof(AppliedCenterX));
            RaisePropertyChanged(nameof(AppliedCenterY));
        }

        public void LoadReferenceImage()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                using (var stream = File.OpenRead(dialog.FileName))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                    image.Freeze();
                    ReferenceImage = image;
                    ImageWidth = image.PixelWidth;
                    ImageHeight = image.PixelHeight;
                    StatusMessage = $"Loaded image '{Path.GetFileName(dialog.FileName)}'.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load image: {ex.Message}";
            }
        }

        public void ResetReferenceImage()
        {
            ReferenceImage = null;
            ImageWidth = 800;
            ImageHeight = 600;
        }

        private void EnsureDefaultItem()
        {
            if (Recipe == null)
            {
                return;
            }

            if (Recipe.Items.Count == 0)
            {
                var item = new PathSegmentRecipeItem { Name = $"Recipe Item {Recipe.Items.Count + 1}" };
                Recipe.Items.Add(item);
            }

            if (Recipe.SelectedItem == null)
            {
                Recipe.SelectedItem = Recipe.Items.FirstOrDefault();
            }
        }

        private void AttachItem(PathSegmentRecipeItem item)
        {
            if (item == null)
            {
                return;
            }

            item.PropertyChanged += OnItemPropertyChanged;
            item.Segments.CollectionChanged += OnSegmentsChanged;
        }

        private void DetachItem(PathSegmentRecipeItem item)
        {
            if (item == null)
            {
                return;
            }

            item.PropertyChanged -= OnItemPropertyChanged;
            item.Segments.CollectionChanged -= OnSegmentsChanged;
        }

        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender == SelectedItem)
            {
                if (e.PropertyName == nameof(PathSegmentRecipeItem.CenterX))
                {
                    _appliedCenterX = SelectedItem.CenterX;
                    RaisePropertyChanged(nameof(AppliedCenterX));
                }
                else if (e.PropertyName == nameof(PathSegmentRecipeItem.CenterY))
                {
                    _appliedCenterY = SelectedItem.CenterY;
                    RaisePropertyChanged(nameof(AppliedCenterY));
                }
                else if (e.PropertyName == nameof(PathSegmentRecipeItem.OverlayOpacity))
                {
                    RaisePropertyChanged(nameof(OverlayOpacity));
                }
                else if (e.PropertyName == nameof(PathSegmentRecipeItem.PixelsPerMillimetre))
                {
                    RaisePropertyChanged(nameof(PixelsPerMillimetre));
                }
            }
        }

        private void OnSegmentsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && SelectedSegment == null)
            {
                SelectedSegment = e.NewItems.OfType<PathSegment>().LastOrDefault();
            }

            RaisePropertyChanged(nameof(Segments));
        }
    }
}
