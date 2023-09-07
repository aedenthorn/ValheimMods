using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace HoeRadius
{
    [BepInPlugin("aedenthorn.HoeRadius", "Hoe Radius", "0.2.1")]
    public class BepInExPlugin : BaseUnityPlugin
    {

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> useScrollWheel;
        public static ConfigEntry<string> scrollModKey;
        public static ConfigEntry<string> increaseHotKey;
        public static ConfigEntry<string> decreaseHotKey;
        
        public static ConfigEntry<float> scrollWheelScale;
        public static ConfigEntry<float> hotkeyScale;

        private static BepInExPlugin context;
        private static float lastOriginalRadius;
        private static float lastModdedRadius;
        private static float lastTotalDelta;

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
            nexusID = Config.Bind<int>("General", "NexusID", 1199, "Nexus mod ID for updates");
            
            useScrollWheel = Config.Bind<bool>("Settings", "UseScrollWheel", true, "Use scroll wheel to modify radius");
            scrollWheelScale = Config.Bind<float>("Settings", "ScrollWheelScale", 0.1f, "Scroll wheel change scale");
            scrollModKey = Config.Bind<string>("Settings", "ScrollModKey", "left alt", "Modifer key to allow scroll wheel change. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            
            increaseHotKey = Config.Bind<string>("Settings", "IncreaseHotKey", "", "Hotkey to increase radius. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            decreaseHotKey = Config.Bind<string>("Settings", "DecreaseHotKey", "", "Hotkey to decrease radius. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            hotkeyScale = Config.Bind<float>("Settings", "HotkeyScale", 0.1f, "Hotkey change scale");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

        }
        private void Update()
        {
            if (false && !modEnabled.Value || !Player.m_localPlayer || !Player.m_localPlayer.InPlaceMode() || Hud.IsPieceSelectionVisible())
            {
                if(lastOriginalRadius != 0)
                {
                    lastOriginalRadius = 0;
                    lastModdedRadius = 0;
                    lastTotalDelta = 0;
                    SetRadius(0);
                }
                return;
            }

            if(useScrollWheel.Value && AedenthornUtils.CheckKeyHeld(scrollModKey.Value, false) && Input.mouseScrollDelta.y != 0)
            {
                SetRadius(Input.mouseScrollDelta.y * scrollWheelScale.Value);
            }
            else if(AedenthornUtils.CheckKeyDown(increaseHotKey.Value))
            {
                SetRadius(hotkeyScale.Value);
            }
            else if(AedenthornUtils.CheckKeyDown(decreaseHotKey.Value))
            {
                SetRadius(-hotkeyScale.Value);
            }
        }

        private void SetRadius(float delta)
        {
            Piece selectedPiece = Traverse.Create(Player.m_localPlayer).Field("m_buildPieces")?.GetValue<PieceTable>()?.GetSelectedPiece();
            if (selectedPiece is null)
                return;
            var op = selectedPiece?.gameObject.GetComponent<TerrainOp>();
            if (op == null)
                return;

            //Dbgl($"Adjusting radius by {delta}");
            float originalRadius = 0;
            float moddedRadius = Mathf.Max(lastModdedRadius + delta, 0);
            lastTotalDelta += delta;
            if (lastOriginalRadius == 0)
            {
                if (op.m_settings.m_level && originalRadius < op.m_settings.m_levelRadius)
                {
                    originalRadius = op.m_settings.m_levelRadius;
                    moddedRadius = Mathf.Max(op.m_settings.m_levelRadius + delta, 0);
                }
                if (op.m_settings.m_raise && originalRadius < op.m_settings.m_raiseRadius)
                {
                    originalRadius = op.m_settings.m_raiseRadius;
                    moddedRadius = Mathf.Max(op.m_settings.m_raiseRadius + delta, 0);
                }
                if (op.m_settings.m_smooth && originalRadius < op.m_settings.m_smoothRadius)
                {
                    originalRadius = op.m_settings.m_smoothRadius;
                    moddedRadius = Mathf.Max(op.m_settings.m_smoothRadius + delta, 0);
                }
                if (op.m_settings.m_paintCleared && originalRadius < op.m_settings.m_paintRadius)
                {
                    originalRadius = op.m_settings.m_paintRadius;
                    moddedRadius = Mathf.Max(op.m_settings.m_paintRadius + delta, 0);
                }
                lastOriginalRadius = originalRadius;
            }
            lastModdedRadius = moddedRadius;

            if (lastOriginalRadius > 0 && lastModdedRadius > 0)
            {
                //Dbgl($"total delta {lastTotalDelta}");

                var ghost = Traverse.Create(Player.m_localPlayer).Field("m_placementGhost").GetValue<GameObject>()?.transform.Find("_GhostOnly");
                if (ghost != null)
                {
                    //Dbgl($"Adjusting ghost scale to {lastModdedRadius / lastOriginalRadius}x");
                    ghost.localScale = new Vector3(lastModdedRadius / lastOriginalRadius, lastModdedRadius / lastOriginalRadius, lastModdedRadius / lastOriginalRadius);
                }
            }
        }

        [HarmonyPatch(typeof(TerrainOp), "Awake")]
        static class TerrainOp_Patch
        {
            static void Prefix(TerrainOp __instance)
            {
                if (!modEnabled.Value)
                    return;

                if (__instance.m_settings.m_level)
                {
                    __instance.m_settings.m_levelRadius += lastTotalDelta;
                    Dbgl($"Applying level radius {__instance.m_settings.m_levelRadius}");
                }
                if (__instance.m_settings.m_raise)
                {
                    __instance.m_settings.m_raiseRadius += lastTotalDelta;
                    Dbgl($"Applying raise radius {__instance.m_settings.m_raiseRadius}");
                }
                if (__instance.m_settings.m_smooth)
                {
                    __instance.m_settings.m_smoothRadius += lastTotalDelta;
                    Dbgl($"Applying smooth radius {__instance.m_settings.m_smoothRadius}");
                }
                if (__instance.m_settings.m_paintCleared)
                {
                    __instance.m_settings.m_paintRadius += lastTotalDelta;
                    Dbgl($"Applying paint radius {__instance.m_settings.m_paintRadius}");
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
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
