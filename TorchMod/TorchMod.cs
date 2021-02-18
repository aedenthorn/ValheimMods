using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace TorchMod
{
    [BepInPlugin("aedenthorn.TorchMod", "Torch Light Mod", "0.2.0")]

    public class TorchMod: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
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

        public static ConfigEntry<Color> firePitColor;
        public static ConfigEntry<float> firePitRange;
        public static ConfigEntry<float> firePitIntensity;
        public static ConfigEntry<float> firePitBounceIntensity;
        public static ConfigEntry<float> firePitShadowStrength;
        public static ConfigEntry<bool> firePitUseColorTemperature;
        public static ConfigEntry<float> firePitColorTemperature;
        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(TorchMod).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            modEnabled = Config.Bind("General", "Enabled", true, "Enable this mod");
            torchColor = Config.Bind("Torches", "TorchColor", new Color(1f, 0.621f, 0.482f, 1f), "The color of the light.");
            torchRange = Config.Bind("Torches", "TorchRange", 10f, "The range of the light. (float)");
            torchIntensity = Config.Bind("Torches", "TorchIntensity", 0f, "The Intensity of a light is multiplied with the Light color. (float 0-8)");
            torchBounceIntensity = Config.Bind("Torches", "TorchBounceIntensity", 1f, "The multiplier that defines the strength of the bounce lighting. (float 0+)");
            torchShadowStrength = Config.Bind("Torches", "TorchShadowStrength", 1f, "Strength of light's shadows. (float)");
            torchUseColorTemperature = Config.Bind("Torches", "TorchUseColorTemperature", false, "Set to true to use the color temperature.");
            torchColorTemperature = Config.Bind("Torches", "TorchColorTemperature", 6570f, "The color temperature of the light. (float)");

            standingTorchColor = Config.Bind("Standing Torches", "StandingTorchColor", new Color(1f, 0.621f, 0.482f, 1f), "The color of the light.");
            standingTorchRange = Config.Bind("Standing Torches", "StandingTorchRange", 0.183822f, "The range of the light. (float)");
            standingTorchIntensity = Config.Bind("Standing Torches", "StandingTorchIntensity", 0f, "The Intensity of a light is multiplied with the Light color. (float 0-8)");
            standingTorchBounceIntensity = Config.Bind("Standing Torches", "StandingTorchBounceIntensity", 1f, "The multiplier that defines the strength of the bounce lighting. (float 0+)");
            standingTorchShadowStrength = Config.Bind("Standing Torches", "StandingTorchShadowStrength", 1f, "Strength of light's shadows. (float)");
            standingTorchUseColorTemperature = Config.Bind("Standing Torches", "StandingTorchUseColorTemperature", false, "Set to true to use the color temperature.");
            standingTorchColorTemperature = Config.Bind("Standing Torches", "StandingTorchColorTemperature", 6570f, "The color temperature of the light. (float)");

            sconceColor = Config.Bind("Sconces", "SconceColor", new Color(1f, 0.621f, 0.482f, 1f), "The color of the light.");
            sconceRange = Config.Bind("Sconces", "SconceRange", 0.183822f, "The range of the light. (float)");
            sconceIntensity = Config.Bind("Sconces", "SconceIntensity", 0f, "The Intensity of a light is multiplied with the Light color. (float 0-8)");
            sconceBounceIntensity = Config.Bind("Sconces", "SconceBounceIntensity", 1f, "The multiplier that defines the strength of the bounce lighting. (float 0+)");
            sconceShadowStrength = Config.Bind("Sconces", "SconceShadowStrength", 1f, "Strength of light's shadows. (float)");
            sconceUseColorTemperature = Config.Bind("Sconces", "SconceUseColorTemperature", false, "Set to true to use the color temperature.");
            sconceColorTemperature = Config.Bind("Sconces", "SconceColorTemperature", 6570f, "The color temperature of the light. (float)");

            firePitColor = Config.Bind("Fire Pits", "FirePitColor", new Color(1f, 0.504f, 0.324f, 1f), "The color of the light.");
            firePitRange = Config.Bind("Fire Pits", "FirePitRange", 0.352742f, "The range of the light. (float)");
            firePitIntensity = Config.Bind("Fire Pits", "FirePitIntensity", 0f, "The Intensity of a light is multiplied with the Light color. (float 0-8)");
            firePitBounceIntensity = Config.Bind("Fire Pits", "FirePitBounceIntensity", 1f, "The multiplier that defines the strength of the bounce lighting. (float 0+)");
            firePitShadowStrength = Config.Bind("Fire Pits", "FirePitShadowStrength", 0f, "Strength of light's shadows. (float)");
            firePitUseColorTemperature = Config.Bind("Fire Pits", "FirePitUseColorTemperature", false, "Set to true to use the color temperature.");
            firePitColorTemperature = Config.Bind("Fire Pits", "FirePitColorTemperature", 6570f, "The color temperature of the light. (float)");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private static void SetTorchLight(Light light)
        {
            //Dbgl($"Old light color: {light.color}");
            //Dbgl($"Old light Range: {light.range}");
            //Dbgl($"Old light intensity: {light.intensity}");
            //Dbgl($"Old light bounce: {light.bounceIntensity}");
            //Dbgl($"Old light shadow strength: {light.shadowStrength}");
            //Dbgl($"Old light temperature: {light.colorTemperature}");
            //Dbgl($"Old light temperature: {light.useColorTemperature}");


            light.color = torchColor.Value;
            light.range = torchRange.Value;
            light.intensity = torchIntensity.Value;
            light.bounceIntensity = torchBounceIntensity.Value;
            light.shadowStrength = torchShadowStrength.Value;
            light.colorTemperature = torchColorTemperature.Value;

        }
        private static void SetStandingTorchLight(Light light)
        {
            //Dbgl($"Setting Standing Torch Light");
            //Dbgl($"Old light color: {light.color}");
            //Dbgl($"Old light Range: {light.range}");
            //Dbgl($"Old light intensity: {light.intensity}");
            //Dbgl($"Old light bounce: {light.bounceIntensity}");
            //Dbgl($"Old light shadow strength: {light.shadowStrength}");
            //Dbgl($"Old light temperature: {light.colorTemperature}");
            //Dbgl($"Old light temperature: {light.useColorTemperature}");


            light.color = standingTorchColor.Value;
            light.range = standingTorchRange.Value;
            light.intensity = standingTorchIntensity.Value;
            light.bounceIntensity = standingTorchBounceIntensity.Value;
            light.shadowStrength = standingTorchShadowStrength.Value;
            light.colorTemperature = standingTorchColorTemperature.Value;

        }

        private static void SetSconceLight(Light light)
        {
            //Dbgl($"Old light color: {light.color}");
            //Dbgl($"Old light Range: {light.range}");
            //Dbgl($"Old light intensity: {light.intensity}");
            //Dbgl($"Old light bounce: {light.bounceIntensity}");
            //Dbgl($"Old light shadow strength: {light.shadowStrength}");
            //Dbgl($"Old light temperature: {light.colorTemperature}");
            //Dbgl($"Old light temperature: {light.useColorTemperature}");


            light.color = sconceColor.Value;
            light.range = sconceRange.Value;
            light.intensity = sconceIntensity.Value;
            light.bounceIntensity = sconceBounceIntensity.Value;
            light.shadowStrength = sconceShadowStrength.Value;
            light.colorTemperature = sconceColorTemperature.Value;

        }
        
        private static void SetFirePitLight(Light light)
        {
            //Dbgl($"Setting Fire Pit Light");
            //Dbgl($"Old light color: {light.color}");
            //Dbgl($"Old light Range: {light.range}");
            //Dbgl($"Old light intensity: {light.intensity}");
            //Dbgl($"Old light bounce: {light.bounceIntensity}");
            //Dbgl($"Old light shadow strength: {light.shadowStrength}");
            //Dbgl($"Old light temperature: {light.colorTemperature}");
            //Dbgl($"Old light temperature: {light.useColorTemperature}");


            light.color = firePitColor.Value;
            light.range = firePitRange.Value;
            light.intensity = firePitIntensity.Value;
            light.bounceIntensity = firePitBounceIntensity.Value;
            light.shadowStrength = firePitShadowStrength.Value;
            light.colorTemperature = firePitColorTemperature.Value;

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
                    SetTorchLight(light);
                }
            }
        }

        [HarmonyPatch(typeof(Fireplace), "UpdateState")]
        static class UpdateState_Patch
        {

            static void Postfix(Fireplace __instance)
            {
                if (!modEnabled.Value || !__instance.IsBurning())
                    return;

                Light light = __instance.GetComponentInChildren<Light>();
                if(light != null)
                {
                    if (__instance.name.Contains("groundtorch"))
                    {
                        SetStandingTorchLight(light);
                    }
                    else if (__instance.name.Contains("walltorch"))
                    {
                        SetSconceLight(light);
                    }
                    else if (__instance.name.Contains("fire_pit"))
                    {
                        SetFirePitLight(light);
                    }
                }
            }
        }

    }
}
