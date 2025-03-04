using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace CustomGraphicsSettings
{
    [BepInPlugin("aedenthorn.CustomGraphicsSettings", "Custom Graphics Settings", "1.0.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static readonly bool isDebug = true;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<AnisotropicFiltering> anisotropicFiltering;
        public static ConfigEntry<int> antiAliasing;
        public static ConfigEntry<int> asyncUploadBufferSize;
        public static ConfigEntry<bool> asyncUploadPersistentBuffer;
        public static ConfigEntry<int> asyncUploadTimeSlice;
        public static ConfigEntry<bool> billboardsFaceCameraPosition;
        public static ConfigEntry<float> lodBias;
        public static ConfigEntry<int> masterTextureLimit;
        public static ConfigEntry<int> maximumLODLevel;
        public static ConfigEntry<int> maxQueuedFrames;
        public static ConfigEntry<int> particleRaycastBudget;
        public static ConfigEntry<int> pixelLightCount;
        public static ConfigEntry<bool> realtimeReflectionProbes;
        public static ConfigEntry<float> resolutionScalingFixedDPIFactor;
        public static ConfigEntry<float> shadowCascade2Split;
        public static ConfigEntry<Vector3> shadowCascade4Split;
        public static ConfigEntry<int> shadowCascades;
        public static ConfigEntry<float> shadowDistance;
        public static ConfigEntry<ShadowmaskMode> shadowmaskMode;
        public static ConfigEntry<float> shadowNearPlaneOffset;
        public static ConfigEntry<ShadowProjection> shadowProjection;
        public static ConfigEntry<ShadowResolution> shadowResolution;
        public static ConfigEntry<ShadowQuality> shadows;
        public static ConfigEntry<SkinWeights> skinWeights;
        public static ConfigEntry<bool> softParticles;
        public static ConfigEntry<bool> softVegetation;
        public static ConfigEntry<bool> streamingMipmapsActive;
        public static ConfigEntry<bool> streamingMipmapsAddAllCameras;
        public static ConfigEntry<int> streamingMipmapsMaxFileIORequests;
        public static ConfigEntry<int> streamingMipmapsMaxLevelReduction;
        public static ConfigEntry<float> streamingMipmapsMemoryBudget;
        public static ConfigEntry<int> vSyncCount;
        
        public static ConfigEntry<string> hotkey;
        public static ConfigEntry<bool> reloadOnChange;

        public static BepInExPlugin context;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 169, "Nexus mod ID for updates");
            hotkey = Config.Bind<string>("Options", "Hotkey", "", "Hotkey to reload settings from config");
            reloadOnChange = Config.Bind<bool>("Options", "ReloadOnChange", true, "Reload graphics settings when config values are changed");


            anisotropicFiltering = Config.Bind<AnisotropicFiltering>("QualitySettings", "anisotropicFiltering", QualitySettings.anisotropicFiltering, "Global anisotropic filtering mode.");
            antiAliasing = Config.Bind<int>("QualitySettings", "antiAliasing", QualitySettings.antiAliasing, "Set The AA Filtering option.");
            asyncUploadBufferSize = Config.Bind<int>("QualitySettings", "asyncUploadBufferSize", QualitySettings.asyncUploadBufferSize, "Asynchronous texture and mesh data upload provides timesliced async texture and mesh data upload on the render thread with tight control over memory and timeslicing. There are no allocations except for the ones which driver has to do. To read data and upload texture and mesh data, Unity re-uses a ringbuffer whose size can be controlled.Use asyncUploadBufferSize to set the buffer size for asynchronous texture and mesh data uploads. The size is in megabytes. The minimum value is 2 and the maximum value is 512. The buffer resizes automatically to fit the largest texture currently loading. To avoid re-sizing of the buffer, which can incur performance cost, set the value approximately to the size of biggest texture used in the Scene.");
            asyncUploadPersistentBuffer = Config.Bind<bool>("QualitySettings", "asyncUploadPersistentBuffer", QualitySettings.asyncUploadPersistentBuffer, "This flag controls if the async upload pipeline's ring buffer remains allocated when there are no active loading operations. Set this to true, to make the ring buffer allocation persist after all upload operations have completed. If you have issues with excessive memory usage, you can set this to false. This means you reduce the runtime memory footprint, but memory fragmentation can occur. The default value is true.");
            asyncUploadTimeSlice = Config.Bind<int>("QualitySettings", "asyncUploadTimeSlice", QualitySettings.asyncUploadTimeSlice, "Async texture upload provides timesliced async texture upload on the render thread with tight control over memory and timeslicing. There are no allocations except for the ones which driver has to do. To read data and upload texture data a ringbuffer whose size can be controlled is re-used.Use asyncUploadTimeSlice to set the time-slice in milliseconds for asynchronous texture uploads per frame. Minimum value is 1 and maximum is 33.");
            billboardsFaceCameraPosition = Config.Bind<bool>("QualitySettings", "billboardsFaceCameraPosition", QualitySettings.billboardsFaceCameraPosition, "If enabled, billboards will face towards camera position rather than camera orientation.");
            lodBias = Config.Bind<float>("QualitySettings", "lodBias", QualitySettings.lodBias, "Global multiplier for the LOD's switching distance.");
            masterTextureLimit = Config.Bind<int>("QualitySettings", "masterTextureLimit", QualitySettings.masterTextureLimit, "A texture size limit applied to all textures.");
            maximumLODLevel = Config.Bind<int>("QualitySettings", "maximumLODLevel", QualitySettings.maximumLODLevel, "A maximum LOD level. All LOD groups.");
            maxQueuedFrames = Config.Bind<int>("QualitySettings", "maxQueuedFrames", QualitySettings.maxQueuedFrames, "Maximum number of frames queued up by graphics driver.");
            particleRaycastBudget = Config.Bind<int>("QualitySettings", "particleRaycastBudget", QualitySettings.particleRaycastBudget, "Budget for how many ray casts can be performed per frame for approximate collision testing.");
            pixelLightCount = Config.Bind<int>("QualitySettings", "pixelLightCount", QualitySettings.pixelLightCount, "The maximum number of pixel lights that should affect any object.");
            realtimeReflectionProbes = Config.Bind<bool>("QualitySettings", "realtimeReflectionProbes", QualitySettings.realtimeReflectionProbes, "Enables realtime reflection probes.");
            resolutionScalingFixedDPIFactor = Config.Bind<float>("QualitySettings", "resolutionScalingFixedDPIFactor", QualitySettings.resolutionScalingFixedDPIFactor, "In resolution scaling mode, this factor is used to multiply with the target Fixed DPI specified to get the actual Fixed DPI to use for this quality setting.");
            shadowCascade2Split = Config.Bind<float>("QualitySettings", "shadowCascade2Split", QualitySettings.shadowCascade2Split, "The normalized cascade distribution for a 2 cascade setup. The value defines the position of the cascade with respect to Zero.");
            shadowCascade4Split = Config.Bind<Vector3>("QualitySettings", "shadowCascade4Split", QualitySettings.shadowCascade4Split, "The normalized cascade start position for a 4 cascade setup. Each member of the vector defines the normalized position of the coresponding cascade with respect to Zero.");
            shadowCascades = Config.Bind<int>("QualitySettings", "shadowCascades", QualitySettings.shadowCascades, "Number of cascades to use for directional light shadows.");
            shadowDistance = Config.Bind<float>("QualitySettings", "shadowDistance", QualitySettings.shadowDistance, "Shadow drawing distance.");
            shadowmaskMode = Config.Bind<ShadowmaskMode>("QualitySettings", "shadowmaskMode", QualitySettings.shadowmaskMode, "The rendering mode of Shadowmask.");
            shadowNearPlaneOffset = Config.Bind<float>("QualitySettings", "shadowNearPlaneOffset", QualitySettings.shadowNearPlaneOffset, "Offset shadow frustum near plane.");
            shadowProjection = Config.Bind<ShadowProjection>("QualitySettings", "shadowProjection", QualitySettings.shadowProjection, "Directional light shadow projection.");
            shadowResolution = Config.Bind<ShadowResolution>("QualitySettings", "shadowResolution", QualitySettings.shadowResolution, "The default resolution of the shadow maps.");
            shadows = Config.Bind<ShadowQuality>("QualitySettings", "shadows", QualitySettings.shadows, "Realtime Shadows type to be used.");
            skinWeights = Config.Bind<SkinWeights>("QualitySettings", "skinWeights", QualitySettings.skinWeights, "The maximum number of bone weights that can affect a vertex, for all skinned meshes in the project.");
            softParticles = Config.Bind<bool>("QualitySettings", "softParticles", QualitySettings.softParticles, "Should soft blending be used for particles?");
            softVegetation = Config.Bind<bool>("QualitySettings", "softVegetation", QualitySettings.softVegetation, "Use a two-pass shader for the vegetation in the terrain engine.");
            streamingMipmapsActive = Config.Bind<bool>("QualitySettings", "streamingMipmapsActive", QualitySettings.streamingMipmapsActive, "Enable automatic streaming of texture mipmap levels based on their distance from all active cameras.");
            streamingMipmapsAddAllCameras = Config.Bind<bool>("QualitySettings", "streamingMipmapsAddAllCameras", QualitySettings.streamingMipmapsAddAllCameras, "Process all enabled Cameras for texture streaming (rather than just those with StreamingController components).");
            streamingMipmapsMaxFileIORequests = Config.Bind<int>("QualitySettings", "streamingMipmapsMaxFileIORequests", QualitySettings.streamingMipmapsMaxFileIORequests, "The maximum number of active texture file IO requests from the texture streaming system.");
            streamingMipmapsMaxLevelReduction = Config.Bind<int>("QualitySettings", "streamingMipmapsMaxLevelReduction", QualitySettings.streamingMipmapsMaxLevelReduction, "The maximum number of mipmap levels to discard for each texture.");
            streamingMipmapsMemoryBudget = Config.Bind<float>("QualitySettings", "streamingMipmapsMemoryBudget", QualitySettings.streamingMipmapsMemoryBudget, "The total amount of memory to be used by streaming and non-streaming textures.");
            vSyncCount = Config.Bind<int>("QualitySettings", "vSyncCount", QualitySettings.vSyncCount, "The VSync Count.");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

            SetGraphicsSettings();

            foreach(var setting in Config)
            {
                if(setting.Key.Section == "QualitySettings")
                {
                    if(setting.Value is ConfigEntry<bool>)
                    {
                        (setting.Value as ConfigEntry<bool>).SettingChanged += BepInExPlugin_SettingChanged;

                    }
                    else if(setting.Value is ConfigEntry<int>)
                    {
                        (setting.Value as ConfigEntry<int>).SettingChanged += BepInExPlugin_SettingChanged;
                    }
                    else if(setting.Value is ConfigEntry<float>)
                    {
                        (setting.Value as ConfigEntry<float>).SettingChanged += BepInExPlugin_SettingChanged;
                    }
                    else if(setting.Value is ConfigEntry<AnisotropicFiltering>)
                    {
                        (setting.Value as ConfigEntry<AnisotropicFiltering>).SettingChanged += BepInExPlugin_SettingChanged;
                    }
                    else if(setting.Value is ConfigEntry<ShadowmaskMode>)
                    {
                        (setting.Value as ConfigEntry<ShadowmaskMode>).SettingChanged += BepInExPlugin_SettingChanged;
                    }
                    else if(setting.Value is ConfigEntry<ShadowProjection>)
                    {
                        (setting.Value as ConfigEntry<ShadowProjection>).SettingChanged += BepInExPlugin_SettingChanged;
                    }
                    else if(setting.Value is ConfigEntry<ShadowResolution>)
                    {
                        (setting.Value as ConfigEntry<ShadowResolution>).SettingChanged += BepInExPlugin_SettingChanged;
                    }
                    else if(setting.Value is ConfigEntry<ShadowQuality>)
                    {
                        (setting.Value as ConfigEntry<ShadowQuality>).SettingChanged += BepInExPlugin_SettingChanged;
                    }
                    else if(setting.Value is ConfigEntry<SkinWeights>)
                    {
                        (setting.Value as ConfigEntry<SkinWeights>).SettingChanged += BepInExPlugin_SettingChanged;
                    }
                }
            }

        }

        public void Update()
        {
            if (modEnabled.Value && AedenthornUtils.CheckKeyDown(hotkey.Value))
                SetGraphicsSettings();
        }

        public void BepInExPlugin_SettingChanged(object sender, System.EventArgs e)
        {
            if(modEnabled.Value && reloadOnChange.Value)
                SetGraphicsSettings();
        }

        public static void SetGraphicsSettings()
        {
            if (!modEnabled.Value)
                return;

            QualitySettings.anisotropicFiltering = anisotropicFiltering.Value;
            QualitySettings.antiAliasing = antiAliasing.Value;
            QualitySettings.asyncUploadBufferSize = asyncUploadBufferSize.Value;
            QualitySettings.asyncUploadPersistentBuffer = asyncUploadPersistentBuffer.Value;
            QualitySettings.asyncUploadTimeSlice = asyncUploadTimeSlice.Value;
            QualitySettings.billboardsFaceCameraPosition = billboardsFaceCameraPosition.Value;
            QualitySettings.lodBias = lodBias.Value;
            QualitySettings.masterTextureLimit = masterTextureLimit.Value;
            QualitySettings.maximumLODLevel = maximumLODLevel.Value;
            QualitySettings.maxQueuedFrames = maxQueuedFrames.Value;
            QualitySettings.particleRaycastBudget = particleRaycastBudget.Value;
            QualitySettings.pixelLightCount = pixelLightCount.Value;
            QualitySettings.realtimeReflectionProbes = realtimeReflectionProbes.Value;
            QualitySettings.resolutionScalingFixedDPIFactor = resolutionScalingFixedDPIFactor.Value;
            QualitySettings.shadowCascade2Split = shadowCascade2Split.Value;
            QualitySettings.shadowCascade4Split = shadowCascade4Split.Value;
            QualitySettings.shadowCascades = shadowCascades.Value;
            QualitySettings.shadowDistance = shadowDistance.Value;
            QualitySettings.shadowmaskMode = shadowmaskMode.Value;
            QualitySettings.shadowNearPlaneOffset = shadowNearPlaneOffset.Value;
            QualitySettings.shadowProjection = shadowProjection.Value;
            QualitySettings.shadowResolution = shadowResolution.Value;
            QualitySettings.shadows = shadows.Value;
            QualitySettings.skinWeights = skinWeights.Value;
            QualitySettings.softParticles = softParticles.Value;
            QualitySettings.softVegetation = softVegetation.Value;
            QualitySettings.streamingMipmapsActive = streamingMipmapsActive.Value;
            QualitySettings.streamingMipmapsAddAllCameras = streamingMipmapsAddAllCameras.Value;
            QualitySettings.streamingMipmapsMaxFileIORequests = streamingMipmapsMaxFileIORequests.Value;
            QualitySettings.streamingMipmapsMaxLevelReduction = streamingMipmapsMaxLevelReduction.Value;
            QualitySettings.streamingMipmapsMemoryBudget = streamingMipmapsMemoryBudget.Value;
            QualitySettings.vSyncCount = vSyncCount.Value;
        }


        [HarmonyPatch(typeof(Settings), "ApplyQualitySettings")]
        public static class Settings_ApplyQualitySettings_Patch
        {
            public static void Postfix()
            {
                if (!modEnabled.Value)
                    return;
                SetGraphicsSettings();
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
