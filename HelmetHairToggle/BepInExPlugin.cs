using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace HelmetHairToggle
{
    [BepInPlugin("aedenthorn.HelmetHairToggle", "Helmet Hair Toggle", "0.5.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static readonly bool isDebug = true;

        public static ConfigEntry<string> hairToggleKey;
        public static ConfigEntry<string> beardToggleKey;
        public static ConfigEntry<string> hairToggleString;
        public static ConfigEntry<string> beardToggleString;
        
        public static ConfigEntry<ItemDrop.ItemData.HelmetHairType> showHair;
        public static ConfigEntry<ItemDrop.ItemData.HelmetHairType> showBeard;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static BepInExPlugin context;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            hairToggleString = Config.Bind<string>("General", "HairToggleString", "Show hair with helmet: {0}", "Text to show on toggle. {0} is replaced with true/false");
            beardToggleString = Config.Bind<string>("General", "BeardToggleString", "Show beard with helmet: {0}", "Text to show on toggle. {0} is replaced with true/false");
            hairToggleKey = Config.Bind<string>("General", "HairToggleKey", "h", "Key to toggle hair. Leave blank to disable the toggle key. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            beardToggleKey = Config.Bind<string>("General", "BeardToggleKey", "b", "Key to toggle beard. Leave blank to disable the toggle key. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            showHair = Config.Bind<ItemDrop.ItemData.HelmetHairType>("General", "ShowHair", ItemDrop.ItemData.HelmetHairType.Hidden, "Hair is currently shown or not");
            showBeard = Config.Bind<ItemDrop.ItemData.HelmetHairType>("General", "ShowBeard", ItemDrop.ItemData.HelmetHairType.Default, "Beard is currently shown or not");
            
            
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 470, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        public void Update()
        {
            if (!modEnabled.Value || AedenthornUtils.IgnoreKeyPresses(true))
                return;
            if (AedenthornUtils.CheckKeyDown(hairToggleKey.Value))
            {
                showHair.Value = showHair.Value == ItemDrop.ItemData.HelmetHairType.Hidden ? ItemDrop.ItemData.HelmetHairType.Default : ItemDrop.ItemData.HelmetHairType.Hidden;
                if(hairToggleString.Value.Length > 0)
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, string.Format(hairToggleString.Value, showHair.Value), 0, null);

                VisEquipment ve = (VisEquipment)typeof(Humanoid).GetField("m_visEquipment", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Player.m_localPlayer);
                AccessTools.Field(typeof(VisEquipment), "m_helmetHideHair").SetValue(ve, showHair.Value);
                GameObject helmet = (GameObject)AccessTools.Field(typeof(VisEquipment), "m_helmetItemInstance").GetValue(ve);       
                if (helmet != null)
                {
                    AccessTools.Method(typeof(VisEquipment), "UpdateEquipmentVisuals").Invoke(ve, new object[] { });
                }
            }
            else if (AedenthornUtils.CheckKeyDown(beardToggleKey.Value))
            {
                showBeard.Value = showBeard.Value == ItemDrop.ItemData.HelmetHairType.Hidden ? ItemDrop.ItemData.HelmetHairType.Default : ItemDrop.ItemData.HelmetHairType.Hidden;
                
                if(beardToggleString.Value.Length > 0)
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, string.Format(beardToggleString.Value, showBeard.Value), 0, null);

                VisEquipment ve = (VisEquipment)typeof(Humanoid).GetField("m_visEquipment", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Player.m_localPlayer);
                GameObject helmet = Traverse.Create(ve).Field("m_helmetItemInstance").GetValue<GameObject>();
                if (helmet != null)
                {
                    Traverse.Create(ve).Method("UpdateEquipmentVisuals").GetValue();
                }
            }
        }

        [HarmonyPatch(typeof(VisEquipment), "HelmetHides")]
        public static class HelmetHides_Patch
        {
            public static bool Prefix(ref bool hideHair, ref bool hideBeard)
            {
                if (!modEnabled.Value)
                    return true;
                hideHair = showHair.Value == ItemDrop.ItemData.HelmetHairType.Hidden;
                hideBeard = showBeard.Value == ItemDrop.ItemData.HelmetHairType.Hidden;
                return false;
            }
        }
        [HarmonyPatch(typeof(VisEquipment), "UpdateEquipmentVisuals")]
        public static class UpdateEquipmentVisuals_Patch
        {
            public static void Postfix(VisEquipment __instance, GameObject ___m_helmetItemInstance)
            {
                if (!modEnabled.Value || showBeard.Value != ItemDrop.ItemData.HelmetHairType.Hidden || ___m_helmetItemInstance == null)
                    return;

                Traverse.Create(__instance).Method("SetBeardEquipped", new object[] { 0 }).GetValue();
                Traverse.Create(__instance).Method("UpdateLodgroup").GetValue();
                
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
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();

                    __instance.AddString(text);
                    __instance.AddString($"{context.Info.Metadata.Name} config reloaded");
                    return false;
                }
                return true;
            }
        }
    }
}
