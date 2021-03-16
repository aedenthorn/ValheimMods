using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace DemolishRestrictions
{
    [BepInPlugin("aedenthorn.DemolishRestrictions", "Demolish Restrictions", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<bool> restrictToCreator;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 516, "Nexus mod ID for updates");
            restrictToCreator = Config.Bind<bool>("General", "RestrictToOwner", true, "Restrict destruction to creator only.");

            if (!modEnabled.Value)
                return;

            //if (debugEnabled.Value)
            //    Player.m_debugMode = true;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        private void Update()
        {
            if (!AedenthornUtils.IgnoreKeyPresses(true) && AedenthornUtils.CheckKeyDown(hotKey.Value)) 
            {
                Player player = Player.m_localPlayer;
                Dbgl($"Ka-boom");
                Collider[] array = Physics.OverlapSphere(player.transform.position, destroyRadius.Value, destroyMask);
                for (int i = 0; i < array.Length; i++)
                {
                    Piece piece = array[i].GetComponentInParent<Piece>();
                    if (piece)
                    {
                        if (!piece.IsCreator())
                        {
                            continue;
                        }
                        if (!piece.m_canBeRemoved)
                        {
                            continue;
                        }
                        if (Location.IsInsideNoBuildLocation(piece.transform.position))
                        {
                            continue;
                        }
                        if (!PrivateArea.CheckAccess(piece.transform.position, 0f, true))
                        {
                            continue;
                        }
                        if (!Traverse.Create(player).Method("CheckCanRemovePiece", new object[] { piece }).GetValue<bool>())
                        {
                            continue;
                        }
                        ZNetView component = piece.GetComponent<ZNetView>();
                        if (component == null)
                        {
                            continue;
                        }
                        if (!piece.CanBeRemoved())
                        {
                            continue;
                        }
                        WearNTear component2 = piece.GetComponent<WearNTear>();
                        if (component2)
                        {
                            component2.Remove();
                        }
                        else
                        {
                            component.ClaimOwnership();
                            piece.DropResources();
                            piece.m_placeEffect.Create(piece.transform.position, piece.transform.rotation, piece.gameObject.transform, 1f);
                            player.m_removeEffects.Create(piece.transform.position, Quaternion.identity, null, 1f);
                            ZNetScene.instance.Destroy(piece.gameObject);
                        }
                        ItemDrop.ItemData rightItem = player.GetRightItem();
                        if (rightItem != null)
                        {
                            player.FaceLookDirection();
                            Traverse.Create(player).Field("m_zanim").GetValue<ZSyncAnimation>().SetTrigger(rightItem.m_shared.m_attack.m_attackAnimation);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(WearNTear), "RPC_Remove")]
        static class RPC_Remove_Patch
        {
            static bool Prefix(WearNTear __instance, long sender)
            {
                if (!modEnabled.Value || !restrictToCreator.Value || sender == Game.instance.GetPlayerProfile().GetPlayerID())
                    return true;
                return false;
            }
        }
        
        [HarmonyPatch(typeof(ZDOMan), "RPC_DestroyZDO")]
        static class RPC_Remove_Patch
        {
            static bool Prefix(WearNTear __instance, long sender)
            {
                if (!modEnabled.Value || !restrictToCreator.Value || sender == Game.instance.GetPlayerProfile().GetPlayerID())
                    return true;
                return false;
            }
        }

        [HarmonyPatch(typeof(Console), "InputText")]
        static class InputText_Patch
        {
            static bool Prefix(Console __instance)
            {
                if (!modEnabled.Value)
                    return true;
                string text = __instance.m_input.text;
                if (text.ToLower().Equals("demolishrestrictions reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    //if (debugEnabled.Value)
                    //    Player.m_debugMode = true;
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { "Demolish Restrictions config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}