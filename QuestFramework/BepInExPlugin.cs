using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace QuestFramework
{
    [BepInPlugin("aedenthorn.QuestFramework", "Quest Framework", "0.2.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> showHUD;
        public static ConfigEntry<string> toggleHUDKey;
        public static ConfigEntry<bool> toggleHUDKeyOnPress;
        public static ConfigEntry<bool> hudUseOSFont;
        public static ConfigEntry<bool> hudUseShadow;
        public static ConfigEntry<Color> hudFontColor;
        public static ConfigEntry<Color> hudShadowColor;
        public static ConfigEntry<int> hudShadowOffset;
        public static ConfigEntry<int> hudFontSize;
        public static ConfigEntry<string> toggleHUDKeyMod;
        public static ConfigEntry<string> toggleClockKey;
        public static ConfigEntry<string> hudFontName;
        public static ConfigEntry<string> hudTemplate;
        public static ConfigEntry<string> questNameTemplate;
        public static ConfigEntry<string> questDescTemplate;
        public static ConfigEntry<string> stageNameTemplate;
        public static ConfigEntry<string> stageDescTemplate;
        public static ConfigEntry<string> objectiveNameTemplate;
        public static ConfigEntry<string> objectiveDescTemplate;
        public static ConfigEntry<string> hudLocationString;
        public static ConfigEntry<TextAnchor> hudTextAlignment;

        public static QuestDataObject currentQuests = new QuestDataObject();

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 1583, "Nexus mod ID for updates");

            showHUD = Config.Bind<bool>("Options", "ShowHUD", true, "Show the quest HUD");
            toggleHUDKey = Config.Bind<string>("Options", "ToggleHUDKey", "j", "Key used to toggle the HUD display. Leave blank to disable toggling. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            toggleHUDKeyMod = Config.Bind<string>("Options", "ToggleHUDKeyMod", "", "Extra modifier key used to toggle the HUD display. Leave blank to not require one. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            hudLocationString = Config.Bind<string>("Options", "HudLocationString", "80%,50%", "Location on the screen to show the HUD (x,y) or (x%,y%)");

            hudUseOSFont = Config.Bind<bool>("Display", "HUDUseOSFont", false, "Set to true to specify the name of a font from your OS; otherwise limited to fonts in the game resources");
            hudUseShadow = Config.Bind<bool>("Display", "HUDUseShadow", false, "Add a shadow behind the HUD text");
            hudShadowOffset = Config.Bind<int>("Display", "HUDShadowOffset", 2, "Shadow offset in pixels");
            hudFontName = Config.Bind<string>("Display", "HUDFontName", "AveriaSerifLibre-Bold", "Name of the font to use");
            hudFontSize = Config.Bind<int>("Display", "HUDFontSize", 24, "Font size of HUD display");
            hudFontColor = Config.Bind<Color>("Display", "HUDFontColor", Color.white, "Font color for the HUD");
            hudShadowColor = Config.Bind<Color>("Display", "HUDShadowColor", Color.black, "Color for the shadow");
            toggleHUDKeyOnPress = Config.Bind<bool>("Display", "ToggleHUDKeyOnPress", false, "If true, limit HUD display to when the hotkey is down");
            
            hudTemplate = Config.Bind<string>("Text", "hudTemplate", "<color=#FFB65F><b>Current Quests:</b></color>\n\n{quests}", "Quest list template. {quests} is replaced by the quest list.");
            questNameTemplate = Config.Bind<string>("Text", "QuestNameTemplate", "<size=24>  </size><size=20>{name}</size>", "Quest name template. {name} is replaced by the quest name");
            questDescTemplate = Config.Bind<string>("Text", "QuestDescTemplate", "<size=24>  </size><size=20><i>{desc}</i></size>", "Quest desc template. {desc} is replaced by the quest description.");
            stageNameTemplate = Config.Bind<string>("Text", "StageNameTemplate", "<size=24>    </size><size=18>{name}</size>", "Quest stage name template. {name} is replaced by the stage name.");
            stageDescTemplate = Config.Bind<string>("Text", "StageDescTemplate", "<size=24>    </size><size=18><i>{desc}</i></size>", "Quest stage desc template. {desc} is replaced by the stage description.");
            objectiveNameTemplate = Config.Bind<string>("Text", "ObjectiveNameTemplate", "<size=24>      </size><size=16>{name}</size>", "Stage objective name template. {name} is replaced by the objective name.");
            objectiveDescTemplate = Config.Bind<string>("Text", "ObjectiveDescTemplate", "<size=24>      </size><size=16><i>{desc}</i></size>", "Stage objective desc template. {desc} is replaced by the objective description.");
            hudTextAlignment = Config.Bind<TextAnchor>("Text", "HudTextAlignment", TextAnchor.MiddleLeft, "HUD display text alignment.");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MetadataHelper.GetMetadata(this).GUID);
        }
        public void Update()
        {
            if (!enabled || !Hud.instance || toggleHUDKeyOnPress.Value || !AedenthornUtils.CheckKeyDown(toggleHUDKey.Value) || !AedenthornUtils.CheckKeyHeld(toggleHUDKeyMod.Value, false) || AedenthornUtils.IgnoreKeyPresses(true))
                return;
            showHUD.Value = !showHUD.Value;
        }
    }
}
