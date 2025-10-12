using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace DispenserEditor.Models
{
    public class IntegratedRecipe : ICloneable
    {
        public string SchemaVersion { get; set; } = "1.0";
        public ObservableCollection<DispenseRecipe> Recipes { get; set; } = new ObservableCollection<DispenseRecipe>();

        public object Clone()
        {
            var clone = (IntegratedRecipe)MemberwiseClone();
            clone.Recipes = new ObservableCollection<DispenseRecipe>(
                Recipes.Select(r => (DispenseRecipe)r.Clone()));
            return clone;
        }
    }
}


