using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ControllerButtonSwitch
{
    [BepInPlugin("aedenthorn.ControllerButtonSwitch", "Controller Button Switch", "0.4.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;
        public Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<string> gamePadButton;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<string> buttonAttack;
        public static ConfigEntry<string> buttonSecondAttack;
        public static ConfigEntry<string> buttonBlock;
        public static ConfigEntry<string> buttonUse;
        public static ConfigEntry<string> buttonHide;
        public static ConfigEntry<string> buttonJump;
        public static ConfigEntry<string> buttonCrouch;
        public static ConfigEntry<string> buttonRun;
        public static ConfigEntry<string> buttonToggleWalk;
        public static ConfigEntry<string> buttonAutoRun;
        public static ConfigEntry<string> buttonSit;
        public static ConfigEntry<string> buttonGPower;
        public static ConfigEntry<string> buttonCamZoomIn;
        public static ConfigEntry<string> buttonCamZoomOut;
        public static ConfigEntry<string> buttonAltPlace;
        public static ConfigEntry<string> buttonForward;
        public static ConfigEntry<string> buttonLeft;
        public static ConfigEntry<string> buttonBackward;
        public static ConfigEntry<string> buttonRight;
        public static ConfigEntry<string> buttonInventory;
        public static ConfigEntry<string> buttonMap;
        public static ConfigEntry<string> buttonMapZoomOut;
        public static ConfigEntry<string> buttonMapZoomIn;
        public static ConfigEntry<string> buttonTabLeft;
        public static ConfigEntry<string> buttonTabRight;
        public static ConfigEntry<string> buttonBuildMenu;
        public static ConfigEntry<string> buttonRemove;

        public static ConfigEntry<string> buttonAutoPickup;
        public static ConfigEntry<string> buttonScrollChatUp;
        public static ConfigEntry<string> buttonScrollChatDown;
        public static ConfigEntry<string> buttonChatUp;
        public static ConfigEntry<string> buttonChatDown;

        public static ConfigEntry<string> buttonJoyUse;
        public static ConfigEntry<string> buttonJoyHide;
        public static ConfigEntry<string> buttonJoyJump;
        public static ConfigEntry<string> buttonJoySit;

        public static ConfigEntry<string> buttonJoyInventory;
        public static ConfigEntry<string> buttonJoyRun;
        public static ConfigEntry<string> buttonJoyCrouch;
        public static ConfigEntry<string> buttonJoyMap;
        public static ConfigEntry<string> buttonJoyChat;
        public static ConfigEntry<string> buttonJoyMenu;
        public static ConfigEntry<string> buttonJoyAltPlace;
        public static ConfigEntry<string> buttonJoyRemove;
        public static ConfigEntry<string> buttonJoyTabLeft;
        public static ConfigEntry<string> buttonJoyTabRight;
        public static ConfigEntry<string> buttonJoyButtonA;
        public static ConfigEntry<string> buttonJoyButtonB;
        public static ConfigEntry<string> buttonJoyButtonX;
        public static ConfigEntry<string> buttonJoyButtonY;
        public static ConfigEntry<string> buttonJoyLStick;
        public static ConfigEntry<string> buttonJoyRStick;

        public static ConfigEntry<string> buttonJoyGPower;
        public static ConfigEntry<string> buttonJoyHotbarUse;
        public static ConfigEntry<string> buttonJoyHotbarLeft;
        public static ConfigEntry<string> buttonJoyHotbarRight;
        public static ConfigEntry<string> buttonJoyAutoPickup;
        public static ConfigEntry<string> buttonJoyCamZoomIn;
        public static ConfigEntry<string> buttonJoyCamZoomOut;


        public static ConfigEntry<string> buttonJoyBlock;
        public static ConfigEntry<string> buttonJoyAttack;
        public static ConfigEntry<string> buttonJoySecondaryAttack;
        public static ConfigEntry<string> buttonJoyAltKeys;
        
        public static ConfigEntry<string> buttonJoyPlace;
        public static ConfigEntry<string> buttonJoyRotate;
        public static ConfigEntry<string> buttonJoyBack;
        
        public static ConfigEntry<string> buttonJoyScrollChatUp;
        public static ConfigEntry<string> buttonJoyScrollChatDown;

        public static ConfigEntry<string> buttonJoyLStickLeft;
        public static ConfigEntry<string> buttonJoyLStickRight;
        public static ConfigEntry<string> buttonJoyLStickUp;
        public static ConfigEntry<string> buttonJoyLStickDown;
        public static ConfigEntry<string> buttonJoyDPadLeft;
        public static ConfigEntry<string> buttonJoyDPadRight;
        public static ConfigEntry<string> buttonJoyDPadUp;
        public static ConfigEntry<string> buttonJoyDPadDown;
        public static ConfigEntry<string> buttonJoyLTrigger;
        public static ConfigEntry<string> buttonJoyRTrigger;
        
        public static ConfigEntry<float> repeatDelaySetting;
        public static ConfigEntry<float> repeatIntervalSetting;
        
        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("Config", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("Config", "NexusID", 1105, "Nexus mod ID for updates");

            buttonAttack = Config.Bind<string>("KeysCombat", "Attack", "Mouse0", "Attack button");
            buttonSecondAttack = Config.Bind<string>("KeysCombat", "SecondaryAttack", "Mouse2", "SecondaryAttack button");
            buttonBlock = Config.Bind<string>("KeysCombat", "Block", "Mouse1", "Block button");
            buttonUse = Config.Bind<string>("KeysMisc", "Use", "E", "Use button");
            buttonHide = Config.Bind<string>("KeysMisc", "Hide", "R", "Hide button");
            buttonJump = Config.Bind<string>("KeysMovement", "Jump", "Space", "Jump button");
            buttonCrouch = Config.Bind<string>("KeysMovement", "Crouch", "LeftControl", "Crouch button");
            buttonRun = Config.Bind<string>("KeysMovement", "Run", "LeftShift", "Run button");
            buttonToggleWalk = Config.Bind<string>("KeysMovement", "ToggleWalk", "C", "ToggleWalk button");
            buttonAutoRun = Config.Bind<string>("KeysMovement", "AutoRun", "Q", "AutoRun button");
            buttonSit = Config.Bind<string>("KeysMovement", "Sit", "X", "Sit button");
            buttonGPower = Config.Bind<string>("KeysMisc", "GP", "F", "GP button");
            buttonAltPlace = Config.Bind<string>("KeysBuild", "AltPlace", "LeftShift", "AltPlace button");
            buttonCamZoomIn = Config.Bind<string>("KeysMisc", "CamZoomIn", "None", "CamZoomIn button");
            buttonCamZoomOut = Config.Bind<string>("KeysMisc", "CamZoomOut", "None", "CamZoomOut button");
            buttonForward = Config.Bind<string>("KeysDirection", "Forward", "W,0.3,0.1", "Forward button");
            buttonLeft = Config.Bind<string>("KeysDirection", "Left", "A,0.3,0.1", "Left button");
            buttonBackward = Config.Bind<string>("KeysDirection", "Backward", "S,0.3,0.1", "Backward button");
            buttonRight = Config.Bind<string>("KeysDirection", "Right", "D,0.3,0.1", "Right button");
            buttonInventory = Config.Bind<string>("KeysMisc", "Inventory", "Tab", "Inventory button");
            buttonMap = Config.Bind<string>("KeysMap", "Map", "M", "Map button");
            buttonMapZoomOut = Config.Bind<string>("KeysMap", "MapZoomOut", "Comma", "MapZoomOut button");
            buttonMapZoomIn = Config.Bind<string>("KeysMap", "MapZoomIn", "Period", "MapZoomIn button");
            buttonTabLeft = Config.Bind<string>("KeysBuild", "TabLeft", "Q", "TabLeft button");
            buttonTabRight = Config.Bind<string>("KeysBuild", "TabRight", "E", "TabRight button");
            buttonBuildMenu = Config.Bind<string>("KeysBuild", "BuildMenu", "Mouse1", "BuildMenu button");
            buttonRemove = Config.Bind<string>("KeysBuild", "Remove", "Mouse2", "Remove button");

            buttonAutoPickup = Config.Bind<string>("KeysMisc", "AutoPickup", "V", "Remove button");

            buttonScrollChatUp = Config.Bind<string>("KeysChat", "ScrollChatUp", "PageUp,0.5,0.5,true", "ScrollChatUp button");
            buttonScrollChatDown = Config.Bind<string>("KeysChat", "ScrollChatDown,0.5,0.5,true", "PageDown", "ScrollChatDown button");
            buttonChatUp = Config.Bind<string>("KeysChat", "ChatUp", "UpArrow,0.5,0.5,true", "ChatUp button");
            buttonChatDown = Config.Bind<string>("KeysChat", "ChatDown", "DownArrow,0.5,0.5,true", "ChatDown button");

            buttonJoyUse = Config.Bind<string>("JoystickMisc", "JoyUse", "JoystickButton0", "JoyUse button");
            buttonJoyHide = Config.Bind<string>("JoystickMisc", "JoyHide", "JoystickButton9", "JoyHide button");
            buttonJoyJump = Config.Bind<string>("JoystickMovement", "JoyJump", "JoystickButton1", "JoyJump button");
            buttonJoySit = Config.Bind<string>("JoystickMovement", "JoySit", "JoystickButton2", "JoySit button");

            buttonJoyGPower = Config.Bind<string>("JoystickMisc", "JoyGP", "JoyAxis 7,0,0,true", "JoyGP button");
            buttonJoyInventory = Config.Bind<string>("JoystickMisc", "JoyInventory", "JoystickButton3", "JoyInventory button");
            buttonJoyRun = Config.Bind<string>("JoystickMovement", "JoyRun", "JoystickButton4", "JoyRun button");
            buttonJoyCrouch = Config.Bind<string>("JoystickMovement", "JoyCrouch", "JoystickButton8", "JoyCrouch button");
            buttonJoyMap = Config.Bind<string>("JoystickUI", "JoyMap", "JoystickButton6", "JoyMap button");
            buttonJoyChat = Config.Bind<string>("JoystickUI", "JoyChat", "JoystickButton6", "JoyChat button");
            buttonJoyMenu = Config.Bind<string>("JoystickUI", "JoyMenu", "JoystickButton7", "JoyMenu button");
            buttonJoyAltPlace = Config.Bind<string>("JoystickBuild", "JoyAltPlace", "JoystickButton4", "JoyAltPlace button");
            buttonJoyRemove = Config.Bind<string>("JoystickBuild", "JoyRemove", "JoystickButton5", "JoyRemove button");
            buttonJoyTabLeft = Config.Bind<string>("JoystickUI", "JoyTabLeft", "JoystickButton4", "JoyTabLeft button");
            buttonJoyTabRight = Config.Bind<string>("JoystickUI", "JoyTabRight", "JoystickButton5", "JoyTabRight button");
            buttonJoyButtonA = Config.Bind<string>("JoystickButtons", "JoyButtonA", "JoystickButton0", "JoyButtonA button");
            buttonJoyButtonB = Config.Bind<string>("JoystickButtons", "JoyButtonB", "JoystickButton1", "JoyButtonB button");
            buttonJoyButtonX = Config.Bind<string>("JoystickButtons", "JoyButtonX", "JoystickButton2", "JoyButtonX button");
            buttonJoyButtonY = Config.Bind<string>("JoystickButtons", "JoyButtonY", "JoystickButton3", "JoyButtonY button");
            buttonJoyLStick = Config.Bind<string>("JoystickButtons", "JoyLStick", "JoystickButton8", "JoyLStick button");
            buttonJoyRStick = Config.Bind<string>("JoystickButtons", "JoyRStick", "JoystickButton9", "JoyRStick button");

            buttonJoyHotbarUse = Config.Bind<string>("JoystickMisc", "JoyHotbarUse", "JoyAxis 7,0,0,false", "JoyHotbarUse button");
            buttonJoyHotbarLeft = Config.Bind<string>("JoystickMisc", "JoyHotbarLeft", "JoyAxis 6,0.3,0.1,true", "JoyHotbarLeft button");
            buttonJoyHotbarRight = Config.Bind<string>("JoystickMisc", "JoyHotbarRight", "JoyAxis 6,0.3,0.1,false", "JoyHotbarRight button");
            buttonJoyCamZoomIn = Config.Bind<string>("JoystickMisc", "JoyCamZoomIn", "JoyAxis 7,0,0,false", "JoyCamZoomIn button");
            buttonJoyCamZoomOut = Config.Bind<string>("JoystickMisc", "JoyCamZoomOut", "JoyAxis 7,0,0,true", "JoyCamZoomOut button");
            buttonJoyAutoPickup = Config.Bind<string>("JoystickMisc", "JoyAutoPickup", "JoystickButton8", "JoyAutoPickup button");

            buttonJoyBlock = Config.Bind<string>("JoystickCombat", "JoyBlock", "JoyAxis 3,0,0,true", "JoyBlock button");
            buttonJoyAttack = Config.Bind<string>("JoystickCombat", "JoyAttack", "JoyAxis 3,0,0,false", "JoyAttack button");
            buttonJoySecondaryAttack = Config.Bind<string>("JoystickCombat", "JoySecondaryAttack", "JoystickButton5", "JoySecondaryAttack button");
            buttonJoyAltKeys = Config.Bind<string>("JoystickMisc", "JoyAltKeys", "JoyAxis 3,0,0,true", "JoyAltKeys button");
            
            buttonJoyRotate = Config.Bind<string>("JoystickBuild", "JoyRotate", "JoyAxis 3,0,0,true", "JoyRotate button");
            buttonJoyPlace = Config.Bind<string>("JoystickBuild", "JoyPlace", "JoyAxis 10,0,0,false", "JoyPlace button");

            buttonJoyLStickLeft = Config.Bind<string>("JoystickPads", "JoyLStickLeft", "JoyAxis 1,0.3,0.1,true", "JoyLStickLeft button");
            buttonJoyLStickRight = Config.Bind<string>("JoystickPads", "JoyLStickRight", "JoyAxis 1,0.3,0.1,false", "JoyLStickRight button");
            buttonJoyLStickUp = Config.Bind<string>("JoystickPads", "JoyLStickUp", "JoyAxis 2,0.3,0.1,true", "JoyLStickUp button");
            buttonJoyLStickDown = Config.Bind<string>("JoystickPads", "JoyLStickDown", "JoyAxis 2,0.3,0.1,false", "JoyLStickDown button");

            buttonJoyScrollChatUp = Config.Bind<string>("JoystickMisc", "JoyScrollChatUp", "JoyAxis 2,0.5,0.5,true", "JoyScrollChatUp button");
            buttonJoyScrollChatDown = Config.Bind<string>("JoystickMisc", "JoyScrollChatDown", "JoyAxis 2,0.5,0.5,false", "JoyScrollChatDown button");
            
            buttonJoyBack = Config.Bind<string>("JoystickMisc", "JoyBack", "JoystickButton6", "JoyBack button");

            buttonJoyDPadLeft = Config.Bind<string>("JoystickPads", "JoyDPadLeft", "JoyAxis 6,0.3,0.1,true", "JoyDPadLeft button");
            buttonJoyDPadRight = Config.Bind<string>("JoystickPads", "JoyDPadRight", "JoyAxis 6,0.3,0.1,false", "JoyDPadRight button");
            buttonJoyDPadUp = Config.Bind<string>("JoystickPads", "JoyDPadUp", "JoyAxis 7,0.3,0.1,false", "JoyDPadUp button");
            buttonJoyDPadDown = Config.Bind<string>("JoystickPads", "JoyDPadDown", "JoyAxis 7,0.3,0.1,true", "JoyDPadDown button");
            
            buttonJoyLTrigger = Config.Bind<string>("JoystickButtons", "JoyLTrigger", "JoyAxis 3,0,0,true", "JoyLTrigger button");
            buttonJoyRTrigger = Config.Bind<string>("JoystickButtons", "JoyRTrigger", "JoyAxis 3,0,0,false", "JoyRTrigger button");

            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        public void OnDestroy()
        {
            Dbgl("Destroying plugin");
            harmony?.UnpatchAll();
        }

        public static void SetButtons()
        {
            if (!modEnabled.Value)
                return;
            if(ZInput.instance is null)
            {
                Dbgl("ZInput is null");
                return;
            }

            ZInput zInput = ZInput.instance;
            Dictionary<string, ZInput.ButtonDef> m_buttons = Traverse.Create(zInput).Field("m_buttons").GetValue<Dictionary<string, ZInput.ButtonDef>>();


            using (var enumerator = context.Config.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.Key.Section == "Config")
                        continue;
                    try
                    {
                        ButtonInfo info = new ButtonInfo(enumerator.Current.Key.Key, (ConfigEntry<string>)enumerator.Current.Value);
                        if (m_buttons.ContainsKey(info.button))
                        {
                            m_buttons.Remove(info.button);
                        }
                        if (Enum.TryParse(info.key, out KeyCode keyCode))
                            zInput.AddButton(info.button, keyCode, info.repeatDelay, info.repeatInterval);
                        else
                            zInput.AddButton(info.button, info.key, info.inverted, info.repeatDelay, info.repeatInterval);
                    }
                    catch(Exception ex)
                    {
                        Dbgl($"Error setting button {enumerator.Current.Key.Key}: {ex}");
                    }
                }
            }
            zInput.Save();
            Dbgl("Finished setting buttons");
        }

        [HarmonyPatch(typeof(ZInput), "Reset")]
        public static class ZInput_Reset_Patch
        {
            public static void Postfix()
            {
                if (modEnabled.Value)
                    SetButtons();
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
                    SetButtons();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                if (text.ToLower().Equals($"dump keycodes"))
                {
                    var codes = Enum.GetNames(typeof(KeyCode));
                    Dbgl($"{codes.Length} KeyCodes:\r\n\r\n" + string.Join("\r\n", codes));
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"KeyCodes dumped to Player.log" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
