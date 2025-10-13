using System.Collections.ObjectModel;
using DispenserEditor.Infrastructure;
using Newtonsoft.Json;

namespace DispenserEditor.Models
{
    public class PathRecipe : ObservableObject
    {
        private int _schemaVersion = 2;
        private string _name = "Path Recipe";
        private PathRecipeItem _selectedItem;

        public PathRecipe()
        {
            Items = new ObservableCollection<PathRecipeItem>();
        }

        [JsonProperty("name")]
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        [JsonProperty("schemaVersion")]
        public int SchemaVersion
        {
            get => _schemaVersion;
            set => SetProperty(ref _schemaVersion, value);
        }

        [JsonProperty("items")]
        public ObservableCollection<PathRecipeItem> Items { get; }

        [JsonIgnore]
        public PathRecipeItem SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }
    }
}
