using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.PostProcessing;

namespace PostProcessing
{
    [BepInPlugin("aedenthorn.PostProcessing", "Post Processing", "0.1.2")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static bool defaultPostProcessingSet = false;
        private static VignetteModel.Settings defaultVignetteSettings;
        private static bool defaultVignetteEnabled;
        private static BloomModel.Settings defaultBloomSettings;
        private static EyeAdaptationModel.Settings defaultEyeAdaptSettings;
        private static bool defaultEyeAdaptEnabled;
        private static MotionBlurModel.Settings defaultMotionBlurSettings;
        private static DepthOfFieldModel.Settings defaultDepthOfFieldSettings;
        private static ColorGradingModel.Settings defaultColorGradingSettings;
        private static AmbientOcclusionModel.Settings defaultAOSettings;
        private static ChromaticAberrationModel.Settings defaultCASettings;
        private static ScreenSpaceReflectionModel.Settings defaultSSRSettings;
        private static bool defaultSSREnabled;

        private static bool defaultAASet = false;
        private static AntialiasingModel.Settings defaultAATaaSettings;
        private static AntialiasingModel.Settings defaultAAFxaaSettings;

        private static readonly bool isDebug = true;
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<string> hotKey;

		public static  ConfigEntry<bool> customVignette;
		public static  ConfigEntry<bool> customBloom;
		public static  ConfigEntry<bool> customEyeAdapt;
		public static  ConfigEntry<bool> customMotionBlur;
		public static  ConfigEntry<bool> customColorGrading;
		public static  ConfigEntry<bool> customDepthOfField;
		public static  ConfigEntry<bool> customAA;
		public static  ConfigEntry<bool> customAO;
		public static  ConfigEntry<bool> customCA;
		public static  ConfigEntry<bool> customSSR;

		public static  ConfigEntry<Color> vignetteColor;

		public static  ConfigEntry<float> vignetteOpacity;
		public static  ConfigEntry<float> vignetteIntensity;
		public static  ConfigEntry<Vector2> vignetteCenter;
		public static  ConfigEntry<float> vignetteSmoothness;
		public static  ConfigEntry<float> vignetteRoundness;
		public static  ConfigEntry<bool> vignetteRounded;

		public static  ConfigEntry<float> bloomIntensity;
		public static  ConfigEntry<float> bloomThreshold;
		public static  ConfigEntry<float> bloomSoftKnee;
		public static  ConfigEntry<float> bloomRadius;
		public static  ConfigEntry<bool> bloomAntiFlicker;
		public static  ConfigEntry<float> bloomLensDirtIntensity;

		public static  ConfigEntry<float> eyeAdaptLowPercent;
		public static  ConfigEntry<float> eyeAdaptHighPercent;
		public static  ConfigEntry<float> eyeAdaptMinLuminance;
		public static  ConfigEntry<float> eyeAdaptMaxLuminance;
		public static  ConfigEntry<float> eyeAdaptKeyValue;
		public static  ConfigEntry<bool> eyeAdaptDynamicKeyValue;
		public static  ConfigEntry<EyeAdaptationModel.EyeAdaptationType> eyeAdaptAdaptationType;
		public static  ConfigEntry<float> eyeAdaptSpeedUp;
		public static  ConfigEntry<float> eyeAdaptSpeedDown;
		public static  ConfigEntry<int> eyeAdaptLogMin;
		public static  ConfigEntry<int> eyeAdaptLogMax;

		public static  ConfigEntry<float> motionBlurShutterAngle;
		public static  ConfigEntry<int> motionBlurSampleCount;
		public static  ConfigEntry<float> motionBlurFrameBlending;

		public static  ConfigEntry<float> depthOfFieldFocusDistance;
		public static  ConfigEntry<float> depthOfFieldAperture;
		public static  ConfigEntry<float> depthOfFieldFocalLength;
		public static  ConfigEntry<bool> depthOfFieldUseCameraFov;
		public static  ConfigEntry<DepthOfFieldModel.KernelSize> depthOfFieldKernelSize;

		public static  ConfigEntry<ColorGradingModel.Tonemapper> colorGradingTonemapper;
		public static  ConfigEntry<Vector3> colorGradingChannelMixerRed;
		public static  ConfigEntry<Vector3> colorGradingChannelMixerGreen;
		public static  ConfigEntry<Vector3> colorGradingChannelMixerBlue;
		public static  ConfigEntry<int> colorGradingChannelMixerCurrentEditingChannel;
		public static  ConfigEntry<float> colorGradingNeutralBlackIn;
		public static  ConfigEntry<float> colorGradingNeutralWhiteIn;
		public static  ConfigEntry<float> colorGradingNeutralBlackOut;
		public static  ConfigEntry<float> colorGradingNeutralWhiteOut;
		public static  ConfigEntry<float> colorGradingNeutralWhiteLevel;
		public static  ConfigEntry<float> colorGradingNeutralWhiteClip;

		public static  ConfigEntry<float> colorGradingPostExposure;
		public static  ConfigEntry<float> colorGradingTemperature;
		public static  ConfigEntry<float> colorGradingTint;
		public static  ConfigEntry<float> colorGradingHueShift;
		public static  ConfigEntry<float> colorGradingSaturation;
		public static  ConfigEntry<float> colorGradingContrast;

		public static  ConfigEntry<AntialiasingModel.Method> AAMethod;
		public static  ConfigEntry<AntialiasingModel.FxaaPreset> AAFxaaPreset;
		public static  ConfigEntry<float> AAJitterSpread;
		public static  ConfigEntry<float> AASharpen;
		public static  ConfigEntry<float> AAStationaryBlending;
		public static  ConfigEntry<float> AAMotionBlending;

		public static  ConfigEntry<float> AOIntensity;
		public static  ConfigEntry<float> AOIntensityFar;
		public static  ConfigEntry<float> AOFarDistance;
		public static  ConfigEntry<float> AORadius;
		public static  ConfigEntry<AmbientOcclusionModel.SampleCount> AOSampleCount;
		public static  ConfigEntry<bool> AODownsampling;
		public static  ConfigEntry<bool> AOForceForwardCompatibility;
		public static  ConfigEntry<bool> AOAmbientOnly;
		public static  ConfigEntry<bool> AOHighPrecision;
		
        public static  ConfigEntry<float> CAIntensity;

        public static  ConfigEntry<ScreenSpaceReflectionModel.SSRReflectionBlendType> SSRBlendType;
        public static  ConfigEntry<ScreenSpaceReflectionModel.SSRResolution> SSRReflectionQuality;
        public static  ConfigEntry<float> SSRMaxDistance;
        public static  ConfigEntry<int> SSRIteractionCount;
        public static  ConfigEntry<int> SSRStepSize;
        public static  ConfigEntry<float> SSRWidthModifier;
        public static  ConfigEntry<float> SSRReflectionBlur;
        public static  ConfigEntry<bool> SSRReflectBackFaces;
        
        public static  ConfigEntry<float> SSRReflectionMultiplier;
        public static  ConfigEntry<float> SSRFadeDistance;
        public static  ConfigEntry<float> SSRFresnelFade;
        public static  ConfigEntry<float> SSRFresnelFadePower;

        public static  ConfigEntry<float> SSRMaskIntensity;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("_General", "Enabled", true, "Enable this mod");
            hotKey = Config.Bind<string>("_Options", "HotKey", "[0]", "Key to toggle mod");
            nexusID = Config.Bind<int>("General", "NexusID", 1587, "Nexus mod ID for updates");

            customVignette = Config.Bind<bool>("_Settings", "customVignette", false, "customVignette");
			customBloom = Config.Bind<bool>("_Settings", "customBloom", false, "customBloom");
			customEyeAdapt = Config.Bind<bool>("_Settings", "customEyeAdapt", false, "customEyeAdapt");
			customMotionBlur = Config.Bind<bool>("_Settings", "customMotionBlur", false, "customMotionBlur");
			customColorGrading = Config.Bind<bool>("_Settings", "customColorGrading", false, "customColorGrading");
			customDepthOfField = Config.Bind<bool>("_Settings", "customDepthOfField", false, "customDepthOfField");
			customAA = Config.Bind<bool>("_Settings", "customAA", false, "customAA");
			customAO = Config.Bind<bool>("_Settings", "customAO", false, "customAO");
			customCA = Config.Bind<bool>("_Settings", "customCA", false, "customCA");
            customSSR = Config.Bind<bool>("_Settings", "customSSR", false, "customSSR");

			vignetteColor = Config.Bind<Color>("VignetteSettings", "vignetteColor", new Color(0,0,0,1), "vignetteColor");
			vignetteOpacity = Config.Bind<float>("VignetteSettings", "vignetteOpacity", 1f, "vignetteOpacity");
			vignetteIntensity = Config.Bind<float>("VignetteSettings", "vignetteIntensity", 0.45f, "vignetteIntensity");
            vignetteCenter = Config.Bind<Vector2>("VignetteSettings", "vignetteCenter", new Vector2(0.5f,0.5f), "vignetteCenter");
			vignetteSmoothness = Config.Bind<float>("VignetteSettings", "vignetteSmoothness", 0.2f, "vignetteSmoothness");
			vignetteRoundness = Config.Bind<float>("VignetteSettings", "vignetteRoundness", 1f, "vignetteRoundness");
			vignetteRounded = Config.Bind<bool>("VignetteSettings", "vignetteRounded", false, "vignetteRounded");

			bloomIntensity = Config.Bind<float>("BloomSettings", "bloomIntensity", 0.3f, "bloomIntensity");
			bloomThreshold = Config.Bind<float>("BloomSettings", "bloomThreshold", 0.7f, "bloomThreshold");
			bloomSoftKnee = Config.Bind<float>("BloomSettings", "bloomSoftKnee", 0.7f, "bloomSoftKnee");
			bloomRadius = Config.Bind<float>("BloomSettings", "bloomRadius", 5f, "bloomRadius");
			bloomAntiFlicker = Config.Bind<bool>("BloomSettings", "bloomAntiFlicker", true, "bloomAntiFlicker");
			bloomLensDirtIntensity = Config.Bind<float>("BloomSettings", "bloomLensDirtIntensity", 10.4f, "bloomLensDirtIntensity");

			eyeAdaptLowPercent = Config.Bind<float>("EyeAdaptSettings", "eyeAdaptLowPercent", 12.2743f, "eyeAdaptLowPercent");
			eyeAdaptHighPercent = Config.Bind<float>("EyeAdaptSettings", "eyeAdaptHighPercent", 87.7257f, "eyeAdaptHighPercent");
			eyeAdaptMinLuminance = Config.Bind<float>("EyeAdaptSettings", "eyeAdaptMinLuminance", -4f, "eyeAdaptMinLuminance");
			eyeAdaptMaxLuminance = Config.Bind<float>("EyeAdaptSettings", "eyeAdaptMaxLuminance", -1f, "eyeAdaptMaxLuminance");
			eyeAdaptKeyValue = Config.Bind<float>("EyeAdaptSettings", "eyeAdaptKeyValue", 0.14f, "eyeAdaptKeyValue");
			eyeAdaptDynamicKeyValue = Config.Bind<bool>("EyeAdaptSettings", "eyeAdaptDynamicKeyValue", false, "eyeAdaptDynamicKeyValue");
			eyeAdaptAdaptationType = Config.Bind<EyeAdaptationModel.EyeAdaptationType>("EyeAdaptSettings", "eyeAdaptAdaptationType", EyeAdaptationModel.EyeAdaptationType.Fixed, "eyeAdaptAdaptationType");
			eyeAdaptSpeedUp = Config.Bind<float>("EyeAdaptSettings", "eyeAdaptSpeedUp", 4f, "eyeAdaptSpeedUp");
			eyeAdaptSpeedDown = Config.Bind<float>("EyeAdaptSettings", "eyeAdaptSpeedDown", 4f, "eyeAdaptSpeedDown");
			eyeAdaptLogMin = Config.Bind<int>("EyeAdaptSettings", "eyeAdaptLogMin", -8, "eyeAdaptLogMin");
			eyeAdaptLogMax = Config.Bind<int>("EyeAdaptSettings", "eyeAdaptLogMax", 4, "eyeAdaptLogMax");

			motionBlurShutterAngle = Config.Bind<float>("MotionBlurSettings", "motionBlurShutterAngle", 150f, "motionBlurShutterAngle");
			motionBlurSampleCount = Config.Bind<int>("MotionBlurSettings", "motionBlurSampleCount", 10, "motionBlurSampleCount");
			motionBlurFrameBlending = Config.Bind<float>("MotionBlurSettings", "motionBlurFrameBlending", 0f, "motionBlurFrameBlending");

			depthOfFieldFocusDistance = Config.Bind<float>("DOFSettings", "depthOfFieldFocusDistance", 35.96f, "depthOfFieldFocusDistance");
			depthOfFieldAperture = Config.Bind<float>("DOFSettings", "depthOfFieldAperture", 1f, "depthOfFieldAperture");
			depthOfFieldFocalLength = Config.Bind<float>("DOFSettings", "depthOfFieldFocalLength", 70f, "depthOfFieldFocalLength");
			depthOfFieldUseCameraFov = Config.Bind<bool>("DOFSettings", "depthOfFieldUseCameraFov", false, "depthOfFieldUseCameraFov");
			depthOfFieldKernelSize = Config.Bind<DepthOfFieldModel.KernelSize>("DOFSettings", "depthOfFieldKernelSize", DepthOfFieldModel.KernelSize.Medium, "depthOfFieldKernelSize");

			colorGradingTonemapper = Config.Bind<ColorGradingModel.Tonemapper>("ColorGradingSettings", "colorGradingTonemapper", ColorGradingModel.Tonemapper.ACES, "colorGradingTonemapper");
			colorGradingChannelMixerRed = Config.Bind<Vector3>("ColorGradingSettings", "colorGradingChannelMixerRed", new Vector3(1,0,0), "colorGradingChannelMixerRed");
			colorGradingChannelMixerGreen = Config.Bind<Vector3>("ColorGradingSettings", "colorGradingChannelMixerGreen", new Vector3(0,1,0), "colorGradingChannelMixerGreen");
			colorGradingChannelMixerBlue = Config.Bind<Vector3>("ColorGradingSettings", "colorGradingChannelMixerBlue", new Vector3(0,0,1), "colorGradingChannelMixerBlue");
            colorGradingChannelMixerCurrentEditingChannel = Config.Bind<int>("ColorGradingSettings", "colorGradingChannelMixerCurrentEditingChannel", 0, "colorGradingChannelMixerCurrentEditingChannel");
			colorGradingNeutralBlackIn = Config.Bind<float>("ColorGradingSettings", "colorGradingNeutralBlackIn", 0.02f, "colorGradingNeutralBlackIn");
			colorGradingNeutralWhiteIn = Config.Bind<float>("ColorGradingSettings", "colorGradingNeutralWhiteIn", 10f, "colorGradingNeutralWhiteIn");
			colorGradingNeutralBlackOut = Config.Bind<float>("ColorGradingSettings", "colorGradingNeutralBlackOut", 0f, "colorGradingNeutralBlackOut");
			colorGradingNeutralWhiteOut = Config.Bind<float>("ColorGradingSettings", "colorGradingNeutralWhiteOut", 10f, "colorGradingNeutralWhiteOut");
			colorGradingNeutralWhiteLevel = Config.Bind<float>("ColorGradingSettings", "colorGradingNeutralWhiteLevel", 5.3f, "colorGradingNeutralWhiteLevel");
			colorGradingNeutralWhiteClip = Config.Bind<float>("ColorGradingSettings", "colorGradingNeutralWhiteClip", 10f, "colorGradingNeutralWhiteClip");

			colorGradingPostExposure = Config.Bind<float>("ColorGradingSettings", "colorGradingPostExposure", 1f, "colorGradingPostExposure");
			colorGradingTemperature = Config.Bind<float>("ColorGradingSettings", "colorGradingTemperature", -8f, "colorGradingTemperature");
			colorGradingTint = Config.Bind<float>("ColorGradingSettings", "colorGradingTint", 0f, "colorGradingTint");
			colorGradingHueShift = Config.Bind<float>("ColorGradingSettings", "colorGradingHueShift", 0f, "colorGradingHueShift");
			colorGradingSaturation = Config.Bind<float>("ColorGradingSettings", "colorGradingSaturation", 1f, "colorGradingSaturation");
			colorGradingContrast = Config.Bind<float>("ColorGradingSettings", "colorGradingContrast", 1.2f, "colorGradingContrast");

			AAMethod = Config.Bind<AntialiasingModel.Method>("AASettings", "AAMethod", AntialiasingModel.Method.Fxaa, "AAMethod");
			AAFxaaPreset = Config.Bind<AntialiasingModel.FxaaPreset>("AASettings", "AAFxaaPreset", AntialiasingModel.FxaaPreset.ExtremeQuality, "AAFxaaPreset");
			AAJitterSpread = Config.Bind<float>("AASettings", "AAJitterSpread", 0.2f, "AAJitterSpread");
			AASharpen = Config.Bind<float>("AASettings", "AASharpen", 0.3f, "AASharpen");
			AAStationaryBlending = Config.Bind<float>("AASettings", "AAStationaryBlending", 0.95f, "AAStationaryBlending");
			AAMotionBlending = Config.Bind<float>("AASettings", "AAMotionBlending", 0.85f, "AAMotionBlending");

			AOIntensity = Config.Bind<float>("AOSettings", "AOIntensity", 1f, "AOIntensity");
            AOIntensityFar = Config.Bind<float>("AOSettings", "AOIntensityFar", 1.5f, "AOIntensityFar");
            AOFarDistance = Config.Bind<float>("AOSettings", "AOFarDistance", 150f, "AOFarDistance");
			AORadius = Config.Bind<float>("AOSettings", "AORadius", 0.15f, "AORadius");
			AOSampleCount = Config.Bind<AmbientOcclusionModel.SampleCount>("AOSettings", "AOSampleCount", AmbientOcclusionModel.SampleCount.Medium, "AOSampleCount");
			AODownsampling = Config.Bind<bool>("AOSettings", "AODownsampling", false, "AODownsampling");
			AOForceForwardCompatibility = Config.Bind<bool>("AOSettings", "AOForceForwardCompatibility", false, "AOForceForwardCompatibility");
			AOAmbientOnly = Config.Bind<bool>("AOSettings", "AOAmbientOnly", false, "AOAmbientOnly");
			AOHighPrecision = Config.Bind<bool>("AOSettings", "AOHighPrecision", false, "AOHighPrecision");

            CAIntensity = Config.Bind<float>("CASettings", "CAIntensity", 0.1f, "CAIntensity");
            
            SSRBlendType = Config.Bind<ScreenSpaceReflectionModel.SSRReflectionBlendType>("ReflectionSettings", "SSRBlendType", ScreenSpaceReflectionModel.SSRReflectionBlendType.PhysicallyBased, "SSRBlendType");
            SSRReflectionQuality = Config.Bind<ScreenSpaceReflectionModel.SSRResolution>("ReflectionSettings", "SSRReflectionQuality", ScreenSpaceReflectionModel.SSRResolution.Low, "SSRReflectionQuality");
            SSRMaxDistance = Config.Bind<float>("ReflectionSettings", "SSRMaxDistance", 100f, "SSRMaxDistance");
            SSRIteractionCount = Config.Bind<int>("ReflectionSettings", "SSRIteractionCount", 256, "SSRIteractionCount");
            SSRStepSize = Config.Bind<int>("ReflectionSettings", "SSRStepSize", 3, "SSRStepSize");
            SSRWidthModifier = Config.Bind<float>("ReflectionSettings", "SSRWidthModifier", 0.5f, "SSRWidthModifier");
            SSRReflectionBlur = Config.Bind<float>("ReflectionSettings", "SSRReflectionBlur", 1f, "SSRReflectionBlur");
            SSRReflectBackFaces = Config.Bind<bool>("ReflectionSettings", "SSRReflectBackFaces", true, "SSRReflectBackFaces");

            SSRReflectionMultiplier = Config.Bind<float>("ReflectionSettings", "SSRReflectionMultiplier", 0.93f, "SSRReflectionMultiplier");
            SSRFadeDistance = Config.Bind<float>("ReflectionSettings", "SSRFadeDistance", 100f, "SSRFadeDistance");
            SSRFresnelFade = Config.Bind<float>("ReflectionSettings", "SSRFresnelFade", 1f, "SSRFresnelFade");
            SSRFresnelFadePower = Config.Bind<float>("ReflectionSettings", "SSRFresnelFadePower", 1f, "SSRFresnelFadePower");
            SSRMaskIntensity = Config.Bind<float>("ReflectionSettings", "SSRMaskIntensity", 0.03f, "SSRMaskIntensity");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MetadataHelper.GetMetadata(this).GUID);
        }

        private void Update()
        {
            if (AedenthornUtils.CheckKeyDown(hotKey.Value))
            {
                modEnabled.Value = !modEnabled.Value;
            }
        }

        [HarmonyPatch(typeof(PostProcessingBehaviour), "OnEnable")]
        static class PostProcessingBehaviour_OnEnable_Patch
        {

            static void Prefix(PostProcessingBehaviour __instance)
            {
                if (__instance.profile != null && !defaultPostProcessingSet)
                {
                    defaultVignetteSettings = __instance.profile.vignette.settings;
                    defaultVignetteEnabled = __instance.profile.vignette.enabled;
                    defaultBloomSettings = __instance.profile.bloom.settings;
                    defaultEyeAdaptSettings = __instance.profile.eyeAdaptation.settings;
                    defaultEyeAdaptEnabled = __instance.profile.eyeAdaptation.enabled;
                    defaultMotionBlurSettings = __instance.profile.motionBlur.settings;
                    defaultDepthOfFieldSettings = __instance.profile.depthOfField.settings;
                    defaultColorGradingSettings = __instance.profile.colorGrading.settings;
                    defaultAOSettings = __instance.profile.ambientOcclusion.settings;
                    defaultCASettings = __instance.profile.chromaticAberration.settings;
                    defaultSSRSettings = __instance.profile.screenSpaceReflection.settings;
                    defaultSSREnabled = __instance.profile.screenSpaceReflection.enabled;

                    defaultPostProcessingSet = true;
                }
            }
        }
        [HarmonyPatch(typeof(PostProcessingBehaviour), "OnPreCull")]
        static class PostProcessingBehaviour_OnPreCull_Patch
        {
            static void Postfix(ref VignetteComponent ___m_Vignette, ref BloomComponent ___m_Bloom, ref EyeAdaptationComponent ___m_EyeAdaptation, ref DepthOfFieldComponent ___m_DepthOfField, ref MotionBlurComponent ___m_MotionBlur, ref ColorGradingComponent ___m_ColorGrading, ref TaaComponent ___m_Taa, ref FxaaComponent ___m_Fxaa, ref AmbientOcclusionComponent ___m_AmbientOcclusion, ref ChromaticAberrationComponent ___m_ChromaticAberration, ref ScreenSpaceReflectionComponent ___m_ScreenSpaceReflection)
            {
                if (modEnabled.Value && customVignette.Value)
                {
                    VignetteModel.Settings vSettings = new VignetteModel.Settings
                    {
                        mode = VignetteModel.Mode.Classic,
                        opacity = vignetteOpacity.Value,
                        intensity = vignetteIntensity.Value,
                        color = vignetteColor.Value,
                        center = vignetteCenter.Value,
                        smoothness = vignetteSmoothness.Value,
                        roundness = vignetteRoundness.Value,
                        rounded = vignetteRounded.Value
                    };
                    ___m_Vignette.model.settings = vSettings;
                    ___m_Vignette.model.enabled = true;
                }
                else
                {
                    ___m_Vignette.model.settings = defaultVignetteSettings;
                    ___m_Vignette.model.enabled = defaultVignetteEnabled;
                }

                if (modEnabled.Value && customBloom.Value)
                {
                    BloomModel.BloomSettings bbSettings = new BloomModel.BloomSettings
                    {
                        intensity = bloomIntensity.Value,
                        threshold = bloomThreshold.Value,
                        softKnee = bloomSoftKnee.Value,
                        radius = bloomRadius.Value,
                        antiFlicker = bloomAntiFlicker.Value
                    };

                    BloomModel.LensDirtSettings blSettings = new BloomModel.LensDirtSettings
                    {
                        intensity = bloomLensDirtIntensity.Value
                    };
                    BloomModel.Settings bSettings = new BloomModel.Settings
                    {
                        bloom = bbSettings,
                        lensDirt = blSettings
                    };

                    ___m_Bloom.model.settings = bSettings;
                }
                else
                {
                    ___m_Bloom.model.settings = defaultBloomSettings;
                }

                if (modEnabled.Value && customEyeAdapt.Value)
                {
                    EyeAdaptationModel.Settings eSettings = new EyeAdaptationModel.Settings
                    {
                        lowPercent = eyeAdaptLowPercent.Value,
                        highPercent = eyeAdaptHighPercent.Value,
                        minLuminance = eyeAdaptMinLuminance.Value,
                        maxLuminance = eyeAdaptMaxLuminance.Value,
                        keyValue = eyeAdaptKeyValue.Value,
                        dynamicKeyValue = eyeAdaptDynamicKeyValue.Value,
                        adaptationType = eyeAdaptAdaptationType.Value,
                        speedUp = eyeAdaptSpeedUp.Value,
                        speedDown = eyeAdaptSpeedDown.Value,
                        logMin = eyeAdaptLogMin.Value,
                        logMax = eyeAdaptLogMax.Value,
                    };

                    ___m_EyeAdaptation.model.settings = eSettings;
                    ___m_EyeAdaptation.model.enabled = true;
                }
                else
                {
                    ___m_EyeAdaptation.model.settings = defaultEyeAdaptSettings;
                    ___m_EyeAdaptation.model.enabled = defaultEyeAdaptEnabled;
                }

                if (modEnabled.Value && customMotionBlur.Value)
                {
                    MotionBlurModel.Settings mSettings = new MotionBlurModel.Settings
                    {
                        shutterAngle = motionBlurShutterAngle.Value,
                        sampleCount = motionBlurSampleCount.Value,
                        frameBlending = motionBlurFrameBlending.Value
                    };

                    ___m_MotionBlur.model.settings = mSettings;
                }
                else
                {
                    ___m_MotionBlur.model.settings = defaultMotionBlurSettings;
                }

                if (modEnabled.Value && customDepthOfField.Value)
                {
                    DepthOfFieldModel.Settings dSettings = new DepthOfFieldModel.Settings
                    {
                        focusDistance = depthOfFieldFocusDistance.Value,
                        aperture = depthOfFieldAperture.Value,
                        focalLength = depthOfFieldFocalLength.Value,
                        useCameraFov = depthOfFieldUseCameraFov.Value,
                        kernelSize = depthOfFieldKernelSize.Value,
                    };

                    ___m_DepthOfField.model.settings = dSettings;
                    ___m_DepthOfField.model.enabled = true;
                }
                else
                {
                    ___m_DepthOfField.model.settings = defaultDepthOfFieldSettings;
                    ___m_DepthOfField.model.enabled = false;
                }

                if (modEnabled.Value && customColorGrading.Value)
                {
                    ColorGradingModel.TonemappingSettings ctSettings = new ColorGradingModel.TonemappingSettings
                    {
                        tonemapper = colorGradingTonemapper.Value,
                        neutralBlackIn = colorGradingNeutralBlackIn.Value,
                        neutralWhiteIn = colorGradingNeutralWhiteIn.Value,
                        neutralBlackOut = colorGradingNeutralBlackOut.Value,
                        neutralWhiteOut = colorGradingNeutralWhiteOut.Value,
                        neutralWhiteLevel = colorGradingNeutralWhiteLevel.Value,
                        neutralWhiteClip = colorGradingNeutralWhiteClip.Value
                    };

                    ColorGradingModel.BasicSettings cbSettings = new ColorGradingModel.BasicSettings
                    {
                        postExposure = colorGradingPostExposure.Value,
                        temperature = colorGradingTemperature.Value,
                        tint = colorGradingTint.Value,
                        hueShift = colorGradingHueShift.Value,
                        saturation = colorGradingSaturation.Value,
                        contrast = colorGradingContrast.Value
                    };

                    ColorGradingModel.ChannelMixerSettings cmSettings = new ColorGradingModel.ChannelMixerSettings
                    {
                        red = colorGradingChannelMixerRed.Value,
                        green = colorGradingChannelMixerGreen.Value,
                        blue = colorGradingChannelMixerBlue.Value,
                        currentEditingChannel = colorGradingChannelMixerCurrentEditingChannel.Value
                    };

                    ColorGradingModel.Settings cSettings = new ColorGradingModel.Settings
                    {
                        tonemapping = ctSettings,
                        basic = cbSettings,
                        channelMixer = cmSettings,
                        colorWheels = defaultColorGradingSettings.colorWheels,
                        curves = defaultColorGradingSettings.curves,
                    };

                    ___m_ColorGrading.model.settings = cSettings;
                }
                else
                {
                    ___m_ColorGrading.model.settings = defaultColorGradingSettings;
                }

                if (modEnabled.Value && customAO.Value)
                {
                    AmbientOcclusionModel.Settings aSettings = new AmbientOcclusionModel.Settings
                    {
                        intensity = AOIntensity.Value,
                        intensityFar = AOIntensityFar.Value,
                        farDistance = AOFarDistance.Value,
                        radius = AORadius.Value,
                        sampleCount = AOSampleCount.Value,
                        downsampling = AODownsampling.Value,
                        forceForwardCompatibility = AOForceForwardCompatibility.Value,
                        ambientOnly = AOAmbientOnly.Value,
                        highPrecision = AOHighPrecision.Value
                    };

                    ___m_AmbientOcclusion.model.settings = aSettings;
                }
                else
                {
                    ___m_AmbientOcclusion.model.settings = defaultAOSettings;
                }

                if(!defaultAASet)
                {
                    defaultAATaaSettings = ___m_Taa.model.settings;
                    defaultAAFxaaSettings = ___m_Fxaa.model.settings;
                    defaultAASet = true;
                }

                if (modEnabled.Value && customAA.Value)
                {
                    AntialiasingModel.FxaaSettings afSettings = new AntialiasingModel.FxaaSettings
                    {
                        preset = (AntialiasingModel.FxaaPreset) AAFxaaPreset.Value
                    };

                    AntialiasingModel.TaaSettings atSettings = new AntialiasingModel.TaaSettings
                    {
                        jitterSpread = AAJitterSpread.Value,
                        sharpen = AASharpen.Value,
                        stationaryBlending = AAStationaryBlending.Value,
                        motionBlending = AAMotionBlending.Value

                    };

                    AntialiasingModel.Settings aSettings = new AntialiasingModel.Settings
                    {
                        method = AAMethod.Value,
                        //method = ___m_Taa.model.settings.method,
                        fxaaSettings = afSettings,
                        taaSettings = atSettings
                    };

                    AntialiasingModel.Settings aSettings2 = new AntialiasingModel.Settings
                    {
                        method = AAMethod.Value,
                        //method = ___m_Fxaa.model.settings.method,
                        fxaaSettings = afSettings,
                        taaSettings = atSettings
                    };

                    ___m_Taa.model.settings = aSettings;
                    ___m_Fxaa.model.settings = aSettings2;
                }
                else
                {
                    ___m_Taa.model.settings = defaultAATaaSettings;
                    ___m_Fxaa.model.settings = defaultAAFxaaSettings;
                }

                if (modEnabled.Value && customCA.Value)
                {
                    ChromaticAberrationModel.Settings caSettings = new ChromaticAberrationModel.Settings
                    {
                        intensity = CAIntensity.Value,
                    };

                    ___m_ChromaticAberration.model.settings = caSettings;
                }
                else
                {
                    ___m_ChromaticAberration.model.settings = defaultCASettings;
                }

                if (modEnabled.Value && customSSR.Value)
                {
                    ___m_ScreenSpaceReflection.model.settings = new ScreenSpaceReflectionModel.Settings
                    {
                        reflection = new ScreenSpaceReflectionModel.ReflectionSettings
                        {
                            blendType = SSRBlendType.Value,
                            reflectionQuality = SSRReflectionQuality.Value,
                            maxDistance = SSRMaxDistance.Value,
                            iterationCount = SSRIteractionCount.Value,
                            stepSize = SSRStepSize.Value,
                            widthModifier = SSRWidthModifier.Value,
                            reflectionBlur = SSRReflectionBlur.Value,
                            reflectBackfaces = SSRReflectBackFaces.Value
                        },
                        intensity = new ScreenSpaceReflectionModel.IntensitySettings
                        {
                            reflectionMultiplier = SSRReflectionMultiplier.Value,
                            fadeDistance = SSRFadeDistance.Value,
                            fresnelFade = SSRFresnelFade.Value,
                            fresnelFadePower = SSRFresnelFadePower.Value
                        },
                        screenEdgeMask = new ScreenSpaceReflectionModel.ScreenEdgeMask
                        {
                            intensity = SSRMaskIntensity.Value
                        }
                    };
                    ___m_ScreenSpaceReflection.model.enabled = true;
                }
                else
                {
                    ___m_ScreenSpaceReflection.model.settings = defaultSSRSettings;
                    ___m_ScreenSpaceReflection.model.enabled = defaultSSREnabled;
                }
            }
        }
    }
}

