using System;
using System.Collections.ObjectModel;
using DispenserEditor.Infrastructure;
using Newtonsoft.Json;

namespace DispenserEditor.Models
{
    public class PathSegmentRecipe : ObservableObject
    {
        private PathSegmentRecipeItem _selectedItem;

        public PathSegmentRecipe()
        {
            Items = new ObservableCollection<PathSegmentRecipeItem>();
        }

        [JsonProperty("items")]
        public ObservableCollection<PathSegmentRecipeItem> Items { get; }

        [JsonProperty("selectedItem")]
        public PathSegmentRecipeItem SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }
    }
}
