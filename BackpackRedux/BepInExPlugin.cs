using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BackpackRedux
{
    [BepInPlugin("aedenthorn.BackpackRedux", "Backpack Redux", "0.5.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;
        private Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<string> backpackName;
        public static ConfigEntry<string> backpackGUID;
        public static ConfigEntry<string> backpackItem;
        public static ConfigEntry<Vector2> backpackSize;
        public static ConfigEntry<float> backpackWeightMult;
        public static ConfigEntry<bool> dropInventoryOnDeath;
        public static ConfigEntry<bool> createTombStone;
        public static ConfigEntry<bool> allowTeleportingMetal;

        //private static GameObject backpack;
        private static Container backpackContainer;
        private static Inventory backpackInventory;
        
        private static string assetPath;
        private static bool opening = false;
        private static string backpackFileName;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 1333, "Nexus mod ID for updates");


            hotKey = Config.Bind<string>("General", "HotKey", "b", "Hotkey to open backpack.");
            allowTeleportingMetal = Config.Bind<bool>("General", "AllowTeleportingMetal", false, "Allow teleporting even if backpack has unteleportable items.");
            backpackName = Config.Bind<string>("General", "BackpackName", "Backpack", "Display name for backpack.");
            backpackItem = Config.Bind<string>("General", "BackpackItem", "", "Required item to equip in order to open backpack. Leave blank to allow opening backpack without item equipped.");
            backpackSize = Config.Bind<Vector2>("General", "BackpackSize", new Vector2(6, 3), "Size of backpack (w,h).");
            backpackWeightMult = Config.Bind<float>("General", "BackpackWeightMult", 0.5f, "Multiplier for weight of items in backpack (set to 0 to disable backpack weight).");
            dropInventoryOnDeath = Config.Bind<bool>("General", "DropInventoryOnDeath", true, "Drop backpack inventory on death");
            createTombStone = Config.Bind<bool>("General", "CreateTombStone", true, "If DropInventoryOnDeath then create tombstone rather than just dropping inventory.");

            if (!modEnabled.Value)
                return;
            assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), typeof(BepInExPlugin).Namespace);
            if (!Directory.Exists(assetPath))
            {
                Dbgl("Creating mod folder");
                Directory.CreateDirectory(assetPath);
            }

            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }
        private void Start()
        {

            try
            {
                Dbgl("Searching for old backpack mod");
                var oldPlugin = Chainloader.PluginInfos.First(p => p.Key == "aedenthorn.Backpack");
                oldPlugin.Value.Instance.enabled = false;
                Dbgl("Disabled old backpack");
            }
            catch
            {
                Dbgl("Old plugin not found.");
            }
        }


        private void Update()
        {
            if (!modEnabled.Value || !Player.m_localPlayer || !ZNetScene.instance)
                return;

            if (!AedenthornUtils.IgnoreKeyPresses(true) && AedenthornUtils.CheckKeyDown(hotKey.Value) && CanOpenBackpack())
            {
                opening = true;
                OpenBackpack();
            }
        }

        private static bool CanOpenBackpack()
        {
            return backpackItem.Value == "" || Player.m_localPlayer.GetInventory().GetEquipedtems().Exists(i => i.m_dropPrefab?.name == backpackItem.Value);
        }

        private static void OpenBackpack()
        {
            /*
            backpack = Instantiate(ZNetScene.instance.GetPrefab("piece_chest"));
            if (backpack.GetComponent<WearNTear>())
                Destroy(backpack.GetComponent<WearNTear>());
            if (backpack.GetComponent<Piece>())
                Destroy(backpack.GetComponent<Piece>());
            if (backpack.transform.Find("New"))
                Destroy(backpack.transform.Find("New").gameObject);
            if (backpack.GetComponent<ZNetView>())
                Destroy(backpack.GetComponent<ZNetView>());
            backpack.transform.SetParent(Player.m_localPlayer.transform);
            backpack.transform.position = Player.m_localPlayer.transform.position;
            Container c = backpack.GetComponent<Container>();
            */
            
            backpackContainer = Player.m_localPlayer.gameObject.GetComponent<Container>();
            if(backpackContainer == null)
                backpackContainer = Player.m_localPlayer.gameObject.AddComponent<Container>();
            backpackContainer.m_name = backpackName.Value;
            //AccessTools.FieldRefAccess<Inventory, Sprite>(backpackInventory, "m_bkg") = c.m_bkg;
            AccessTools.FieldRefAccess<Container, Inventory>(backpackContainer, "m_inventory") = backpackInventory;
            InventoryGui.instance.Show(backpackContainer);

        }
        private static void LoadBackpackInventory()
        {
            backpackInventory = new Inventory(backpackName.Value, null, (int)backpackSize.Value.x, (int)backpackSize.Value.y);
            if (File.Exists(Path.Combine(assetPath, backpackFileName)))
            {
                try{
                    string input = File.ReadAllText(Path.Combine(assetPath, backpackFileName));
                    ZPackage pkg = new ZPackage(input);

                    backpackInventory.Load(pkg);
                }
                catch(Exception ex)
                {
                    Dbgl($"Backpack file corrupt!\n{ex}");
                }
            }
        }

        [HarmonyPatch(typeof(FejdStartup), "LoadMainScene")]
        static class LoadMainScene_Patch
        {
            static void Prefix(FejdStartup __instance, List<PlayerProfile> ___m_profiles)
            {
                if (!modEnabled.Value || ___m_profiles == null)
                    return;

                var profile = ___m_profiles.Find(p => p.GetFilename() == (string)typeof(Game).GetField("m_profileFilename", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));
                if (profile == null)
                    return;
                backpackFileName = Path.GetFileNameWithoutExtension(profile.GetFilename()) + "_backpack";
                LoadBackpackInventory();
            }
        }
                
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsTeleportable))]
        static class Humanoid_IsTeleportable_Patch
        {
            static void Postfix(Humanoid __instance, ref bool __result)
            {
                if (!modEnabled.Value || !__result || __instance != Player.m_localPlayer || backpackInventory == null)
                    return;

                if (!backpackInventory.IsTeleportable())
                    __result = allowTeleportingMetal.Value;
            }
        }
                
        [HarmonyPatch(typeof(InventoryGui), "Update")]
        static class InventoryGui_Update_Patch
        {
            static void Postfix(Animator ___m_animator, ref Container ___m_currentContainer)
            {
                /*
                if(!___m_animator.GetBool("visible") && backpack != null)
                {
                    backpackInventory = backpack.GetComponent<Container>().GetInventory();
                    Destroy(backpack);
                    backpack = null;
                    return;
                }
                */
                if (!modEnabled.Value || !AedenthornUtils.CheckKeyDown(hotKey.Value) || !Player.m_localPlayer || !___m_animator.GetBool("visible"))
                    return;

                if (opening)
                {
                    opening = false;
                    return;
                }

                if (___m_currentContainer != null && ___m_currentContainer == backpackContainer)
                {
                    backpackInventory = backpackContainer.GetInventory();
                    ___m_currentContainer = null;
                }
                else if (CanOpenBackpack())
                {
                    OpenBackpack();
                }
            }
        }   

        [HarmonyPatch(typeof(Inventory), "GetTotalWeight")]
        [HarmonyPriority(Priority.Last)]
        static class GetTotalWeight_Patch
        {
            static void Postfix(Inventory __instance, ref float __result)
            {
                if (!modEnabled.Value || !backpackContainer || !Player.m_localPlayer)
                    return;
                if(__instance == Player.m_localPlayer.GetInventory())
                {
                    if (new StackFrame(2).ToString().IndexOf("OverrideGetTotalWeight") > -1)
                    {
                        return;
                    }
                    __result += backpackInventory.GetTotalWeight();
                }
                else if(__instance == backpackInventory)
                {
                    __result *= backpackWeightMult.Value;
                }
            }
        }   

        [HarmonyPatch(typeof(Player), "OnDeath")]
        static class OnDeath_Patch
        {
            static void Prefix(Player __instance)
            {
                if (!modEnabled.Value || !Player.m_localPlayer || backpackInventory == null || __instance.GetPlayerID() != Player.m_localPlayer.GetPlayerID() || backpackInventory.NrOfItems() == 0)
                    return;

                if (dropInventoryOnDeath.Value)
                {
                    if (createTombStone.Value)
                    {
                        GameObject gameObject = Instantiate(__instance.m_tombstone, __instance.GetCenterPoint() + Vector3.forward, __instance.transform.rotation);
                        gameObject.GetComponent<Container>().GetInventory().MoveInventoryToGrave(backpackInventory);
                        TombStone component = gameObject.GetComponent<TombStone>();
                        PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
                        component.Setup(playerProfile.GetName(), playerProfile.GetPlayerID());
                    }
                    else
                    {
                        List<ItemDrop.ItemData> allItems = backpackInventory.GetAllItems();
                        foreach (ItemDrop.ItemData item in allItems)
                        {
                            Vector3 position = __instance.transform.position + Vector3.up * 0.5f + UnityEngine.Random.insideUnitSphere * 0.3f;
                            Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
                            ItemDrop.DropItem(item, 0, position, rotation);
                            backpackInventory.RemoveAll();
                        }
                    }
                }
            }
        }

       [HarmonyPatch(typeof(Game), "SavePlayerProfile")]
        static class SavePlayerProfile_Patch
        {
            static void Prefix()
            {
                if (!modEnabled.Value)
                    return;

                if (backpackInventory != null)
                {
                    ZPackage zpackage = new ZPackage();
                    backpackInventory.Save(zpackage);
                    string output = zpackage.GetBase64();

                    File.WriteAllText(Path.Combine(assetPath, backpackFileName), output);

                }

            }
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
                    if (Player.m_localPlayer)
                        LoadBackpackInventory();
                    __instance.AddString(text);
                    __instance.AddString($"{context.Info.Metadata.Name} config reloaded");
                    return false;
                }
                return true;
            }
        }
    }
}
