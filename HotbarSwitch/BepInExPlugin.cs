using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace HotbarSwitch
{
    [BepInPlugin("aedenthorn.HotbarSwitch", "Hotbar Switch", "0.2.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<int> rowsToSwitch;
        public static ConfigEntry<string> hotKey;

        //public static int currentRow = 0;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 535, "Nexus mod ID for updates");
            rowsToSwitch = Config.Bind<int>("General", "RowsToSwitch", 2, "Rows of inventory to switch via hotkey");
            hotKey = Config.Bind<string>("General", "HotKey", "`", "Hotkey to initiate switch. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        public void Update()
        {
            if (modEnabled.Value && !AedenthornUtils.IgnoreKeyPresses(true) && AedenthornUtils.CheckKeyDown(hotKey.Value)) 
            {
                int gridHeight = Player.m_localPlayer.GetInventory().GetHeight();
                int rows = Math.Max(1, Math.Min(gridHeight, rowsToSwitch.Value));

                List<ItemDrop.ItemData> items = Traverse.Create(Player.m_localPlayer.GetInventory()).Field("m_inventory").GetValue<List<ItemDrop.ItemData>>();
                for(int i = 0; i < items.Count; i++)
                {
                    if (items[i].m_gridPos.y >= rows)
                        continue;
                    items[i].m_gridPos.y--;
                    if (items[i].m_gridPos.y < 0)
                        items[i].m_gridPos.y = rows - 1;
                }
                Traverse.Create(Player.m_localPlayer.GetInventory()).Method("Changed").GetValue();
            }
        }

        [HarmonyPatch(typeof(Terminal), "InputText")]
        public static class InputText_Patch
        {
            public static bool Prefix(Terminal __instance)
            {
                if (!modEnabled.Value)
                    return true;
                string text = __instance.m_input.text;
                if (text.ToLower().Equals("hotbarswitch reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "hotbar switch config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}