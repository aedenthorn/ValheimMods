using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace HelmetHairToggle
{
    [BepInPlugin("aedenthorn.HelmetHairToggle", "Helmet Hair Toggle", "0.3.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<string> hairToggleKey;
        public static ConfigEntry<string> beardToggleKey;
        public static ConfigEntry<string> hairToggleString;
        public static ConfigEntry<string> beardToggleString;
        
        public static ConfigEntry<bool> showHair;
        public static ConfigEntry<bool> showBeard;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        private static BepInExPlugin context;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            hairToggleString = Config.Bind<string>("General", "HairToggleString", "Show hair with helmet: {0}", "Text to show on toggle. {0} is replaced with true/false");
            beardToggleString = Config.Bind<string>("General", "BeardToggleString", "Show beard with helmet: {0}", "Text to show on toggle. {0} is replaced with true/false");
            hairToggleKey = Config.Bind<string>("General", "HairToggleKey", "h", "Key to toggle hair. Leave blank to disable the toggle key. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            beardToggleKey = Config.Bind<string>("General", "BeardToggleKey", "b", "Key to toggle beard. Leave blank to disable the toggle key. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            showHair = Config.Bind<bool>("General", "ShowHair", false, "Hair is currently shown or not");
            showBeard = Config.Bind<bool>("General", "ShowBeard", true, "Beard is currently shown or not");
            
            
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 470, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private void Update()
        {
            if (!modEnabled.Value || AedenthornUtils.IgnoreKeyPresses(true))
                return;
            if (AedenthornUtils.CheckKeyDown(hairToggleKey.Value))
            {
                showHair.Value = !showHair.Value;
                Config.Save();
                if(hairToggleString.Value.Length > 0)
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, string.Format(hairToggleString.Value, showHair.Value), 0, null);

                VisEquipment ve = (VisEquipment)typeof(Humanoid).GetField("m_visEquipment", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Player.m_localPlayer);
                Traverse.Create(ve).Field("m_helmetHideHair").SetValue(!showHair.Value);
                GameObject helmet = Traverse.Create(ve).Field("m_helmetItemInstance").GetValue<GameObject>();
                if (helmet != null)
                {
                    Traverse.Create(ve).Method("UpdateEquipmentVisuals").GetValue();
                }
            }
            else if (AedenthornUtils.CheckKeyDown(beardToggleKey.Value))
            {
                showBeard.Value = !showBeard.Value;
                Config.Save();
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

        [HarmonyPatch(typeof(VisEquipment), "HelmetHidesHair")]
        static class HelmetHidesHair_Patch
        {
            static bool Prefix(ref bool __result)
            {
                if (!modEnabled.Value || !showHair.Value)
                    return true;
                __result = false;
                return false;
            }
        }
        [HarmonyPatch(typeof(VisEquipment), "UpdateEquipmentVisuals")]
        static class UpdateEquipmentVisuals_Patch
        {
            static void Postfix(VisEquipment __instance, GameObject ___m_helmetItemInstance)
            {
                if (!modEnabled.Value || showBeard.Value || ___m_helmetItemInstance == null)
                    return;

                Traverse.Create(__instance).Method("SetBeardEquiped", new object[] { 0 }).GetValue();
                Traverse.Create(__instance).Method("UpdateLodgroup").GetValue();
                
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

                    __instance.AddString(text);
                    __instance.AddString($"{context.Info.Metadata.Name} config reloaded");
                    return false;
                }
                return true;
            }
        }
    }
}
