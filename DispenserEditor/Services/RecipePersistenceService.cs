using System.IO;
using Newtonsoft.Json;
using DispenserEditor.Models;

namespace DispenserEditor.Services;

public static class RecipePersistenceService
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore
    };

    public static void Save(string path, IntegratedRecipe recipe)
    {
        var json = JsonConvert.SerializeObject(recipe, Settings);
        File.WriteAllText(path, json);
    }

    public static IntegratedRecipe Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<IntegratedRecipe>(json, Settings)
               ?? new IntegratedRecipe();
    }
}
