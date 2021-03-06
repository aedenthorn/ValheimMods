﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CookingStationTweaks
{
    [BepInPlugin("aedenthorn.CookingStationTweaks", "CookingStationTweaks", "0.2.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;
        private Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<bool> isDebug;
        
        public static ConfigEntry<float> slotMultiplier;
        public static ConfigEntry<bool> preventBurning;
        public static ConfigEntry<bool> autoPop;


        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 895, "Nexus mod ID for updates");


            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Show debug messages in console");
            preventBurning = Config.Bind<bool>("General", "PreventBurning", true, "Prevent burning.");
            autoPop = Config.Bind<bool>("General", "AutoPop", true, "Automatically pop cooked items off the station.");
            slotMultiplier = Config.Bind<float>("General", "SlotMultiplier", 2.5f, "Multiply number of cooking slots by this amount");

            if (!modEnabled.Value)
                return;


            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        private void OnDestroy()
        {
            Dbgl("Destroying plugin");
            harmony?.UnpatchAll();
        }

        private void UUpdate()
        {
        }


        [HarmonyPatch(typeof(CookingStation), "Awake")]
        static class CookingStation_Awake_Patch
        {
            static void Prefix(CookingStation __instance)
            {
                if (!modEnabled.Value)
                    return;

                int count = __instance.m_slots.Length;
                List<Transform> newSlots = new List<Transform>(__instance.m_slots);
                float target = Mathf.RoundToInt(count * slotMultiplier.Value);
                Dbgl($"Cooking station {__instance.name} awake. Slots {count}, target {target}");
                while(count < target)
                {
                    bool neg = true;
                    List<Transform> currentSlots = new List<Transform>(newSlots);
                    int off = 0;
                    for (int i = 0; i <= currentSlots.Count; i++)
                    {
                        int idx = currentSlots.Count / 2 - 1 + (neg ? -off : off);
                        Transform a = null;
                        Transform b = null;
                        if (idx >=0)
                            a = currentSlots[idx]; // 0, 1, -1, 2
                        if (idx < currentSlots.Count - 1)
                            b = currentSlots[idx+1]; // 1, 2, 0, 3

                        Transform c;
                        if (a == null)
                        {
                            c = Instantiate(b, b.parent);
                            c.position = b.position * 2 - Vector3.Lerp(b.position, currentSlots[idx + 2].position, 0.5f);
                            newSlots.Insert(0, c);
                        }
                        else if(b == null)
                        {
                            c = Instantiate(a, a.parent);
                            c.position = a.position * 2 - Vector3.Lerp(a.position, currentSlots[idx - 1].position, 0.5f);
                            newSlots.Add(c);
                        }
                        else
                        {
                            c = Instantiate(a, a.parent);
                            c.position = Vector3.Lerp(a.position, b.position, 0.5f);
                            newSlots.Insert(idx+1, c);
                        }
                        count++;
                        if (count >= target)
                            break;
                        neg = !neg;
                        if (i % 2 == 0)
                            off++;
                    }
                }
                Dbgl($"New number of slots {newSlots.Count}");
                for (int i = 0; i < newSlots.Count; i++)
                    newSlots[i].name = "slot" + i;
                __instance.m_slots = newSlots.ToArray();
            }
        }

        [HarmonyPatch(typeof(CookingStation), "UpdateCooking")]
        static class CookingStation_UpdateCooking_Patch
        {
            static void Postfix(CookingStation __instance, ZNetView ___m_nview)
            {
                if (!modEnabled.Value || !___m_nview.IsValid() || !___m_nview.IsOwner() || !EffectArea.IsPointInsideArea(__instance.transform.position, EffectArea.Type.Burning, 0.25f))
                    return;
                for (int i = 0; i < __instance.m_slots.Length; i++)
                {
                    string itemName = ___m_nview.GetZDO().GetString("slot" + i, "");

                    float num = ___m_nview.GetZDO().GetFloat("slot" + i, 0f);
                    if (itemName != "" && itemName != __instance.m_overCookedItem.name && itemName != null)
                    {
                        CookingStation.ItemConversion itemConversion = Traverse.Create(__instance).Method("GetItemConversion", new object[] { itemName }).GetValue<CookingStation.ItemConversion>();
                        if (num > itemConversion.m_cookTime && itemName == itemConversion.m_to.name)
                        {
                            if (autoPop.Value)
                            {
                                Traverse.Create(__instance).Method("SpawnItem", new object[] { itemName }).GetValue();
                                ___m_nview.GetZDO().Set("slot" + i, "");
                                ___m_nview.GetZDO().Set("slot" + i, 0f);
                                ___m_nview.InvokeRPC(ZNetView.Everybody, "SetSlotVisual", new object[] { i, "" });
                            }
                            else if (preventBurning.Value)
                                ___m_nview.GetZDO().Set("slot" + i, itemConversion.m_cookTime);
                        }
                    }
                }
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
