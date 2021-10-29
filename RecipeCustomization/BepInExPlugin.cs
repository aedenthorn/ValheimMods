using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace RecipeCustomization
{
    [BepInPlugin("aedenthorn.RecipeCustomization", "Recipe Customization", "0.5.1")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<float> globalArmorDurabilityLossMult;
        public static ConfigEntry<float> globalArmorMovementModMult;
        
        public static ConfigEntry<string> waterModifierName;

        private static List<RecipeData> recipeDatas = new List<RecipeData>();
        private static string assetPath;

        private enum NewDamageTypes 
        {
            Water = 1024
        }

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {

            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            nexusID = Config.Bind<int>("General", "NexusID", 1245, "Nexus mod ID for updates");
            nexusID.Value = 1245;

            assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), typeof(BepInExPlugin).Namespace);

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        [HarmonyPriority(Priority.Last)]
        static class ZNetScene_Awake_Patch
        {
            static void Postfix()
            {
                if (!modEnabled.Value)
                    return;
                context.StartCoroutine(DelayedLoadRecipes());
                //LoadAllRecipeData(true);
            }
        }
        public static IEnumerator DelayedLoadRecipes()
        {
            yield return new WaitForSeconds(0.1f);
            LoadAllRecipeData(true);
            yield break;
        }

        private static void LoadAllRecipeData(bool reload)
        {
            if(reload)
                GetRecipeDataFromFiles();
            foreach (var data in recipeDatas)
            {
                SetRecipeData(data);
            }
        }

        private static void GetRecipeDataFromFiles()
        {
            CheckModFolder();

            recipeDatas.Clear();

            foreach (string file in Directory.GetFiles(assetPath, "*.json"))
            {
                RecipeData data = JsonUtility.FromJson<RecipeData>(File.ReadAllText(file));
                recipeDatas.Add(data);
            }
        }

        private static void CheckModFolder()
        {
            if (!Directory.Exists(assetPath))
            {
                Dbgl("Creating mod folder");
                Directory.CreateDirectory(assetPath);
            }
        }

        private static void SetRecipeData(RecipeData data)
        {
            GameObject go = ObjectDB.instance.GetItemPrefab(data.name);
            if (go == null)
            {
                SetPieceRecipeData(data);
                return;
            }
            if (go.GetComponent<ItemDrop>() == null)
            {
                Dbgl($"Item data for {data.name} not found!");
                return;
            }

            for (int i = ObjectDB.instance.m_recipes.Count - 1; i > 0; i--)
            {
                if (ObjectDB.instance.m_recipes[i].m_item?.m_itemData.m_shared.m_name == go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name)
                {
                    if (data.disabled)
                    {
                        Dbgl($"Removing recipe for {data.name} from the game");
                        ObjectDB.instance.m_recipes.RemoveAt(i);
                        return;
                    }

                    ObjectDB.instance.m_recipes[i].m_amount = data.amount;
                    ObjectDB.instance.m_recipes[i].m_minStationLevel = data.minStationLevel;
                    ObjectDB.instance.m_recipes[i].m_craftingStation = GetCraftingStation(data.craftingStation);
                    List<Piece.Requirement> reqs = new List<Piece.Requirement>();
                    foreach (string req in data.reqs)
                    {
                        string[] parts = req.Split(':');
                        reqs.Add(new Piece.Requirement() { m_resItem = ObjectDB.instance.GetItemPrefab(parts[0]).GetComponent<ItemDrop>(), m_amount = int.Parse(parts[1]), m_amountPerLevel = int.Parse(parts[2]), m_recover = parts[3].ToLower() == "true" });
                    }
                    ObjectDB.instance.m_recipes[i].m_resources = reqs.ToArray();
                    return;
                }
            }
        }

        private static void SetPieceRecipeData(RecipeData data)
        {
            GameObject go = GetPieces().Find(g => Utils.GetPrefabName(g) == data.name);
            if (go == null)
            {
                Dbgl($"Item {data.name} not found!");
                return;
            }
            if (go.GetComponent<Piece>() == null)
            {
                Dbgl($"Item data for {data.name} not found!");
                return;
            }

            if (data.disabled)
            {
                Dbgl($"Removing recipe for {data.name} from the game");

                ItemDrop hammer = ObjectDB.instance.GetItemPrefab("Hammer")?.GetComponent<ItemDrop>();
                if (hammer && hammer.m_itemData.m_shared.m_buildPieces.m_pieces.Contains(go))
                {
                    hammer.m_itemData.m_shared.m_buildPieces.m_pieces.Remove(go);
                    return;
                }
                ItemDrop hoe = ObjectDB.instance.GetItemPrefab("Hoe")?.GetComponent<ItemDrop>();
                if (hoe && hoe.m_itemData.m_shared.m_buildPieces.m_pieces.Contains(go))
                {
                    hoe.m_itemData.m_shared.m_buildPieces.m_pieces.Remove(go);
                    return;
                }
            }

            go.GetComponent<Piece>().m_craftingStation = GetCraftingStation(data.craftingStation);
            List<Piece.Requirement> reqs = new List<Piece.Requirement>();
            foreach (string req in data.reqs)
            {
                string[] parts = req.Split(':');
                reqs.Add(new Piece.Requirement() { m_resItem = ObjectDB.instance.GetItemPrefab(parts[0]).GetComponent<ItemDrop>(), m_amount = int.Parse(parts[1]), m_amountPerLevel = int.Parse(parts[2]), m_recover = parts[3].ToLower() == "true" });
            }
            go.GetComponent<Piece>().m_resources = reqs.ToArray();
        }

        private static CraftingStation GetCraftingStation(string name)
        {
            if (name == "" || name == null)
                return null;

            Dbgl("Looking for crafting station " + name);

            foreach (Recipe recipe in ObjectDB.instance.m_recipes)
            {
                if (recipe?.m_craftingStation?.m_name == name)
                {
                    Dbgl("got crafting station " + name);
                    return recipe.m_craftingStation;
                }
            }
            foreach(GameObject piece in GetPieces())
            {
                if (piece.GetComponent<Piece>()?.m_craftingStation?.m_name == name)
                {
                    Dbgl("got crafting station " + name);
                    return piece.GetComponent<Piece>().m_craftingStation;
                }
            }

            return null;
        }
        private static List<GameObject> GetPieces()
        {
            var pieces = new List<GameObject>();
            if (!ObjectDB.instance)
                return pieces;

            ItemDrop hammer = ObjectDB.instance.GetItemPrefab("Hammer")?.GetComponent<ItemDrop>();

            if (hammer)
                pieces.AddRange(Traverse.Create(hammer.m_itemData.m_shared.m_buildPieces).Field("m_pieces").GetValue<List<GameObject>>());

            ItemDrop hoe = ObjectDB.instance.GetItemPrefab("Hoe")?.GetComponent<ItemDrop>();
            if (hoe)
                pieces.AddRange(Traverse.Create(hoe.m_itemData.m_shared.m_buildPieces).Field("m_pieces").GetValue<List<GameObject>>());
            return pieces;

        }
        private static RecipeData GetRecipeDataByName(string name)
        {
            GameObject go = ObjectDB.instance.GetItemPrefab(name);
            if (go == null)
            {
                return GetPieceRecipeByName(name);
            }


            ItemDrop.ItemData item = go.GetComponent<ItemDrop>().m_itemData;
            if(item == null)
            {
                Dbgl("Item data not found!");
                return null;
            }
            Recipe recipe = ObjectDB.instance.GetRecipe(item);
            if (!recipe)
            {
                if (Chainloader.PluginInfos.ContainsKey("com.jotunn.jotunn"))
                {
                    object itemManager = Chainloader.PluginInfos["com.jotunn.jotunn"].Instance.GetType().Assembly.GetType("Jotunn.Managers.ItemManager").GetProperty("Instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
                    object cr = AccessTools.Method(itemManager.GetType(), "GetRecipe").Invoke(itemManager, new[] { item.m_shared.m_name });
                    if (cr != null)
                    {
                        recipe = (Recipe)AccessTools.Property(cr.GetType(), "Recipe").GetValue(cr);
                        Dbgl($"Jotunn recipe: {item.m_shared.m_name} {recipe != null}");
                    }
                }

                if (!recipe) 
                { 
                    Dbgl($"Recipe not found for item {item.m_shared.m_name}!");
                    return null;
                }
            }

            var data = new RecipeData()
            {
                name = name,
                amount = recipe.m_amount,
                craftingStation = recipe.m_craftingStation?.m_name ?? "",
                minStationLevel = recipe.m_minStationLevel,
            };
            foreach(Piece.Requirement req in recipe.m_resources)
            {
                data.reqs.Add($"{Utils.GetPrefabName(req.m_resItem.gameObject)}:{req.m_amount}:{req.m_amountPerLevel}:{req.m_recover}");
            }

            return data;
        }

        private static RecipeData GetPieceRecipeByName(string name)
        {
            GameObject go = GetPieces().Find(g => Utils.GetPrefabName(g) == name);
            if (go == null)
            {
                Dbgl($"Item {name} not found!");
                return null;
            }
            Piece piece = go.GetComponent<Piece>();
            if (piece == null)
            {
                Dbgl("Item data not found!");
                return null;
            }
            var data = new RecipeData()
            {
                name = name,
                amount = 1,
                craftingStation = piece.m_craftingStation?.m_name ?? "",
                minStationLevel = 1,
            };
            foreach (Piece.Requirement req in piece.m_resources)
            {
                data.reqs.Add($"{Utils.GetPrefabName(req.m_resItem.gameObject)}:{req.m_amount}:{req.m_amountPerLevel}:{req.m_recover}");
            }

            return data;
        }

        [HarmonyPatch(typeof(Terminal), "InputText")]
        static class InputText_Patch
        {
            static bool Prefix(Terminal __instance)
            {
                if (!modEnabled.Value)
                    return true;

                string text = __instance.m_input.text;
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { text });
                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { $"{context.Info.Metadata.Name} config reloaded" });
                    return false;
                }
                else if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reload"))
                {
                    GetRecipeDataFromFiles();
                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { text });
                    if (ObjectDB.instance)
                    {
                        LoadAllRecipeData(true);
                        AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { $"{context.Info.Metadata.Name} reloaded recipes from files" });
                    }
                    else
                    {
                        AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { $"{context.Info.Metadata.Name} reloaded recipes from files" });
                    }
                    return false;
                }
                else if (text.ToLower().StartsWith($"{typeof(BepInExPlugin).Namespace.ToLower()} save "))
                {
                    var t = text.Split(' ');
                    string file = t[t.Length - 1];
                    RecipeData recipData = GetRecipeDataByName(file);
                    if (recipData == null)
                        return false;
                    CheckModFolder();
                    File.WriteAllText(Path.Combine(assetPath, recipData.name + ".json"), JsonUtility.ToJson(recipData));
                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { text });
                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { $"{context.Info.Metadata.Name} saved recipe data to {file}.json" });
                    return false;
                }
                else if (text.ToLower().StartsWith($"{typeof(BepInExPlugin).Namespace.ToLower()} dump "))
                {
                    var t = text.Split(' ');
                    string recipe = t[t.Length - 1];
                    RecipeData recipeData = GetRecipeDataByName(recipe);
                    if (recipeData == null)
                        return false;
                    Dbgl(JsonUtility.ToJson(recipeData));
                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { text });
                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { $"{context.Info.Metadata.Name} dumped {recipe}" });
                    return false;
                }
                else if (text.ToLower().StartsWith($"{typeof(BepInExPlugin).Namespace.ToLower()}"))
                {
                    string output = $"{context.Info.Metadata.Name} reset\r\n"
                    + $"{context.Info.Metadata.Name} reload\r\n"
                    + $"{context.Info.Metadata.Name} dump <ItemName>\r\n"
                    + $"{context.Info.Metadata.Name} save <ItemName>";

                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { text });
                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { output });
                    return false;
                }
                return true;
            }
        }
    }
}
