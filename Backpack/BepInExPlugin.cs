using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Backpack
{
    [BepInPlugin("aedenthorn.Backpack", "Backpack", "0.3.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;
        public Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<string> backpackName;
        public static ConfigEntry<string> backpackGUID;
        public static ConfigEntry<Vector2> backpackSize;
        public static ConfigEntry<float> backpackWeightMult;
        public static ConfigEntry<bool> dropInventoryOnDeath;
        public static ConfigEntry<bool> createTombStone;

        public static GameObject backpack = null;
        public static ZDO backpackZDO = null;
        public static string backpackObjectPrefix = "Container_Backpack";
        public static string backpackObjectName = "";
        public static bool saving = false;
        public static bool opening = false;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 858, "Nexus mod ID for updates");


            hotKey = Config.Bind<string>("General", "HotKey", "b", "Hotkey to open backpack.");
            backpackName = Config.Bind<string>("General", "BackpackName", "Backpack", "Display name for backpack.");
            backpackGUID = Config.Bind<string>("General", "BackpackGUID", Guid.NewGuid().ToString(), "Unique ID for your backpack (don't change this).");
            backpackSize = Config.Bind<Vector2>("General", "BackpackSize", new Vector2(6,3), "Size of backpack (w,h).");
            backpackWeightMult = Config.Bind<float>("General", "BackpackWeightMult", 0.5f, "Multiplier for weight of items in backpack (set to 0 to disable backpack weight).");
            dropInventoryOnDeath = Config.Bind<bool>("General", "DropInventoryOnDeath", true, "Drop backpack inventory on death");
            createTombStone = Config.Bind<bool>("General", "CreateTombStone", true, "If DropInventoryOnDeath then create tombstone rather than just dropping inventory.");

            if (!modEnabled.Value)
                return;


            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        public void OnDestroy()
        {
            Dbgl("Destroying plugin");
            harmony?.UnpatchAll();
        }

        public void Update()
        {
            if (!modEnabled.Value || !Player.m_localPlayer || !ZNetScene.instance)
                return;

            if (backpack)
            {
                if (!saving && backpack.transform.parent != Player.m_localPlayer.transform)
                {
                    Dbgl("Moving backpack to player");
                    backpack.transform.SetParent(Player.m_localPlayer.transform);
                    InitBackpack();
                }
                backpack.transform.position = Player.m_localPlayer.transform.position;
                if (!AedenthornUtils.IgnoreKeyPresses(true) && AedenthornUtils.CheckKeyDown(hotKey.Value))
                {
                    opening = true;
                    Traverse.Create(backpack.GetComponent<Container>()).Field("m_nview").GetValue<ZNetView>().ClaimOwnership();
                    Traverse.Create(backpack.GetComponent<Container>()).Field("m_nview").GetValue<ZNetView>().InvokeRPC("RequestOpen", new object[] { Player.m_localPlayer.GetPlayerID() });
                }
            }
        }


        [HarmonyPatch(typeof(FejdStartup), "LoadMainScene")]
        public static class LoadMainScene_Patch
        {
            public static void Prefix(FejdStartup __instance, List<PlayerProfile> ___m_profiles)
            {
                if (!modEnabled.Value)
                    return;

                var profile = ___m_profiles.Find(p => p.GetFilename() == (string)typeof(Game).GetField("m_profileFilename", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));
                long id = profile.GetPlayerID();
                string name = profile.GetName();

                backpackObjectName = backpackObjectPrefix + "_" + backpackGUID.Value + "_" + name;
                backpack = null;
                backpackZDO = null;
            }
        }

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        public static class ZNetScene_Awake_Patch
        {
            public static void Postfix(Dictionary<int, GameObject> ___m_namedPrefabs)
            {
                if (!modEnabled.Value)
                    return;
                
                //GameObject go = Instantiate(___m_namedPrefabs["piece_chest".GetStableHashCode()]);
                ___m_namedPrefabs.Add(backpackObjectName.GetStableHashCode(), ___m_namedPrefabs["piece_chest".GetStableHashCode()]);
            }
        }

        [HarmonyPatch(typeof(ZNetScene), "CreateObject")]
        public static class CreateObject_Patch
        {
            public static void Postfix(ZDO zdo, GameObject __result)
            {
                if (!modEnabled.Value || !__result || zdo.GetPrefab() != backpackObjectName.GetStableHashCode())
                    return;
                Dbgl($"Created backpack {__result.name}");
                __result.name = backpackObjectName;
                __result.GetComponent<Container>().m_name = backpackName.Value;
                Traverse.Create(Traverse.Create(__result.GetComponent<Container>()).Field("m_inventory").GetValue<Inventory>()).Field("m_width").SetValue((int)Math.Min(8, backpackSize.Value.x));
                Traverse.Create(Traverse.Create(__result.GetComponent<Container>()).Field("m_inventory").GetValue<Inventory>()).Field("m_height").SetValue((int)backpackSize.Value.y);
                backpack = __result;

                if (Player.m_localPlayer)
                {
                    backpack.transform.SetParent(Player.m_localPlayer.transform);
                    InitBackpack();
                }
            }
        }

        [HarmonyPatch(typeof(Player), "Start")]
        public static class Player_Start_Patch
        {
            public static void Prefix(Player __instance)
            {
                if (!modEnabled.Value || !ZNetScene.instance || Player.m_localPlayer == null || __instance.GetPlayerID() != Player.m_localPlayer.GetPlayerID())
                    return;

                saving = false;

                if (backpack)
                {
                    Dbgl("Backpack exists, moving to player.");
                    backpack.transform.SetParent(Player.m_localPlayer.transform);
                }
                else if(backpackZDO != null)
                {
                    Dbgl($"Creating new backpack from existing ZDO {backpackZDO.GetPrefab()}.");
                    Traverse.Create(ZNetScene.instance).Method("CreateObject", new object[] { backpackZDO }).GetValue();
                    return;
                }
                else
                {
                    Dbgl("Creating new backpack.");
                    GameObject prefab = ZNetScene.instance.GetPrefab("piece_chest");
                    backpack = Instantiate(prefab, Player.m_localPlayer.transform);
                }

                InitBackpack();
            }
        }
        public static void InitBackpack()
        {
            Dbgl("Initializing backpack.");

            backpack.transform.localPosition = Vector3.zero;
            backpack.name = backpackObjectName;

            if (backpack.GetComponent<WearNTear>())
                Destroy(backpack.GetComponent<WearNTear>());
            if (backpack.GetComponent<Piece>())
                Destroy(backpack.GetComponent<Piece>());
            if (backpack.transform.Find("New"))
                Destroy(backpack.transform.Find("New").gameObject);

            backpack.GetComponent<Container>().m_name = backpackName.Value;
            Traverse tc = Traverse.Create(backpack.GetComponent<Container>());
            Traverse ti = Traverse.Create(tc.Field("m_inventory").GetValue<Inventory>());
            tc.Field("m_nview").GetValue<ZNetView>().ClaimOwnership();
            ti.Field("m_name").SetValue(backpackName.Value);
            ti.Field("m_width").SetValue((int)Math.Min(8, backpackSize.Value.x));
            ti.Field("m_height").SetValue((int)backpackSize.Value.y);
            backpackZDO = backpack.GetComponent<ZNetView>().GetZDO();
            ResetBackpackSector();
        }

        [HarmonyPatch(typeof(Container), "Awake")]
        public static class Container_Awake_Patch
        {
            public static void Prefix(Container __instance)
            {
                if (!modEnabled.Value || !Player.m_localPlayer)
                    return;

                if(__instance.transform.parent == Player.m_localPlayer.transform)
                {
                    __instance.m_name = backpackName.Value;
                    __instance.name = backpackObjectName;
                    Dbgl("Backpack awake.");
                }

            }
            public static void Postfix(Container __instance, ZNetView ___m_nview)
            {
                if (!modEnabled.Value || !Player.m_localPlayer)
                    return;

                if(__instance.transform.parent == Player.m_localPlayer.transform)
                {
                    Dbgl($"Backpack name {__instance.name} hash {backpackObjectName.GetStableHashCode()} {___m_nview.GetZDO().GetPrefab()}");
                    __instance.name = backpackObjectName;
                    ___m_nview.GetZDO().SetPrefab(backpackObjectName.GetStableHashCode());
                }
            }
        }
        
        [HarmonyPatch(typeof(ZNetScene), "CreateDestroyObjects")]
        public static class CreateDestroyObjects_Patch
        {
            public static void Prefix(ZNetScene __instance)
            {
                if (!modEnabled.Value || !Player.m_localPlayer)
                    return;
                ResetBackpackSector();
            }
        }
                
        [HarmonyPatch(typeof(InventoryGui), "Update")]
        public static class InventoryGui_Update_Patch
        {
            public static void Postfix(InventoryGui __instance, Animator ___m_animator, Container ___m_currentContainer)
            {
                if (!modEnabled.Value || !backpack || !Player.m_localPlayer || !___m_animator.GetBool("visible") || !AedenthornUtils.CheckKeyDown(hotKey.Value))
                    return;

                if (opening)
                {
                    opening = false;
                    return;
                }

                if(___m_currentContainer == backpack.GetComponent<Container>())
                {
                    Traverse.Create(__instance).Method("CloseContainer").GetValue();
                }
                else
                {
                    Traverse.Create(backpack.GetComponent<Container>()).Field("m_nview").GetValue<ZNetView>().ClaimOwnership();
                    Traverse.Create(backpack.GetComponent<Container>()).Field("m_nview").GetValue<ZNetView>().InvokeRPC("RequestOpen", new object[] { Player.m_localPlayer.GetPlayerID() });
                }
            }
        }   

        [HarmonyPatch(typeof(Inventory), "GetTotalWeight")]
        [HarmonyPriority(Priority.Last)]
        public static class GetTotalWeight_Patch
        {
            public static void Postfix(Inventory __instance, ref float __result)
            {
                if (!modEnabled.Value || !backpack || !Player.m_localPlayer)
                    return;
                if(__instance == Player.m_localPlayer.GetInventory())
                {
                    if (new StackFrame(2).ToString().IndexOf("OverrideGetTotalWeight") > -1)
                    {
                        return;
                    }
                    __result += backpack.GetComponent<Container>().GetInventory().GetTotalWeight();
                }
                else if(__instance == backpack.GetComponent<Container>().GetInventory())
                {
                    __result *= backpackWeightMult.Value;
                }
            }
        }   

        [HarmonyPatch(typeof(Player), "OnDeath")]
        public static class OnDeath_Patch
        {
            public static void Prefix(Player __instance)
            {
                if (!modEnabled.Value || !backpack || !Player.m_localPlayer || __instance.GetPlayerID() != Player.m_localPlayer.GetPlayerID() || backpack.GetComponent<Container>().GetInventory().NrOfItems() == 0)
                    return;

                if (dropInventoryOnDeath.Value)
                {
                    if (createTombStone.Value)
                    {
                        GameObject gameObject = Instantiate(__instance.m_tombstone, __instance.GetCenterPoint() + Vector3.forward, __instance.transform.rotation);
                        gameObject.GetComponent<Container>().GetInventory().MoveInventoryToGrave(backpack.GetComponent<Container>().GetInventory());
                        TombStone component = gameObject.GetComponent<TombStone>();
                        PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
                        component.Setup(playerProfile.GetName(), playerProfile.GetPlayerID());
                    }
                    else
                        Traverse.Create(backpack.GetComponent<Container>()).Method("DropAllItems", new object[] { }).GetValue();
                }

                Dbgl("Moving backpack to scene on death");
                MoveBackpackToScene();
                ResetBackpackSector();
            }

        }

        public static void MoveBackpackToScene()
        {
            saving = true;
            backpack.transform.SetParent(Traverse.Create(ZNetScene.instance).Field("m_netSceneRoot").GetValue<GameObject>().transform);
            backpack.transform.localPosition = Player.m_localPlayer.transform.localPosition;
        }
        public static void ResetBackpackSector()
        {
            if (backpack?.GetComponent<ZNetView>()?.GetZDO() == null)
                return;

            Vector2i zone = ZoneSystem.instance.GetZone(ZNet.instance.GetReferencePosition());
            Traverse.Create(backpack.GetComponent<ZNetView>().GetZDO()).Method("SetSector", new object[] { zone }).GetValue();
        }

        //[HarmonyPatch(typeof(Game), "SavePlayerProfile")]
        public static class SavePlayerProfile_Patch
        {
            public static void Prefix()
            {
                if (!modEnabled.Value)
                    return;
                saving = true;

                if (backpack)
                {
                    //Dbgl("Moving backpack to scene on save");
                    //MoveBackpackToScene();
                }

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
                    if (Player.m_localPlayer && backpack)
                    {
                        backpack.transform.SetParent(Player.m_localPlayer.transform);
                        InitBackpack();
                    }
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
