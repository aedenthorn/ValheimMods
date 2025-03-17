using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace TorchMod
{
    [BepInPlugin("aedenthorn.TorchMod", "Torch Light Mod", "0.9.0")]

    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin  context;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<Color> torchColor;
        public static ConfigEntry<float> torchRange;
        public static ConfigEntry<float> torchIntensity;
        public static ConfigEntry<float> torchBounceIntensity;
        public static ConfigEntry<float> torchShadowStrength;
        public static ConfigEntry<bool> torchUseColorTemperature;
        public static ConfigEntry<bool> torchUseColor;
        public static ConfigEntry<float> torchColorTemperature;

        public static ConfigEntry<Color> standingTorchColor;
        public static ConfigEntry<float> standingTorchRange;
        public static ConfigEntry<float> standingTorchIntensity;
        public static ConfigEntry<float> standingTorchBounceIntensity;
        public static ConfigEntry<float> standingTorchShadowStrength;
        public static ConfigEntry<bool> standingTorchUseColorTemperature;
        public static ConfigEntry<bool> standingTorchUseColor;
        public static ConfigEntry<float> standingTorchColorTemperature;

        public static ConfigEntry<Color> sconceColor;
        public static ConfigEntry<float> sconceRange;
        public static ConfigEntry<float> sconceIntensity;
        public static ConfigEntry<float> sconceBounceIntensity;
        public static ConfigEntry<float> sconceShadowStrength;
        public static ConfigEntry<bool> sconceUseColorTemperature;
        public static ConfigEntry<bool> sconceUseColor;
        public static ConfigEntry<float> sconceColorTemperature;

        public static ConfigEntry<Color> helmetColor;
        public static ConfigEntry<float> helmetRange;
        public static ConfigEntry<float> helmetIntensity;
        public static ConfigEntry<float> helmetBounceIntensity;
        public static ConfigEntry<float> helmetShadowStrength;
        public static ConfigEntry<bool> helmetUseColorTemperature;
        public static ConfigEntry<float> helmetColorTemperature;

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

        public static ConfigEntry<Color> bonfireColorLow;
        public static ConfigEntry<float> bonfireRangeLow;
        public static ConfigEntry<float> bonfireIntensityLow;
        public static ConfigEntry<float> bonfireBounceIntensityLow;
        public static ConfigEntry<float> bonfireShadowStrengthLow;
        public static ConfigEntry<bool> bonfireUseColorTemperatureLow;
        public static ConfigEntry<float> bonfireColorTemperatureLow;

        public static ConfigEntry<Color> bonfireColorHigh;
        public static ConfigEntry<float> bonfireRangeHigh;
        public static ConfigEntry<float> bonfireIntensityHigh;
        public static ConfigEntry<float> bonfireBounceIntensityHigh;
        public static ConfigEntry<float> bonfireShadowStrengthHigh;
        public static ConfigEntry<bool> bonfireUseColorTemperatureHigh;
        public static ConfigEntry<float> bonfireColorTemperatureHigh;

        public static ConfigEntry<Color> hearthColorLow;
        public static ConfigEntry<float> hearthRangeLow;
        public static ConfigEntry<float> hearthIntensityLow;
        public static ConfigEntry<float> hearthBounceIntensityLow;
        public static ConfigEntry<float> hearthShadowStrengthLow;
        public static ConfigEntry<bool> hearthUseColorTemperatureLow;
        public static ConfigEntry<float> hearthColorTemperatureLow;

        public static ConfigEntry<Color> hearthColorHigh;
        public static ConfigEntry<float> hearthRangeHigh;
        public static ConfigEntry<float> hearthIntensityHigh;
        public static ConfigEntry<float> hearthBounceIntensityHigh;
        public static ConfigEntry<float> hearthShadowStrengthHigh;
        public static ConfigEntry<bool> hearthUseColorTemperatureHigh;
        public static ConfigEntry<float> hearthColorTemperatureHigh;
        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin ).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;

            modEnabled = Config.Bind("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 11, "Nexus mod ID for updates");

            torchUseColor = Config.Bind("Torches", "TorchUseColor", false, "Set all torches to use custom color.");
            torchColor = Config.Bind("Torches", "TorchColor", new Color(1f, 0.621f, 0.482f, 1f), "The color of the light.");
            torchRange = Config.Bind("Torches", "TorchRange", 10f, "The range of the light. (float)");
            torchIntensity = Config.Bind("Torches", "TorchIntensity", 1f, "The Intensity of a light is multiplied with the Light color. (float 0-8)");
            torchBounceIntensity = Config.Bind("Torches", "TorchBounceIntensity", 1f, "The multiplier that defines the strength of the bounce lighting. (float 0+)");
            torchShadowStrength = Config.Bind("Torches", "TorchShadowStrength", 1f, "Strength of light's shadows. (float)");
            torchUseColorTemperature = Config.Bind("Torches", "TorchUseColorTemperature", false, "Set to true to use the color temperature.");
            torchColorTemperature = Config.Bind("Torches", "TorchColorTemperature", 6570f, "The color temperature of the light. (float)");

            standingTorchUseColor = Config.Bind("Standing Torches", "standingTorchUseColor", false, "Set all standing torches to use custom color.");
            standingTorchColor = Config.Bind("Standing Torches", "StandingTorchColor", new Color(1f, 0.621f, 0.482f, 1f), "The color of the light.");
            standingTorchRange = Config.Bind("Standing Torches", "StandingTorchRange", 10f, "The range of the light. (float)");
            standingTorchIntensity = Config.Bind("Standing Torches", "StandingTorchIntensity", 1f, "The Intensity of a light is multiplied with the Light color. (float 0-8)");
            standingTorchBounceIntensity = Config.Bind("Standing Torches", "StandingTorchBounceIntensity", 1f, "The multiplier that defines the strength of the bounce lighting. (float 0+)");
            standingTorchShadowStrength = Config.Bind("Standing Torches", "StandingTorchShadowStrength", 1f, "Strength of light's shadows. (float)");
            standingTorchUseColorTemperature = Config.Bind("Standing Torches", "StandingTorchUseColorTemperature", false, "Set to true to use the color temperature.");
            standingTorchColorTemperature = Config.Bind("Standing Torches", "StandingTorchColorTemperature", 6570f, "The color temperature of the light. (float)");

            sconceUseColor = Config.Bind("Sconces", "SconceUseColor", false, "Set all sconces to use custom color.");
            sconceColor = Config.Bind("Sconces", "SconceColor", new Color(1f, 0.621f, 0.482f, 1f), "The color of the light.");
            sconceRange = Config.Bind("Sconces", "SconceRange", 12f, "The range of the light. (float)");
            sconceIntensity = Config.Bind("Sconces", "SconceIntensity", 1f, "The Intensity of a light is multiplied with the Light color. (float 0-8)");
            sconceBounceIntensity = Config.Bind("Sconces", "SconceBounceIntensity", 1f, "The multiplier that defines the strength of the bounce lighting. (float 0+)");
            sconceShadowStrength = Config.Bind("Sconces", "SconceShadowStrength", 1f, "Strength of light's shadows. (float)");
            sconceUseColorTemperature = Config.Bind("Sconces", "SconceUseColorTemperature", false, "Set to true to use the color temperature.");
            sconceColorTemperature = Config.Bind("Sconces", "SconceColorTemperature", 6570f, "The color temperature of the light. (float)");

            helmetColor = Config.Bind("Helmets", "HelmetColor", new Color(0.629f, 0.973f, 0.934f, 1f), "The color of the light.");
            helmetRange = Config.Bind("Helmets", "HelmetRange", 10f, "The range of the light. (float)");
            helmetIntensity = Config.Bind("Helmets", "HelmetIntensity", 2f, "The Intensity of a light is multiplied with the Light color. (float 0-8)");
            helmetBounceIntensity = Config.Bind("Helmets", "HelmetBounceIntensity", 1f, "The multiplier that defines the strength of the bounce lighting. (float 0+)");
            helmetShadowStrength = Config.Bind("Helmets", "HelmetshadowStrength", 1f, "Strength of light's shadows. (float)");
            helmetUseColorTemperature = Config.Bind("Helmets", "HelmetUseColorTemperature", false, "Set to true to use the color temperature.");
            helmetColorTemperature = Config.Bind("Helmets", "HelmetColorTemperature", 6570f, "The color temperature of the light. (float)");

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

            bonfireColorLow = Config.Bind("Bonfires", "BonfireColorLow", new Color(0.838f, 0.504f, 0.324f, 1f), "The color of the light.");
            bonfireRangeLow = Config.Bind("Bonfires", "BonfireRangeLow", 20f, "The range of the light. (float)");
            bonfireIntensityLow = Config.Bind("Bonfires", "BonfireIntensityLow", 1.75f, "The Intensity of a light is multiplied with the Light color. (float 0-8)");
            bonfireBounceIntensityLow = Config.Bind("Bonfires", "BonfireBounceIntensityLow", 1f, "The multiplier that defines the strength of the bounce lighting. (float 0+)");
            bonfireShadowStrengthLow = Config.Bind("Bonfires", "BonfireShadowStrengthLow", 1f, "Strength of light's shadows. (float)");
            bonfireUseColorTemperatureLow = Config.Bind("Bonfires", "BonfireUseColorTemperatureLow", false, "Set to true to use the color temperature.");
            bonfireColorTemperatureLow = Config.Bind("Bonfires", "BonfireColorTemperatureLow", 6570f, "The color temperature of the light. (float)");

            bonfireColorHigh = Config.Bind("Bonfires", "BonfireColorHigh", new Color(1f, 0.504f, 0.324f, 1f), "The color of the light.");
            bonfireRangeHigh = Config.Bind("Bonfires", "BonfireRangeHigh", 20f, "The range of the light. (float)");
            bonfireIntensityHigh = Config.Bind("Bonfires", "BonfireIntensityHigh", 1.894302f, "The Intensity of a light is multiplied with the Light color. (float 0-8)");
            bonfireBounceIntensityHigh = Config.Bind("Bonfires", "BonfireBounceIntensityHigh", 1f, "The multiplier that defines the strength of the bounce lighting. (float 0+)");
            bonfireShadowStrengthHigh = Config.Bind("Bonfires", "BonfireShadowStrengthHigh", 1f, "Strength of light's shadows. (float)");
            bonfireUseColorTemperatureHigh = Config.Bind("Bonfires", "BonfireUseColorTemperatureHigh", false, "Set to true to use the color temperature.");
            bonfireColorTemperatureHigh = Config.Bind("Bonfires", "BonfireColorTemperatureHigh", 6570f, "The color temperature of the light. (float)");

            hearthColorLow = Config.Bind("Hearths", "HearthColorLow", new Color(0.838f, 0.527f, 0.413f, 1f), "The color of the light.");
            hearthRangeLow = Config.Bind("Hearths", "HearthRangeLow", 3f, "The range of the light. (float)");
            hearthIntensityLow = Config.Bind("Hearths", "HearthIntensityLow", 1.5f, "The Intensity of a light is multiplied with the Light color. (float 0-8)");
            hearthBounceIntensityLow = Config.Bind("Hearths", "HearthBounceIntensityLow", 1f, "The multiplier that defines the strength of the bounce lighting. (float 0+)");
            hearthShadowStrengthLow = Config.Bind("Hearths", "HearthShadowStrengthLow", 1f, "Strength of light's shadows. (float)");
            hearthUseColorTemperatureLow = Config.Bind("Hearths", "HearthUseColorTemperatureLow", false, "Set to true to use the color temperature.");
            hearthColorTemperatureLow = Config.Bind("Hearths", "HearthColorTemperatureLow", 6570f, "The color temperature of the light. (float)");

            hearthColorHigh = Config.Bind("Hearths", "HearthColorHigh", new Color(1f, 0.621f, 0.482f, 1f), "The color of the light.");
            hearthRangeHigh = Config.Bind("Hearths", "HearthRangeHigh", 13f, "The range of the light. (float)");
            hearthIntensityHigh = Config.Bind("Hearths", "HearthIntensityHigh", 2.145109f, "The Intensity of a light is multiplied with the Light color. (float 0-8)");
            hearthBounceIntensityHigh = Config.Bind("Hearths", "HearthBounceIntensityHigh", 1f, "The multiplier that defines the strength of the bounce lighting. (float 0+)");
            hearthShadowStrengthHigh = Config.Bind("Hearths", "HearthShadowStrengthHigh", 1f, "Strength of light's shadows. (float)");
            hearthUseColorTemperatureHigh = Config.Bind("Hearths", "HearthUseColorTemperatureHigh", false, "Set to true to use the color temperature.");
            hearthColorTemperatureHigh = Config.Bind("Hearths", "HearthColorTemperatureHigh", 6570f, "The color temperature of the light. (float)");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(VisEquipment), "AttachItem")]
        public static class AttachItem_Patch
        {

            public static void Postfix(VisEquipment __instance, GameObject __result, int itemHash, int ___m_currentRightItemHash, int ___m_currentLeftItemHash)
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
                    if(torchUseColor.Value)
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


        [HarmonyPatch(typeof(VisEquipment), "SetHelmetEquipped")]
        public static class VisEquipment_SetHelmetEquipped_Patch
        {
            public static void Postfix(bool __result, string ___m_helmetItem, GameObject ___m_helmetItemInstance)
            {
                if (!__result || ___m_helmetItem == null)
                    return;
                Dbgl($"checking {___m_helmetItem}");

                Light light = ___m_helmetItemInstance?.GetComponentInChildren<Light>();
                if (light != null)
                {

                    Dbgl($"color: {light.color}");
                    Dbgl($"range: {light.range}");
                    Dbgl($"bounceIntensity: {light.bounceIntensity}");
                    Dbgl($"useColorTemperature: {light.useColorTemperature}");
                    Dbgl($"colorTemperature: {light.colorTemperature}");
                    Dbgl($"shadowStrength: {light.shadowStrength}");
                    Dbgl($"intensity: {light.intensity}");

                    Dbgl("Setting light for helmet");
                    light.color = helmetColor.Value;
                    light.range = helmetRange.Value;
                    light.intensity = helmetIntensity.Value;
                    light.bounceIntensity = helmetBounceIntensity.Value;
                    light.shadowStrength = helmetShadowStrength.Value;
                    light.useColorTemperature = helmetUseColorTemperature.Value;
                    light.colorTemperature = helmetColorTemperature.Value;
                }
            }
        }

        [HarmonyPatch(typeof(Fireplace), "Start")]
        public static class Fireplace_Patch
        {

            public static void Postfix(Fireplace __instance)
            {
                if (!modEnabled.Value)
                    return;

                string name = __instance.name;

                Dbgl($"starting fireplace {name}");

                FieldInfo lfi = typeof(LightFlicker).GetField("m_baseIntensity", BindingFlags.NonPublic | BindingFlags.Instance);
                if (name.Contains("groundtorch"))
                {
                    LightFlicker lf = __instance.m_enabledObject.GetComponentInChildren<LightFlicker>();
                    Light light = lf.GetComponent<Light>();

                    //Dbgl($"color: {light.color}");
                    //Dbgl($"range: {light.range}");
                    //Dbgl($"bounceIntensity: {light.bounceIntensity}");
                    //Dbgl($"useColorTemperature: {light.useColorTemperature}");
                    //Dbgl($"colorTemperature: {light.colorTemperature}");
                    //Dbgl($"shadowStrength: {light.shadowStrength}");
                    //Dbgl($"intensity: {lfi.GetValue(lf)}");

                    if (standingTorchUseColor.Value)
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


                    if (sconceUseColor.Value)
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



                    light.color = firePitColorLow.Value;
                    light.range = firePitRangeLow.Value;
                    light.bounceIntensity = firePitBounceIntensityLow.Value;
                    light.useColorTemperature = firePitUseColorTemperatureLow.Value;
                    light.colorTemperature = firePitColorTemperatureLow.Value;
                    light.shadowStrength = firePitShadowStrengthLow.Value;
                    lfi.SetValue(lf, firePitIntensityLow.Value);

                    LightFlicker lf2 = __instance.m_enabledObjectHigh.GetComponentInChildren<LightFlicker>();
                    Light light2 = lf2.GetComponent<Light>();

                    light2.color = firePitColorHigh.Value;
                    light2.range = firePitRangeHigh.Value;
                    light2.bounceIntensity = firePitBounceIntensityHigh.Value;
                    light2.useColorTemperature = firePitUseColorTemperatureHigh.Value;
                    light2.colorTemperature = firePitColorTemperatureHigh.Value;
                    light2.shadowStrength = firePitShadowStrengthHigh.Value;
                    lfi.SetValue(lf2, firePitIntensityHigh.Value);
                }
                else if (name.Contains("bonfire"))
                {
                    LightFlicker lf = __instance.m_enabledObjectLow.GetComponentInChildren<LightFlicker>();
                    Light light = lf.GetComponent<Light>();

                    light.color = bonfireColorLow.Value;
                    light.range = bonfireRangeLow.Value;
                    light.bounceIntensity = bonfireBounceIntensityLow.Value;
                    light.useColorTemperature = bonfireUseColorTemperatureLow.Value;
                    light.colorTemperature = bonfireColorTemperatureLow.Value;
                    light.shadowStrength = bonfireShadowStrengthLow.Value;
                    lfi.SetValue(lf, bonfireIntensityLow.Value);

                    LightFlicker lf2 = __instance.m_enabledObjectHigh.GetComponentInChildren<LightFlicker>();
                    Light light2 = lf2.GetComponent<Light>();

                    light2.color = bonfireColorHigh.Value;
                    light2.range = bonfireRangeHigh.Value;
                    light2.bounceIntensity = bonfireBounceIntensityHigh.Value;
                    light2.useColorTemperature = bonfireUseColorTemperatureHigh.Value;
                    light2.colorTemperature = bonfireColorTemperatureHigh.Value;
                    light2.shadowStrength = bonfireShadowStrengthHigh.Value;
                    lfi.SetValue(lf2, bonfireIntensityHigh.Value);
                }
                else if (name.Contains("hearth"))
                {
                    LightFlicker lf = __instance.m_enabledObjectLow.GetComponentInChildren<LightFlicker>();
                    Light light = lf.GetComponent<Light>();

                    light.color = hearthColorLow.Value;
                    light.range = hearthRangeLow.Value;
                    light.bounceIntensity = hearthBounceIntensityLow.Value;
                    light.useColorTemperature = hearthUseColorTemperatureLow.Value;
                    light.colorTemperature = hearthColorTemperatureLow.Value;
                    light.shadowStrength = hearthShadowStrengthLow.Value;
                    lfi.SetValue(lf, hearthIntensityLow.Value);

                    LightFlicker lf2 = __instance.m_enabledObjectHigh.GetComponentInChildren<LightFlicker>();
                    Light light2 = lf2.GetComponent<Light>();

                    light2.color = hearthColorHigh.Value;
                    light2.range = hearthRangeHigh.Value;
                    light2.bounceIntensity = hearthBounceIntensityHigh.Value;
                    light2.useColorTemperature = hearthUseColorTemperatureHigh.Value;
                    light2.colorTemperature = hearthColorTemperatureHigh.Value;
                    light2.shadowStrength = hearthShadowStrengthHigh.Value;
                    lfi.SetValue(lf2, hearthIntensityHigh.Value);
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
