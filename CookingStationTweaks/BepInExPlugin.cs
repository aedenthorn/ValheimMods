using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CookingStationTweaks
{
    [BepInPlugin("aedenthorn.CookingStationTweaks", "CookingStationTweaks", "0.7.1")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;
        public Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<bool> isDebug;
        
        public static ConfigEntry<float> slotMultiplier;
        public static ConfigEntry<float> cookTimeMultiplier;
        public static ConfigEntry<bool> preventBurning;
        public static ConfigEntry<bool> autoPop;


        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 895, "Nexus mod ID for updates");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Show debug messages in console");

            preventBurning = Config.Bind<bool>("Options", "PreventBurning", true, "Prevent burning.");
            autoPop = Config.Bind<bool>("Options", "AutoPop", true, "Automatically pop cooked items off the station.");
            slotMultiplier = Config.Bind<float>("Options", "SlotMultiplier", 2.5f, "Multiply number of cooking slots by this amount");
            cookTimeMultiplier = Config.Bind<float>("Options", "CookTimeMultiplier", 1f, "Multiply cooking time by this amount");

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

        public void UUpdate()
        {
        }


        [HarmonyPatch(typeof(CookingStation), "Awake")]
        public static class CookingStation_Awake_Patch
        {
            public static void Prefix(CookingStation __instance, ref ParticleSystem[] ___m_burntPS, ref ParticleSystem[] ___m_donePS, ref ParticleSystem[] ___m_ps, ref AudioSource[] ___m_as)
            {
                if (!modEnabled.Value)
                    return;

                if(cookTimeMultiplier.Value != 1f)
                {
                    for(int i = 0; i < __instance.m_conversion.Count; i++)
                    {
                        __instance.m_conversion[i].m_cookTime *= cookTimeMultiplier.Value;
                    }
                }

                int count = __instance.m_slots.Length;
                List<Transform> newSlots = new List<Transform>(__instance.m_slots);

                Transform burnt = null;
                Transform done = null;

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

                ___m_ps = new ParticleSystem[__instance.m_slots.Length];
                ___m_as = new AudioSource[__instance.m_slots.Length];


                List<ParticleSystem> oldBurnt = new List<ParticleSystem>(___m_burntPS);
                List<ParticleSystem> oldDone = new List<ParticleSystem>(___m_donePS);
                if (___m_burntPS.Length != 0)
                {
                    Dbgl("setting burnt");

                    burnt = ___m_burntPS[0].transform;
                    ___m_burntPS = new ParticleSystem[__instance.m_slots.Length];
                }
                if (___m_donePS.Length != 0)
                {
                    Dbgl("setting done");

                    done = ___m_donePS[0].transform;
                    ___m_donePS = new ParticleSystem[__instance.m_slots.Length];
                }

                for (int i = 0; i < __instance.m_slots.Length; i++)
                {
                    ___m_ps[i] = __instance.m_slots[i].GetComponent<ParticleSystem>();
                    ___m_as[i] = __instance.m_slots[i].GetComponent<AudioSource>();
                    if (burnt)
                    {
                        ___m_burntPS[i] = Instantiate(burnt, burnt.parent).GetComponent<ParticleSystem>();
                        ___m_burntPS[i].name = "burnt" + i;
                        ___m_burntPS[i].transform.localPosition = __instance.m_slots[i].localPosition;
                        ParticleSystem.EmissionModule emissionModule = ___m_burntPS[i].emission;
                        emissionModule.enabled = false;
                    }
                    if (done)
                    {
                        ___m_donePS[i] = Instantiate(done, done.parent).GetComponent<ParticleSystem>();
                        ___m_donePS[i].name = "done" + i;
                        ___m_donePS[i].transform.localPosition = __instance.m_slots[i].localPosition;
                        ParticleSystem.EmissionModule emissionModule = ___m_donePS[i].emission;
                        emissionModule.enabled = false;
                    }
                }
                foreach(var ps in oldBurnt)
                {
                    Destroy(ps.gameObject);
                }
                foreach(var ps in oldDone)
                {
                    Destroy(ps.gameObject);
                }
            }
        }

        [HarmonyPatch(typeof(CookingStation), "UpdateCooking")]
        public static class CookingStation_UpdateCooking_Patch
        {
            public static void Postfix(CookingStation __instance, ZNetView ___m_nview)
            {
                Traverse traverse = Traverse.Create(__instance);

                if (!modEnabled.Value || !___m_nview.IsValid() || !___m_nview.IsOwner() || (__instance.m_requireFire && !traverse.Method("IsFireLit").GetValue<bool>()) || (__instance.m_useFuel && traverse.Method("GetFuel").GetValue<float>() <= 0f))
                    return;

                //Dbgl($"Updating {__instance.name}");
                for (int i = 0; i < __instance.m_slots.Length; i++)
                {
                    string itemName = ___m_nview.GetZDO().GetString("slot" + i, "");
                    float cookedTime = ___m_nview.GetZDO().GetFloat("slot" + i, 0f);
                    int status = ___m_nview.GetZDO().GetInt("slotstatus" + i, 0);

                    if (itemName == "")
                        continue;

                    CookingStation.ItemConversion itemConversion = traverse.Method("GetItemConversion", new object[] { itemName }).GetValue<CookingStation.ItemConversion>();
                    //Dbgl($"Updating slot {i} {cookedTime}/{itemConversion.m_cookTime} {status}");

                    if (itemName != "" && status != 2)
                    {
                        //Dbgl($"Updating slot {i} {itemName} {cookedTime}");
                        if (itemConversion != null && cookedTime > itemConversion.m_cookTime && itemName == itemConversion.m_to.name)
                        {
                            if (autoPop.Value)
                            {
                                Dbgl($"Popping {__instance.name} slot {i} {itemName}");
                                Traverse.Create(__instance).Method("SpawnItem", new object[] { itemName, i, __instance.m_slots[i].position }).GetValue();
                                ___m_nview.GetZDO().Set("slot" + i, "");
                                ___m_nview.GetZDO().Set("slot" + i, 0f);
                                ___m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetSlotVisual", new object[] { i, "" });
                            }
                            else if (preventBurning.Value)
                                ___m_nview.GetZDO().Set("slot" + i, itemConversion.m_cookTime);
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

                    __instance.AddString(text);
                    __instance.AddString($"{context.Info.Metadata.Name} config reloaded");
                    return false;
                }
                return true;
            }
        }
    }
}
