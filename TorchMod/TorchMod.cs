using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TorchMod
{
    [BepInPlugin("aedenthorn.TorchMod", "Torch Light Mod", "0.4.0")]

    public class TorchMod: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static TorchMod context;
        public static ConfigEntry<bool> modEnabled;

        public static ConfigEntry<Color> torchColor;
        public static ConfigEntry<float> torchRange;
        public static ConfigEntry<float> torchIntensity;
        public static ConfigEntry<float> torchBounceIntensity;
        public static ConfigEntry<float> torchShadowStrength;
        public static ConfigEntry<bool> torchUseColorTemperature;
        public static ConfigEntry<float> torchColorTemperature;

        public static ConfigEntry<Color> standingTorchColor;
        public static ConfigEntry<float> standingTorchRange;
        public static ConfigEntry<float> standingTorchIntensity;
        public static ConfigEntry<float> standingTorchBounceIntensity;
        public static ConfigEntry<float> standingTorchShadowStrength;
        public static ConfigEntry<bool> standingTorchUseColorTemperature;
        public static ConfigEntry<float> standingTorchColorTemperature;

        public static ConfigEntry<Color> sconceColor;
        public static ConfigEntry<float> sconceRange;
        public static ConfigEntry<float> sconceIntensity;
        public static ConfigEntry<float> sconceBounceIntensity;
        public static ConfigEntry<float> sconceShadowStrength;
        public static ConfigEntry<bool> sconceUseColorTemperature;
        public static ConfigEntry<float> sconceColorTemperature;

        public static ConfigEntry<Color> firePitColorLow;
        public static ConfigEntry<float> firePitRangeLow;
        public static ConfigEntry<float> firePitIntensityLow;
        public static ConfigEntry<float> firePitBounceIntensityLow;
        public static ConfigEntry<float> firePitShadowStrengthLow;
        public static ConfigEntry<bool> firePitUseColorTemperatureLow;
        public static ConfigEntry<float> firePitColorTemperatureLow;

        public static ConfigEntry<Color> firePitColorHigh;
        public static ConfigEntry<float> firePitRangeHigh;
        public static ConfigEntry<float> firePitIntensityHigh;
        public static ConfigEntry<float> firePitBounceIntensityHigh;
        public static ConfigEntry<float> firePitShadowStrengthHigh;
        public static ConfigEntry<bool> firePitUseColorTemperatureHigh;
        public static ConfigEntry<float> firePitColorTemperatureHigh;
        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(TorchMod).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;

            modEnabled = Config.Bind("General", "Enabled", true, "Enable this mod");
            torchColor = Config.Bind("Torches", "TorchColor", new Color(1f, 0.621f, 0.482f, 1f), "The color of the light.");
            torchRange = Config.Bind("Torches", "TorchRange", 10f, "The range of the light. (float)");
            torchIntensity = Config.Bind("Torches", "TorchIntensity", 1f, "The Intensity of a light is multiplied with the Light color. (float 0-8)");
            torchBounceIntensity = Config.Bind("Torches", "TorchBounceIntensity", 1f, "The multiplier that defines the strength of the bounce lighting. (float 0+)");
            torchShadowStrength = Config.Bind("Torches", "TorchShadowStrength", 1f, "Strength of light's shadows. (float)");
            torchUseColorTemperature = Config.Bind("Torches", "TorchUseColorTemperature", false, "Set to true to use the color temperature.");
            torchColorTemperature = Config.Bind("Torches", "TorchColorTemperature", 6570f, "The color temperature of the light. (float)");

            standingTorchColor = Config.Bind("Standing Torches", "StandingTorchColor", new Color(1f, 0.621f, 0.482f, 1f), "The color of the light.");
            standingTorchRange = Config.Bind("Standing Torches", "StandingTorchRange", 0.183822f, "The range of the light. (float)");
            standingTorchIntensity = Config.Bind("Standing Torches", "StandingTorchIntensity", 1f, "The Intensity of a light is multiplied with the Light color. (float 0-8)");
            standingTorchBounceIntensity = Config.Bind("Standing Torches", "StandingTorchBounceIntensity", 1f, "The multiplier that defines the strength of the bounce lighting. (float 0+)");
            standingTorchShadowStrength = Config.Bind("Standing Torches", "StandingTorchShadowStrength", 1f, "Strength of light's shadows. (float)");
            standingTorchUseColorTemperature = Config.Bind("Standing Torches", "StandingTorchUseColorTemperature", false, "Set to true to use the color temperature.");
            standingTorchColorTemperature = Config.Bind("Standing Torches", "StandingTorchColorTemperature", 6570f, "The color temperature of the light. (float)");

            sconceColor = Config.Bind("Sconces", "SconceColor", new Color(1f, 0.621f, 0.482f, 1f), "The color of the light.");
            sconceRange = Config.Bind("Sconces", "SconceRange", 12f, "The range of the light. (float)");
            sconceIntensity = Config.Bind("Sconces", "SconceIntensity", 1f, "The Intensity of a light is multiplied with the Light color. (float 0-8)");
            sconceBounceIntensity = Config.Bind("Sconces", "SconceBounceIntensity", 1f, "The multiplier that defines the strength of the bounce lighting. (float 0+)");
            sconceShadowStrength = Config.Bind("Sconces", "SconceShadowStrength", 1f, "Strength of light's shadows. (float)");
            sconceUseColorTemperature = Config.Bind("Sconces", "SconceUseColorTemperature", false, "Set to true to use the color temperature.");
            sconceColorTemperature = Config.Bind("Sconces", "SconceColorTemperature", 6570f, "The color temperature of the light. (float)");

            firePitColorLow = Config.Bind("Fire Pits", "FirePitColorLow", new Color(0.838f, 0.527f, 0.413f, 1f), "The color of the light.");
            firePitRangeLow = Config.Bind("Fire Pits", "FirePitRangeLow", 2.5f, "The range of the light. (float)");
            firePitIntensityLow = Config.Bind("Fire Pits", "FirePitIntensityLow", 1f, "The Intensity of a light is multiplied with the Light color. (float 0-8)");
            firePitBounceIntensityLow = Config.Bind("Fire Pits", "FirePitBounceIntensityLow", 1f, "The multiplier that defines the strength of the bounce lighting. (float 0+)");
            firePitShadowStrengthLow = Config.Bind("Fire Pits", "FirePitShadowStrengthLow", 1f, "Strength of light's shadows. (float)");
            firePitUseColorTemperatureLow = Config.Bind("Fire Pits", "FirePitUseColorTemperatureLow", false, "Set to true to use the color temperature.");
            firePitColorTemperatureLow = Config.Bind("Fire Pits", "FirePitColorTemperatureLow", 6570f, "The color temperature of the light. (float)");

            firePitColorHigh = Config.Bind("Fire Pits", "FirePitColorHigh", new Color(1f, 0.504f, 0.324f, 1f), "The color of the light.");
            firePitRangeHigh = Config.Bind("Fire Pits", "FirePitRangeHigh", 10f, "The range of the light. (float)");
            firePitIntensityHigh = Config.Bind("Fire Pits", "FirePitIntensityHigh", 1f, "The Intensity of a light is multiplied with the Light color. (float 0-8)");
            firePitBounceIntensityHigh = Config.Bind("Fire Pits", "FirePitBounceIntensityHigh", 1f, "The multiplier that defines the strength of the bounce lighting. (float 0+)");
            firePitShadowStrengthHigh = Config.Bind("Fire Pits", "FirePitShadowStrengthHigh", 1f, "Strength of light's shadows. (float)");
            firePitUseColorTemperatureHigh = Config.Bind("Fire Pits", "FirePitUseColorTemperatureHigh", false, "Set to true to use the color temperature.");
            firePitColorTemperatureHigh = Config.Bind("Fire Pits", "FirePitColorTemperatureHigh", 6570f, "The color temperature of the light. (float)");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(VisEquipment), "AttachItem")]
        static class AttachItem_Patch
        {

            static void Postfix(VisEquipment __instance, GameObject __result, int itemHash, int ___m_currentRightItemHash, int ___m_currentLeftItemHash)
            {
                if (!modEnabled.Value || __result == null || !__instance.m_isPlayer)
                    return;

                GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemHash);
                if (itemPrefab == null)
                {
                    return;
                }
                ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
                ItemDrop.ItemData.ItemType itemType = (component.m_itemData.m_shared.m_attachOverride != ItemDrop.ItemData.ItemType.None) ? component.m_itemData.m_shared.m_attachOverride : component.m_itemData.m_shared.m_itemType;
                if (itemType != ItemDrop.ItemData.ItemType.Torch)
                    return;

                Dbgl($"attached torch {__result.name}");
                Light light = __result.GetComponentInChildren<Light>();
                if(light != null)
                {
                    Dbgl("Setting light for torch");
                    light.color = torchColor.Value;
                    light.range = torchRange.Value;
                    light.intensity = torchIntensity.Value;
                    light.bounceIntensity = torchBounceIntensity.Value;
                    light.shadowStrength = torchShadowStrength.Value;
                    light.useColorTemperature = torchUseColorTemperature.Value;
                    light.colorTemperature = torchColorTemperature.Value;
                }
            }
        }
        
        [HarmonyPatch(typeof(Fireplace), "Start")]
        static class Fireplace_Patch
        {

            static void Postfix(Fireplace __instance)
            {
                if (!modEnabled.Value)
                    return;

                string name = __instance.name;

                Dbgl(name);

                FieldInfo lfi = typeof(LightFlicker).GetField("m_baseIntensity", BindingFlags.NonPublic | BindingFlags.Instance);
                if (name.Contains("groundtorch"))
                {
                    LightFlicker lf = __instance.m_enabledObject.GetComponentInChildren<LightFlicker>();
                    Light light = lf.GetComponent<Light>();
                    
                    Dbgl($"color: {light.color}");
                    Dbgl($"range: {light.range}");
                    Dbgl($"bounceIntensity: {light.bounceIntensity}");
                    Dbgl($"useColorTemperature: {light.useColorTemperature}");
                    Dbgl($"colorTemperature: {light.colorTemperature}");
                    Dbgl($"shadowStrength: {light.shadowStrength}");
                    Dbgl($"intensity: {lfi.GetValue(lf)}");

                    light.color = standingTorchColor.Value;
                    light.range = standingTorchRange.Value;
                    light.bounceIntensity = standingTorchBounceIntensity.Value;
                    light.useColorTemperature = standingTorchUseColorTemperature.Value;
                    light.colorTemperature = standingTorchColorTemperature.Value;
                    light.shadowStrength = standingTorchShadowStrength.Value;
                    lfi.SetValue(lf, standingTorchIntensity.Value);
                }
                else if (name.Contains("walltorch"))
                {

                    LightFlicker lf = __instance.m_enabledObject.GetComponentInChildren<LightFlicker>();
                    Light light = lf.GetComponent<Light>();

                    Dbgl($"color: {light.color}");
                    Dbgl($"range: {light.range}");
                    Dbgl($"bounceIntensity: {light.bounceIntensity}");
                    Dbgl($"useColorTemperature: {light.useColorTemperature}");
                    Dbgl($"colorTemperature: {light.colorTemperature}");
                    Dbgl($"shadowStrength: {light.shadowStrength}");
                    Dbgl($"intensity: {lfi.GetValue(lf)}");

                    light.color = sconceColor.Value;
                    light.range = sconceRange.Value;
                    light.bounceIntensity = sconceBounceIntensity.Value;
                    light.useColorTemperature = sconceUseColorTemperature.Value;
                    light.colorTemperature = sconceColorTemperature.Value;
                    light.shadowStrength = sconceShadowStrength.Value;
                    lfi.SetValue(lf, sconceIntensity.Value);
                }
                else if (name.Contains("fire_pit"))
                {
                    LightFlicker lf = __instance.m_enabledObjectLow.GetComponentInChildren<LightFlicker>();
                    Light light = lf.GetComponent<Light>();

                    Dbgl($"color: {light.color}");
                    Dbgl($"range: {light.range}");
                    Dbgl($"bounceIntensity: {light.bounceIntensity}");
                    Dbgl($"useColorTemperature: {light.useColorTemperature}");
                    Dbgl($"colorTemperature: {light.colorTemperature}");
                    Dbgl($"shadowStrength: {light.shadowStrength}");
                    Dbgl($"intensity: {lfi.GetValue(lf)}");

                    light.color = firePitColorLow.Value;
                    light.range = firePitRangeLow.Value;
                    light.bounceIntensity = firePitBounceIntensityLow.Value;
                    light.useColorTemperature = firePitUseColorTemperatureLow.Value;
                    light.colorTemperature = firePitColorTemperatureLow.Value;
                    light.shadowStrength = firePitShadowStrengthLow.Value;
                    lfi.SetValue(lf, firePitIntensityLow.Value);

                    LightFlicker lf2 = __instance.m_enabledObjectHigh.GetComponentInChildren<LightFlicker>();
                    Light light2 = lf2.GetComponent<Light>();

                    Dbgl($"color: {light2.color}");
                    Dbgl($"range: {light2.range}");
                    Dbgl($"bounceIntensity: {light2.bounceIntensity}");
                    Dbgl($"useColorTemperature: {light2.useColorTemperature}");
                    Dbgl($"colorTemperature: {light2.colorTemperature}");
                    Dbgl($"shadowStrength: {light2.shadowStrength}");
                    Dbgl($"intensity: {lfi.GetValue(lf2)}");


                    light2.color = firePitColorHigh.Value;
                    light2.range = firePitRangeHigh.Value;
                    light2.bounceIntensity = firePitBounceIntensityHigh.Value;
                    light2.useColorTemperature = firePitUseColorTemperatureHigh.Value;
                    light2.colorTemperature = firePitColorTemperatureHigh.Value;
                    light2.shadowStrength = firePitShadowStrengthHigh.Value;
                    lfi.SetValue(lf2, firePitIntensityHigh.Value);
                }

            }
        }

    }
}
