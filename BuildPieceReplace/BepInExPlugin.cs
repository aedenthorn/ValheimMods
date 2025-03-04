using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BuildPieceReplace
{
    [BepInPlugin("aedenthorn.BuildPieceReplace", "Build Piece Replace", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;
        public Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;

            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 1239, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(Player), "IsOverlapingOtherPiece")]
        public static class IsOverlapingOtherPiece_Patch
        {
            public static bool Prefix(List<Piece> ___m_tempPieces, Vector3 p, string pieceName, ref bool __result)
            {
                if (!modEnabled.Value)
                    return true;
                __result = false;
                foreach (Piece piece in ___m_tempPieces)
                {
                    if (Vector3.Distance(p, piece.transform.position) < 0.05f && p != piece.transform.position && piece.gameObject.name.StartsWith(pieceName))
                    {
                        __result = true;
                    }
                }
                return false;
            }
        }
        
        [HarmonyPatch(typeof(Player), "TestGhostClipping")]
        public static class TestGhostClipping_Patch
        {
            public static void Postfix(ref bool __result, GameObject ghost, float maxPenetration, int ___m_placeRayMask)
            {
                if (!modEnabled.Value || !__result)
                    return;

                __result = false;

                Collider[] componentsInChildren = ghost.GetComponentsInChildren<Collider>();
                Collider[] array = Physics.OverlapSphere(ghost.transform.position, 10f, ___m_placeRayMask);
                foreach (Collider collider in componentsInChildren)
                {
                    foreach (Collider collider2 in array)
                    {
                        if (collider2.gameObject.GetComponent<Piece>() && (ghost.transform.position != collider2.gameObject.transform.position || ghost.gameObject.GetComponent<Piece>().m_name == collider2.gameObject.GetComponent<Piece>().m_name) && Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation, collider2, collider2.transform.position, collider2.transform.rotation, out Vector3 vector, out float num) && num > maxPenetration)
                        {
                            __result = true;
                        }
                    }
                }
            }
        }
                
        [HarmonyPatch(typeof(Player), "PlacePiece")]
        public static class PlacePiece_Patch
        {
            public static void Postfix(Player __instance, bool __result, Piece piece, int ___m_placeRayMask, GameObject ___m_placementGhost)
            {
                if (!modEnabled.Value || !__result)
                    return;

                Vector3 position = ___m_placementGhost.transform.position;

                Collider[] componentsInChildren = piece.GetComponentsInChildren<Collider>();
                Collider[] array = Physics.OverlapSphere(position, 10f, ___m_placeRayMask);
                foreach (Collider collider in componentsInChildren)
                {
                    foreach (Collider collider2 in array)
                    {
                        if (collider2.GetComponent<Piece>() && position == collider2.gameObject.transform.position && piece.m_name != collider2.gameObject.GetComponent<Piece>().m_name)
                        {
                            WearNTear component2 = collider2.GetComponent<WearNTear>();
                            if (component2)
                            {
                                Dbgl("removeing wnt piece");

                                component2.Remove();
                            }
                            else
                            {
                                ZNetView component = collider2.GetComponent<ZNetView>();
                                if (component == null)
                                {
                                    continue;
                                }
                                ZLog.Log("Removing non WNT object with hammer " + collider2.name);
                                component.ClaimOwnership();
                                collider2.GetComponent<Piece>().DropResources();
                                collider2.GetComponent<Piece>().m_placeEffect.Create(collider2.transform.position, collider2.transform.rotation, collider2.gameObject.transform, 1f);
                                __instance.m_removeEffects.Create(collider2.transform.position, Quaternion.identity, null, 1f);
                                ZNetScene.instance.Destroy(collider2.gameObject);
                            }

                        }
                    }
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
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
