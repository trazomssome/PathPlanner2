using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows.Forms;

namespace DispensePath
{
    // 동작 정의
    public enum MotorAction
    {
        Figuer1 = 0,
        Figuer2,
        Figuer3,
        Figuer4,
        Point,
        GrabBefor,
        GrabInspection
    }

    public class Recipe_Align
    {
        public float PixelSize_X { get; set; }
        public float PixelSize_Y { get; set; }
        public int DefaultZ { get; set; }
        public int ActiveZ { get; set; }
        public int ActiveLimitZ { get; set; }
        public int ZUpSpeed { get; set; }
        public int ZDownSpeed { get; set; }
        public string Name { get; set; }
    }
        [Serializable]
    public class SpecLVDT
    {
        public bool Use { get; set; } = false;
        public float Min { get; set; } = -10.0F;
        public float Max { get; set; } = 10.0F;

        public bool IsSpecIn(float value)
        {
            if (Use == false) return true;
            if (value < Min) return false;
            if (value > Max) return false;

            return true;
        }
    }

    [Serializable]
    public class Recipe_LVDT
    {
        public SpecLVDT Spec1 { get; set; } = new SpecLVDT();
        public SpecLVDT Spec2 { get; set; } = new SpecLVDT();
        public SpecLVDT Spec3 { get; set; } = new SpecLVDT();
        public SpecLVDT Spec4 { get; set; } = new SpecLVDT();
        public SpecLVDT Spec5 { get; set; } = new SpecLVDT();

        public Plane OffSetZ { get; set; } = new Plane();

        public Recipe_LVDT()
        {
        }

    }

    [Serializable]
    public class Setting_Recipe
    {
        public string ModelName { get; set; } = "TEST";
        public string FileName {get; set; } = "Setting";
        public Recipe_Align Align { get; set; } = new Recipe_Align();
        public Recipe_GraphicObject GraphicObjects { get; set; } = new Recipe_GraphicObject();
        public Recipe_LVDT LVDT { get; set; } = new Recipe_LVDT();


        public Setting_Recipe(string modelName, string fileName)
        {
            ModelName = modelName;
            FileName = fileName;
        }

        public Setting_Recipe Load(string modelName, string fileName)
        {
            string path = $"D:\\[SGMachineFDP]\\Recipe\\_config_recipe\\{fileName}.json";
            //string equipmentPath = $"{Application.StartupPath}\\IMAGE\\{modelName}\\Equipment.json";

            Setting_Recipe newData = null;
            if (File.Exists(path))
            {
                try
                {
                    newData = JsonSerializer.Deserialize<Setting_Recipe>(File.ReadAllText(path));
                }
                catch (Exception ex)
                {
                }

                if (newData != null)
                    return newData;
            }

            newData = new Setting_Recipe(modelName, fileName);
            //newData.Save(recipeName);
            return newData;
        }

        public void Save(string recipeName, string fileName)
        {
            //string path = $"{Application.StartupPath}\\RECIPE\\{recipeName}\\Setting.json";
            //Directory.CreateDirectory($"{Application.StartupPath}\\RECIPE\\{recipeName}\\");

            //MainRecipe가 이터널이라 보호수준으로 FileName을 가져올수 없음
            //IAuroraEnvironment Aurora = AuroraApp.Current.AuroraEnv();
            //var recipe = Aurora.RecipeManager["MainRecipe"] as MainRecipe;

            string path = $"D:\\[SGMachineFDP]\\Recipe\\_config_recipe\\{fileName}.json";
            Directory.CreateDirectory($"D:\\[SGMachineFDP]\\Recipe\\_config_recipe\\{recipeName}\\");

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                WriteIndented = true
            };

            string currRecipe;

            try
            {
                currRecipe = JsonSerializer.Serialize(this, options);

                if (File.Exists(path))
                {
                    //이전값과 비교하여 바뀐 부분 로깅
                    string prevRecipe = File.ReadAllText(path);

                    JObject previousObject = JObject.Parse(prevRecipe);
                    JObject currentObject = JObject.Parse(currRecipe);

                    var result = JToken.DeepEquals(previousObject, currentObject);

                    if (!result)
                    {
                        foreach (var item in previousObject)
                        {
                            if (!JToken.DeepEquals(item.Value, currentObject[item.Key]))
                            {
                                //CLogger.Add(LOG.NORMAL, $"Property '{item.Key}' changed from '{item.Value}' to '{currentObject[item.Key]}'");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("JSON objects are equal");
                    }
                }


                File.WriteAllText(path, currRecipe);
            }
            catch (JsonException ex)
            {
                options.IgnoreNullValues = true;
                currRecipe = JsonSerializer.Serialize(this, options);
                File.WriteAllText(path, currRecipe);

                //CLogger.Add(LOG.EXCEPTION, "[FAILED] {0}==>{1}   Execption ==> {2}", MethodBase.GetCurrentMethod().ReflectedType.Name, MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
            catch (Exception ex)
            {
                //CLogger.Add(LOG.EXCEPTION, "[FAILED] {0}==>{1}   Execption ==> {2}", MethodBase.GetCurrentMethod().ReflectedType.Name, MethodBase.GetCurrentMethod().Name, ex.ToString());
            }

        }
    }
}
