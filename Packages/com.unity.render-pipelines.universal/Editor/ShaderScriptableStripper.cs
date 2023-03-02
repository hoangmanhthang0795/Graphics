using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using System.Diagnostics.CodeAnalysis;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// Class used for Scriptable Shader keyword stripping in URP.
    /// </summary>
    internal class ShaderScriptableStripper : IShaderVariantStripper, IShaderVariantStripperScope
    {
        public bool active => UniversalRenderPipeline.asset != null;

        // Interfaces / Structs

        // Interface for info gathered when making builds. Used for
        // Scriptable Stripping and when testing the scriptable stripper.
        internal interface IShaderScriptableStrippingData
        {
            public ShaderFeatures shaderFeatures { get; set; }
            public VolumeFeatures volumeFeatures { get; set; }

            public bool isGLDevice { get; set; }
            public bool strip2DPasses { get; set; }
            public bool stripDebugDisplayShaders { get; set; }
            public bool stripScreenCoordOverrideVariants { get; set; }
            public bool stripUnusedVariants { get; set; }
            public bool stripUnusedPostProcessingVariants { get; set; }

            public Shader shader { get; set; }
            public ShaderType shaderType { get; set; }
            public ShaderCompilerPlatform shaderCompilerPlatform { get; set; }

            public string passName { get; set; }
            public PassType passType { get; set; }
            public PassIdentifier passIdentifier { get; set; }

            public bool IsHDRShaderVariantValid { get; set; }

            public bool IsShaderFeatureEnabled(ShaderFeatures feature);

            public bool IsVolumeFeatureEnabled(VolumeFeatures feature);

            public bool IsKeywordEnabled(LocalKeyword keyword);
            public bool PassHasKeyword(LocalKeyword keyword);
        }

        // Data containing all the info needed to compare
        // against the features gathered in ShaderBuildPreprocessor.cs
        internal struct StrippingData : IShaderScriptableStrippingData
        {
            public ShaderFeatures shaderFeatures { get; set; }
            public VolumeFeatures volumeFeatures { get; set; }

            public bool isGLDevice { get => variantData.shaderCompilerPlatform == ShaderCompilerPlatform.GLES3x || variantData.shaderCompilerPlatform == ShaderCompilerPlatform.OpenGLCore; set{} }
            public bool strip2DPasses { get; set; }
            public bool stripDebugDisplayShaders { get; set; }
            public bool stripScreenCoordOverrideVariants { get; set; }
            public bool stripUnusedVariants { get; set; }
            public bool stripUnusedPostProcessingVariants { get; set; }

            public Shader shader { get; set; }
            public ShaderType shaderType { get => passData.shaderType; set{} }
            public ShaderCompilerPlatform shaderCompilerPlatform { get => variantData.shaderCompilerPlatform; set {} }

            public string passName { get => passData.passName; set {} }
            public PassType passType { get => passData.passType; set {} }
            public PassIdentifier passIdentifier { get => passData.pass; set {} }

            public bool IsHDRShaderVariantValid { get => HDROutputUtils.IsShaderVariantValid(variantData.shaderKeywordSet, PlayerSettings.useHDRDisplay); set{ } }


            public bool IsKeywordEnabled(LocalKeyword keyword)
            {
                return variantData.shaderKeywordSet.IsEnabled(keyword);
            }

            public bool IsShaderFeatureEnabled(ShaderFeatures feature)
            {
                return (shaderFeatures & feature) != 0;
            }

            public bool IsVolumeFeatureEnabled(VolumeFeatures feature)
            {
                return (volumeFeatures & feature) != 0;
            }

            public bool PassHasKeyword(LocalKeyword keyword)
            {
                return ShaderUtil.PassHasKeyword(shader, passData.pass, keyword, passData.shaderType, shaderCompilerPlatform);
            }

            public ShaderSnippetData passData { get; set; }
            public ShaderCompilerData variantData { get; set; }
        }

        // Shaders
        Shader m_BokehDepthOfField = Shader.Find("Hidden/Universal Render Pipeline/BokehDepthOfField");
        Shader m_GaussianDepthOfField = Shader.Find("Hidden/Universal Render Pipeline/GaussianDepthOfField");
        Shader m_CameraMotionBlur = Shader.Find("Hidden/Universal Render Pipeline/CameraMotionBlur");
        Shader m_PaniniProjection = Shader.Find("Hidden/Universal Render Pipeline/PaniniProjection");
        Shader m_Bloom = Shader.Find("Hidden/Universal Render Pipeline/Bloom");
        Shader m_TerrainLit = Shader.Find("Universal Render Pipeline/Terrain/Lit");
        Shader m_StencilDeferred = Shader.Find("Hidden/Universal Render Pipeline/StencilDeferred");
        Shader m_UberPostShader = Shader.Find("Hidden/Universal Render Pipeline/UberPost");

        // Pass names
        public static readonly string kPassNameUniversal2D = "Universal2D";
        public static readonly string kPassNameGBuffer = "GBuffer";
        public static readonly string kPassNameForwardLit = "ForwardLit";
        public static readonly string kPassNameDepthNormals = "DepthNormals";

        // Keywords
        LocalKeyword m_MainLightShadows;
        LocalKeyword m_MainLightShadowsCascades;
        LocalKeyword m_MainLightShadowsScreen;
        LocalKeyword m_AdditionalLightsVertex;
        LocalKeyword m_AdditionalLightsPixel;
        LocalKeyword m_AdditionalLightShadows;
        LocalKeyword m_ReflectionProbeBlending;
        LocalKeyword m_ReflectionProbeBoxProjection;
        LocalKeyword m_CastingPunctualLightShadow;
        LocalKeyword m_SoftShadows;
        LocalKeyword m_MixedLightingSubtractive;
        LocalKeyword m_LightmapShadowMixing;
        LocalKeyword m_ShadowsShadowMask;
        LocalKeyword m_Lightmap;
        LocalKeyword m_DynamicLightmap;
        LocalKeyword m_DirectionalLightmap;
        LocalKeyword m_AlphaTestOn;
        LocalKeyword m_GbufferNormalsOct;
        LocalKeyword m_ScreenSpaceOcclusion;
        LocalKeyword m_UseFastSRGBLinearConversion;
        LocalKeyword m_LightLayers;
        LocalKeyword m_DecalLayers;
        LocalKeyword m_WriteRenderingLayers;
        LocalKeyword m_RenderPassEnabled;
        LocalKeyword m_DebugDisplay;
        LocalKeyword m_DBufferMRT1;
        LocalKeyword m_DBufferMRT2;
        LocalKeyword m_DBufferMRT3;
        LocalKeyword m_DecalNormalBlendLow;
        LocalKeyword m_DecalNormalBlendMedium;
        LocalKeyword m_DecalNormalBlendHigh;
        LocalKeyword m_ForwardPlus;
        LocalKeyword m_FoveatedRenderingNonUniformRaster;
        LocalKeyword m_EditorVisualization;
        LocalKeyword m_LODFadeCrossFade;
        LocalKeyword m_LightCookies;
        LocalKeyword m_LensDistortion;
        LocalKeyword m_ChromaticAberration;
        LocalKeyword m_BloomLQ;
        LocalKeyword m_BloomHQ;
        LocalKeyword m_BloomLQDirt;
        LocalKeyword m_BloomHQDirt;
        LocalKeyword m_HdrGrading;
        LocalKeyword m_ToneMapACES;
        LocalKeyword m_ToneMapNeutral;
        LocalKeyword m_FilmGrain;
        LocalKeyword m_ScreenCoordOverride;
        LocalKeyword m_ProbeVolumesL1;
        LocalKeyword m_ProbeVolumesL2;


        private LocalKeyword TryGetLocalKeyword(Shader shader, string name)
        {
            return shader.keywordSpace.FindKeyword(name);
        }

        private void InitializeLocalShaderKeywords([DisallowNull] Shader shader)
        {
            m_MainLightShadows = TryGetLocalKeyword(shader, ShaderKeywordStrings.MainLightShadows);
            m_MainLightShadowsCascades = TryGetLocalKeyword(shader, ShaderKeywordStrings.MainLightShadowCascades);
            m_MainLightShadowsScreen = TryGetLocalKeyword(shader, ShaderKeywordStrings.MainLightShadowScreen);
            m_AdditionalLightsVertex = TryGetLocalKeyword(shader, ShaderKeywordStrings.AdditionalLightsVertex);
            m_AdditionalLightsPixel = TryGetLocalKeyword(shader, ShaderKeywordStrings.AdditionalLightsPixel);
            m_AdditionalLightShadows = TryGetLocalKeyword(shader, ShaderKeywordStrings.AdditionalLightShadows);
            m_ReflectionProbeBlending = TryGetLocalKeyword(shader, ShaderKeywordStrings.ReflectionProbeBlending);
            m_ReflectionProbeBoxProjection = TryGetLocalKeyword(shader, ShaderKeywordStrings.ReflectionProbeBoxProjection);
            m_CastingPunctualLightShadow = TryGetLocalKeyword(shader, ShaderKeywordStrings.CastingPunctualLightShadow);
            m_SoftShadows = TryGetLocalKeyword(shader, ShaderKeywordStrings.SoftShadows);
            m_MixedLightingSubtractive = TryGetLocalKeyword(shader, ShaderKeywordStrings.MixedLightingSubtractive);
            m_LightmapShadowMixing = TryGetLocalKeyword(shader, ShaderKeywordStrings.LightmapShadowMixing);
            m_ShadowsShadowMask = TryGetLocalKeyword(shader, ShaderKeywordStrings.ShadowsShadowMask);
            m_Lightmap = TryGetLocalKeyword(shader, ShaderKeywordStrings.LIGHTMAP_ON);
            m_DynamicLightmap = TryGetLocalKeyword(shader, ShaderKeywordStrings.DYNAMICLIGHTMAP_ON);
            m_DirectionalLightmap = TryGetLocalKeyword(shader, ShaderKeywordStrings.DIRLIGHTMAP_COMBINED);
            m_AlphaTestOn = TryGetLocalKeyword(shader, ShaderKeywordStrings._ALPHATEST_ON);
            m_GbufferNormalsOct = TryGetLocalKeyword(shader, ShaderKeywordStrings._GBUFFER_NORMALS_OCT);
            m_ScreenSpaceOcclusion = TryGetLocalKeyword(shader, ShaderKeywordStrings.ScreenSpaceOcclusion);
            m_UseFastSRGBLinearConversion = TryGetLocalKeyword(shader, ShaderKeywordStrings.UseFastSRGBLinearConversion);
            m_LightLayers = TryGetLocalKeyword(shader, ShaderKeywordStrings.LightLayers);
            m_DecalLayers = TryGetLocalKeyword(shader, ShaderKeywordStrings.DecalLayers);
            m_WriteRenderingLayers = TryGetLocalKeyword(shader, ShaderKeywordStrings.WriteRenderingLayers);
            m_RenderPassEnabled = TryGetLocalKeyword(shader, ShaderKeywordStrings.RenderPassEnabled);
            m_DebugDisplay = TryGetLocalKeyword(shader, ShaderKeywordStrings.DEBUG_DISPLAY);
            m_DBufferMRT1 = TryGetLocalKeyword(shader, ShaderKeywordStrings.DBufferMRT1);
            m_DBufferMRT2 = TryGetLocalKeyword(shader, ShaderKeywordStrings.DBufferMRT2);
            m_DBufferMRT3 = TryGetLocalKeyword(shader, ShaderKeywordStrings.DBufferMRT3);
            m_DecalNormalBlendLow = TryGetLocalKeyword(shader, ShaderKeywordStrings.DecalNormalBlendLow);
            m_DecalNormalBlendMedium = TryGetLocalKeyword(shader, ShaderKeywordStrings.DecalNormalBlendMedium);
            m_DecalNormalBlendHigh = TryGetLocalKeyword(shader, ShaderKeywordStrings.DecalNormalBlendHigh);
            m_ForwardPlus = TryGetLocalKeyword(shader, ShaderKeywordStrings.ForwardPlus);
            m_FoveatedRenderingNonUniformRaster = TryGetLocalKeyword(shader, ShaderKeywordStrings.FoveatedRenderingNonUniformRaster);
            m_EditorVisualization = TryGetLocalKeyword(shader, ShaderKeywordStrings.EDITOR_VISUALIZATION);
            m_LODFadeCrossFade = TryGetLocalKeyword(shader, ShaderKeywordStrings.LOD_FADE_CROSSFADE);
            m_LightCookies = TryGetLocalKeyword(shader, ShaderKeywordStrings.LightCookies);

            m_ScreenCoordOverride = TryGetLocalKeyword(shader, ShaderKeywordStrings.SCREEN_COORD_OVERRIDE);
            m_ProbeVolumesL1 = TryGetLocalKeyword(shader, ShaderKeywordStrings.ProbeVolumeL1);
            m_ProbeVolumesL2 = TryGetLocalKeyword(shader, ShaderKeywordStrings.ProbeVolumeL2);

            // Post processing
            m_LensDistortion = TryGetLocalKeyword(shader, ShaderKeywordStrings.Distortion);
            m_ChromaticAberration = TryGetLocalKeyword(shader, ShaderKeywordStrings.ChromaticAberration);
            m_BloomLQ = TryGetLocalKeyword(shader, ShaderKeywordStrings.BloomLQ);
            m_BloomHQ = TryGetLocalKeyword(shader, ShaderKeywordStrings.BloomHQ);
            m_BloomLQDirt = TryGetLocalKeyword(shader, ShaderKeywordStrings.BloomLQDirt);
            m_BloomHQDirt = TryGetLocalKeyword(shader, ShaderKeywordStrings.BloomHQDirt);
            m_HdrGrading = TryGetLocalKeyword(shader, ShaderKeywordStrings.HDRGrading);
            m_ToneMapACES = TryGetLocalKeyword(shader, ShaderKeywordStrings.TonemapACES);
            m_ToneMapNeutral = TryGetLocalKeyword(shader, ShaderKeywordStrings.TonemapNeutral);
            m_FilmGrain = TryGetLocalKeyword(shader, ShaderKeywordStrings.FilmGrain);
        }



        /*********************************************************
                            Volume Features
        *********************************************************/

        internal bool StripVolumeFeatures_UberPostShader(ref IShaderScriptableStrippingData strippingData)
        {
            if (strippingData.shader != m_UberPostShader)
                return false;

            ShaderStripTool<VolumeFeatures> stripTool = new ShaderStripTool<VolumeFeatures>(strippingData.volumeFeatures, ref strippingData);
            if (stripTool.StripMultiCompileKeepOffVariant(m_LensDistortion, VolumeFeatures.LensDistortion))
                return true;

            if (stripTool.StripMultiCompileKeepOffVariant(m_ChromaticAberration, VolumeFeatures.ChromaticAberration))
                return true;

            if (stripTool.StripMultiCompileKeepOffVariant(m_BloomLQ, VolumeFeatures.Bloom))
                return true;

            if (stripTool.StripMultiCompileKeepOffVariant(m_BloomHQ, VolumeFeatures.Bloom))
                return true;

            if (stripTool.StripMultiCompileKeepOffVariant(m_BloomLQDirt, VolumeFeatures.Bloom))
                return true;

            if (stripTool.StripMultiCompileKeepOffVariant(m_BloomHQDirt, VolumeFeatures.Bloom))
                return true;

            if (stripTool.StripMultiCompileKeepOffVariant(m_ToneMapACES, VolumeFeatures.ToneMapping))
                return true;

            if (stripTool.StripMultiCompileKeepOffVariant(m_ToneMapNeutral, VolumeFeatures.ToneMapping))
                return true;

            if (stripTool.StripMultiCompileKeepOffVariant(m_FilmGrain, VolumeFeatures.FilmGrain))
                return true;

            return false;
        }

        internal bool StripVolumeFeatures_BokehDepthOfFieldShader(ref IShaderScriptableStrippingData strippingData)
        {
            if (strippingData.shader != m_BokehDepthOfField)
                return false;

            return !strippingData.IsVolumeFeatureEnabled(VolumeFeatures.DepthOfField);
        }

        internal bool StripVolumeFeatures_GaussianDepthOfFieldShader(ref IShaderScriptableStrippingData strippingData)
        {
            if (strippingData.shader != m_GaussianDepthOfField)
                return false;

            return !strippingData.IsVolumeFeatureEnabled(VolumeFeatures.DepthOfField);
        }

        internal bool StripVolumeFeatures_CameraMotionBlurShader(ref IShaderScriptableStrippingData strippingData)
        {
            if (strippingData.shader != m_CameraMotionBlur)
                return false;

            return !strippingData.IsVolumeFeatureEnabled(VolumeFeatures.CameraMotionBlur);
        }

        internal bool StripVolumeFeatures_PaniniProjectionShader(ref IShaderScriptableStrippingData strippingData)
        {
            if (strippingData.shader != m_PaniniProjection)
                return false;

            return !strippingData.IsVolumeFeatureEnabled(VolumeFeatures.PaniniProjection);
        }

        internal bool StripVolumeFeatures_BloomShader(ref IShaderScriptableStrippingData strippingData)
        {
            if (strippingData.shader != m_Bloom)
                return false;

            return !strippingData.IsVolumeFeatureEnabled(VolumeFeatures.Bloom);
        }


        internal bool StripVolumeFeatures(VolumeFeatures features, ref IShaderScriptableStrippingData strippingData)
        {
            if (StripVolumeFeatures_UberPostShader(ref strippingData))
                return true;

            if (StripVolumeFeatures_BokehDepthOfFieldShader(ref strippingData))
                return true;

            if (StripVolumeFeatures_GaussianDepthOfFieldShader(ref strippingData))
                return true;

            if (StripVolumeFeatures_CameraMotionBlurShader(ref strippingData))
                return true;

            if (StripVolumeFeatures_PaniniProjectionShader(ref strippingData))
                return true;

            if (StripVolumeFeatures_BloomShader(ref strippingData))
                return true;

            return false;
        }

        /*********************************************************
                            Unused Variants
        *********************************************************/

        internal bool StripUnusedFeatures_DebugDisplay(ref IShaderScriptableStrippingData strippingData)
        {
            return strippingData.stripDebugDisplayShaders && strippingData.IsKeywordEnabled(m_DebugDisplay);
        }

        internal bool StripUnusedFeatures_ScreenCoordOverride(ref IShaderScriptableStrippingData strippingData)
        {
            return strippingData.stripScreenCoordOverrideVariants && strippingData.IsKeywordEnabled(m_ScreenCoordOverride);
        }

        internal bool StripUnusedFeatures_PunctualLightShadows(ref IShaderScriptableStrippingData strippingData)
        {
            // Shadow caster punctual light strip
            if (strippingData.passType == PassType.ShadowCaster && strippingData.PassHasKeyword(m_CastingPunctualLightShadow))
            {
                bool mainLightShadowsDisabled =
                    !strippingData.IsShaderFeatureEnabled(ShaderFeatures.MainLightShadows) &&
                    !strippingData.IsShaderFeatureEnabled(ShaderFeatures.MainLightShadowsCascade) &&
                    !strippingData.IsShaderFeatureEnabled(ShaderFeatures.ScreenSpaceShadows);
                if (mainLightShadowsDisabled && !strippingData.IsKeywordEnabled(m_CastingPunctualLightShadow))
                    return true;

                if (!strippingData.IsShaderFeatureEnabled(ShaderFeatures.AdditionalLightShadows) && strippingData.IsKeywordEnabled(m_CastingPunctualLightShadow))
                    return true;
            }

            return false;
        }

        internal bool StripUnusedFeatures_FoveatedRendering(ref IShaderScriptableStrippingData strippingData)
        {
            // Strip Foveated Rendering variants on all platforms (except PS5 and Metal)
            // TODO: add a way to communicate this requirement from the xr plugin directly
            #if ENABLE_VR && ENABLE_XR_MODULE
            if (strippingData.shaderCompilerPlatform != ShaderCompilerPlatform.PS5NGGC && strippingData.shaderCompilerPlatform != ShaderCompilerPlatform.Metal)
            #endif
            {
                if (strippingData.IsKeywordEnabled(m_FoveatedRenderingNonUniformRaster))
                    return true;
            }

            return false;
        }

        internal bool StripUnusedFeatures_DeferredRendering(ref IShaderScriptableStrippingData strippingData)
        {
            // TODO: Test against lightMode tag instead.
            if (strippingData.passName == kPassNameGBuffer)
            {
                if (!strippingData.IsShaderFeatureEnabled(ShaderFeatures.DeferredShading))
                    return true;
            }

            return false;
        }

        internal bool StripUnusedFeatures_MainLightShadows(ref IShaderScriptableStrippingData strippingData, ref ShaderStripTool<ShaderFeatures> stripTool)
        {
            // strip main light shadows, cascade and screen variants
            if (strippingData.IsShaderFeatureEnabled(ShaderFeatures.ShadowsKeepOffVariants))
            {
                if (stripTool.StripMultiCompileKeepOffVariant(
                        m_MainLightShadows, ShaderFeatures.MainLightShadows,
                        m_MainLightShadowsCascades, ShaderFeatures.MainLightShadowsCascade,
                        m_MainLightShadowsScreen, ShaderFeatures.ScreenSpaceShadows))
                    return true;
            }
            else
            {
                if (stripTool.StripMultiCompile(
                        m_MainLightShadows, ShaderFeatures.MainLightShadows,
                        m_MainLightShadowsCascades, ShaderFeatures.MainLightShadowsCascade,
                        m_MainLightShadowsScreen, ShaderFeatures.ScreenSpaceShadows))
                    return true;
            }

            return false;
        }

        internal bool StripUnusedFeatures_AdditionalLightShadows(ref IShaderScriptableStrippingData strippingData, ref ShaderStripTool<ShaderFeatures> stripTool)
        {
            // No additional light shadows
            if (strippingData.IsShaderFeatureEnabled(ShaderFeatures.ShadowsKeepOffVariants))
            {
                if (stripTool.StripMultiCompileKeepOffVariant(m_AdditionalLightShadows, ShaderFeatures.AdditionalLightShadows))
                    return true;
            }
            else if (stripTool.StripMultiCompile(m_AdditionalLightShadows, ShaderFeatures.AdditionalLightShadows))
                return true;

            return false;
        }

        internal bool StripUnusedFeatures_MixedLighting(ref IShaderScriptableStrippingData strippingData)
        {
            // Strip here only if mixed lighting is disabled
            if (!strippingData.IsShaderFeatureEnabled(ShaderFeatures.MixedLighting))
            {
                if (strippingData.IsKeywordEnabled(m_MixedLightingSubtractive))
                    return true;

                // No need to check here if actually used by scenes as this taken care by builtin stripper
                if (strippingData.IsKeywordEnabled(m_LightmapShadowMixing) || strippingData.IsKeywordEnabled(m_ShadowsShadowMask))
                    return true;
            }

            return false;
        }

        internal bool StripUnusedFeatures_SoftShadows(ref ShaderStripTool<ShaderFeatures> stripTool)
        {
            // TODO: Strip off variants once we have global soft shadows option for forcing instead as support
            return stripTool.StripMultiCompileKeepOffVariant(m_SoftShadows, ShaderFeatures.SoftShadows);
        }

        internal bool StripUnusedFeatures_HDRGrading(ref ShaderStripTool<ShaderFeatures> stripTool)
        {
            return stripTool.StripMultiCompileKeepOffVariant(m_HdrGrading, ShaderFeatures.HdrGrading);
        }

        internal bool StripUnusedFeatures_UseFastSRGBLinearConversion(ref ShaderStripTool<ShaderFeatures> stripTool)
        {
            return stripTool.StripMultiCompile(m_UseFastSRGBLinearConversion, ShaderFeatures.UseFastSRGBLinearConversion);
        }

        internal bool StripUnusedFeatures_LightLayers(ref ShaderStripTool<ShaderFeatures> stripTool)
        {
            return stripTool.StripMultiCompile(m_LightLayers, ShaderFeatures.LightLayers);
        }

        internal bool StripUnusedFeatures_RenderPassEnabled(ref ShaderStripTool<ShaderFeatures> stripTool)
        {
            return stripTool.StripMultiCompileKeepOffVariant(m_RenderPassEnabled, ShaderFeatures.RenderPassEnabled);
        }

        internal bool StripUnusedFeatures_ReflectionProbes(ref ShaderStripTool<ShaderFeatures> stripTool)
        {
            // Reflection probes
            if (stripTool.StripMultiCompile(m_ReflectionProbeBlending, ShaderFeatures.ReflectionProbeBlending))
                return true;

            if (stripTool.StripMultiCompile(m_ReflectionProbeBoxProjection, ShaderFeatures.ReflectionProbeBoxProjection))
                return true;

            return false;
        }

        internal bool StripUnusedFeatures_ForwardPlus(ref ShaderStripTool<ShaderFeatures> stripTool)
        {
            return stripTool.StripMultiCompile(m_ForwardPlus, ShaderFeatures.ForwardPlus);
        }

        internal bool StripUnusedFeatures_AdditionalLights(ref IShaderScriptableStrippingData strippingData, ref ShaderStripTool<ShaderFeatures> stripTool)
        {
            // Forward Plus doesn't use Vertex or the Pixel Light variants.
            // It enables the Pixel keyword through a define.
            if (strippingData.IsShaderFeatureEnabled(ShaderFeatures.ForwardPlus))
            {
                if (strippingData.IsShaderFeatureEnabled(ShaderFeatures.AdditionalLightsVertex))
                    return true;

                if (strippingData.IsShaderFeatureEnabled(ShaderFeatures.AdditionalLightsPixel))
                    return true;
            }

            // Additional light are shaded per-vertex or per-pixel.
            if (strippingData.IsShaderFeatureEnabled(ShaderFeatures.AdditionalLightsKeepOffVariants))
            {
                if (stripTool.StripMultiCompileKeepOffVariant(m_AdditionalLightsVertex, ShaderFeatures.AdditionalLightsVertex, m_AdditionalLightsPixel, ShaderFeatures.AdditionalLightsPixel))
                    return true;
            }
            else
            {
                if (stripTool.StripMultiCompile(m_AdditionalLightsVertex, ShaderFeatures.AdditionalLightsVertex, m_AdditionalLightsPixel, ShaderFeatures.AdditionalLightsPixel))
                    return true;
            }
            return false;
        }

        internal bool StripUnusedFeatures_ScreenSpaceOcclusion(ref IShaderScriptableStrippingData strippingData, ref ShaderStripTool<ShaderFeatures> stripTool)
        {
            // Screen Space Occlusion
            if (strippingData.IsShaderFeatureEnabled(ShaderFeatures.ScreenSpaceOcclusionAfterOpaque))
            {
                // SSAO after opaque setting requires off variants
                if (stripTool.StripMultiCompileKeepOffVariant(m_ScreenSpaceOcclusion, ShaderFeatures.ScreenSpaceOcclusion))
                    return true;
            }
            else
            {
                if (stripTool.StripMultiCompile(m_ScreenSpaceOcclusion, ShaderFeatures.ScreenSpaceOcclusion))
                    return true;
            }
            return false;
        }

        internal bool StripUnusedFeatures_DecalsDbuffer(ref IShaderScriptableStrippingData strippingData, ref ShaderStripTool<ShaderFeatures> stripTool)
        {
            // DBuffer
            if (strippingData.isGLDevice)
            {
                // Decal DBuffer is not supported on gl
                if (strippingData.IsKeywordEnabled(m_DBufferMRT1) ||
                    strippingData.IsKeywordEnabled(m_DBufferMRT2) ||
                    strippingData.IsKeywordEnabled(m_DBufferMRT3))
                    return true;
            }
            else
            {
                if (stripTool.StripMultiCompile(
                        m_DBufferMRT1, ShaderFeatures.DBufferMRT1,
                        m_DBufferMRT2, ShaderFeatures.DBufferMRT2,
                        m_DBufferMRT3, ShaderFeatures.DBufferMRT3))
                    return true;
            }

            return false;
        }

        internal bool StripUnusedFeatures_DecalsNormalBlend(ref IShaderScriptableStrippingData strippingData, ref ShaderStripTool<ShaderFeatures> stripTool)
        {
            // Decal Normal Blend
            if (stripTool.StripMultiCompile(
                    m_DecalNormalBlendLow, ShaderFeatures.DecalNormalBlendLow,
                    m_DecalNormalBlendMedium, ShaderFeatures.DecalNormalBlendMedium,
                    m_DecalNormalBlendHigh, ShaderFeatures.DecalNormalBlendHigh))
                return true;

            return false;
        }

        internal bool StripUnusedFeatures_DecalLayers(ref IShaderScriptableStrippingData strippingData, ref ShaderStripTool<ShaderFeatures> stripTool)
        {
            // Rendering layers are not supported on gl
            if (strippingData.isGLDevice)
            {
                if (strippingData.IsKeywordEnabled(m_DecalLayers))
                    return true;
            }
            else if (stripTool.StripMultiCompile(m_DecalLayers, ShaderFeatures.DecalLayers))
                return true;

            return false;
        }

        internal bool StripUnusedFeatures_WriteRenderingLayers(ref IShaderScriptableStrippingData strippingData, ref ShaderStripTool<ShaderFeatures> stripTool)
        {
            // Rendering layers are not supported on gl
            if (strippingData.isGLDevice)
            {
                if (strippingData.IsKeywordEnabled(m_WriteRenderingLayers))
                    return true;
            }
            else
            {
                if (strippingData.passName == kPassNameDepthNormals)
                {
                    if (stripTool.StripMultiCompile(m_WriteRenderingLayers, ShaderFeatures.DepthNormalPassRenderingLayers))
                        return true;
                }
                if (strippingData.passName == kPassNameForwardLit)
                {
                    if (stripTool.StripMultiCompile(m_WriteRenderingLayers, ShaderFeatures.OpaqueWriteRenderingLayers))
                        return true;
                }
                if (strippingData.passName == kPassNameGBuffer)
                {
                    if (stripTool.StripMultiCompile(m_WriteRenderingLayers, ShaderFeatures.GBufferWriteRenderingLayers))
                        return true;
                }
            }
            return false;
        }

        internal bool StripUnusedFeatures_AccurateGbufferNormals(ref IShaderScriptableStrippingData strippingData, ref ShaderStripTool<ShaderFeatures> stripTool)
        {
            // Do not strip accurateGbufferNormals on Mobile Vulkan as some GPUs do not support R8G8B8A8_SNorm,
            // which then force us to use accurateGbufferNormals
            if (strippingData.shaderCompilerPlatform != ShaderCompilerPlatform.Vulkan)
                if (stripTool.StripMultiCompile(m_GbufferNormalsOct, ShaderFeatures.AccurateGbufferNormals))
                    return true;

            return false;
        }

        internal bool StripUnusedFeatures_LightCookies(ref IShaderScriptableStrippingData strippingData, ref ShaderStripTool<ShaderFeatures> stripTool)
        {
            return stripTool.StripMultiCompileKeepOffVariant(m_LightCookies, ShaderFeatures.LightCookies);
        }

        internal bool StripUnusedFeatures_ProbesVolumes(ref ShaderStripTool<ShaderFeatures> stripTool)
        {
            return stripTool.StripMultiCompileKeepOffVariant(m_ProbeVolumesL1, ShaderFeatures.ProbeVolumeL1, m_ProbeVolumesL2, ShaderFeatures.ProbeVolumeL2);
        }

        internal bool StripUnusedFeatures(ref IShaderScriptableStrippingData strippingData)
        {
            if (StripUnusedFeatures_DebugDisplay(ref strippingData))
                return true;

            if (StripUnusedFeatures_ScreenCoordOverride(ref strippingData))
                return true;

            if (StripUnusedFeatures_MixedLighting(ref strippingData))
                return true;

            if (StripUnusedFeatures_PunctualLightShadows(ref strippingData))
                return true;

            if (StripUnusedFeatures_FoveatedRendering(ref strippingData))
                return true;

            if (StripUnusedFeatures_DeferredRendering(ref strippingData))
                return true;

            ShaderStripTool<ShaderFeatures> stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);

            if (StripUnusedFeatures_MainLightShadows(ref strippingData, ref stripTool))
                return true;

            if (StripUnusedFeatures_AdditionalLightShadows(ref strippingData, ref stripTool))
                return true;

            if (StripUnusedFeatures_SoftShadows(ref stripTool))
                return true;

            if (StripUnusedFeatures_HDRGrading(ref stripTool))
                return true;

            if (StripUnusedFeatures_UseFastSRGBLinearConversion(ref stripTool))
                return true;

            if (StripUnusedFeatures_LightLayers(ref stripTool))
                return true;

            if (StripUnusedFeatures_RenderPassEnabled(ref stripTool))
                return true;

            if (StripUnusedFeatures_ReflectionProbes(ref stripTool))
                return true;

            if (StripUnusedFeatures_ForwardPlus(ref stripTool))
                return true;

            if (StripUnusedFeatures_AdditionalLights(ref strippingData, ref stripTool))
                return true;

            if (StripUnusedFeatures_ScreenSpaceOcclusion(ref strippingData, ref stripTool))
                return true;

            if (StripUnusedFeatures_DecalsDbuffer(ref strippingData, ref stripTool))
                return true;

            if (StripUnusedFeatures_DecalsNormalBlend(ref strippingData, ref stripTool))
                return true;

            if (StripUnusedFeatures_DecalLayers(ref strippingData, ref stripTool))
                return true;

            if (StripUnusedFeatures_WriteRenderingLayers(ref strippingData, ref stripTool))
                return true;

            if (StripUnusedFeatures_AccurateGbufferNormals(ref strippingData, ref stripTool))
                return true;

            if (stripTool.StripMultiCompileKeepOffVariant(m_LODFadeCrossFade, ShaderFeatures.LODCrossFade))
                return true;

            if (StripUnusedFeatures_LightCookies(ref strippingData, ref stripTool))
                return true;

            if (StripUnusedFeatures_ProbesVolumes(ref stripTool))
                return true;

            return false;
        }



        /*********************************************************
                            Unsupported Variants
        *********************************************************/

        internal bool StripUnsupportedVariants_DirectionalLightmap(ref IShaderScriptableStrippingData strippingData)
        {
            // We can strip variants that have directional lightmap enabled but not static nor dynamic lightmap.
            if (strippingData.IsKeywordEnabled(m_DirectionalLightmap)
                && !(strippingData.IsKeywordEnabled(m_Lightmap) || strippingData.IsKeywordEnabled(m_DynamicLightmap)))
                return true;

            return false;
        }

        internal bool StripUnsupportedVariants_LightmapProbes(ref IShaderScriptableStrippingData strippingData)
        {
            // We can strip shaders where both lightmaps and probe volumes are enabled
            if ((strippingData.IsKeywordEnabled(m_Lightmap) || strippingData.IsKeywordEnabled(m_DynamicLightmap))
                && (strippingData.IsKeywordEnabled(m_ProbeVolumesL1) || strippingData.IsKeywordEnabled(m_ProbeVolumesL2)))
                return true;

            return false;
        }

        internal bool StripUnsupportedVariants_EditorVisualization(ref IShaderScriptableStrippingData strippingData)
        {
            // Editor visualization is only used in scene view debug modes.
            if (strippingData.IsKeywordEnabled(m_EditorVisualization))
                return true;

            return false;
        }

        internal bool StripUnsupportedVariants(ref IShaderScriptableStrippingData strippingData)
        {
            // We can strip variants that have directional lightmap enabled but not static nor dynamic lightmap.
            if (StripUnsupportedVariants_DirectionalLightmap(ref strippingData))
                return true;

            // We can strip shaders where both lightmaps and probe volumes are enabled
            if (StripUnsupportedVariants_LightmapProbes(ref strippingData))
                return true;

            if (StripUnsupportedVariants_EditorVisualization(ref strippingData))
                return true;

            return false;
        }

        /*********************************************************
                            Invalid Variants
        *********************************************************/

        internal bool StripInvalidVariants_HDR(ref IShaderScriptableStrippingData strippingData)
        {
            return !strippingData.IsHDRShaderVariantValid;
        }

        internal bool StripInvalidVariants_TerrainHoles(ref IShaderScriptableStrippingData strippingData)
        {
            if (strippingData.shader == m_TerrainLit)
                if (!strippingData.IsShaderFeatureEnabled(ShaderFeatures.TerrainHoles) && strippingData.IsKeywordEnabled(m_AlphaTestOn))
                    return true;
            return false;
        }

        internal bool StripInvalidVariants_Shadows(ref IShaderScriptableStrippingData strippingData)
        {
            // Strip Additional Shadow variants if it's not set to PerPixel and not F+/Deferred
            bool areAdditionalShadowsEnabled = strippingData.IsKeywordEnabled(m_AdditionalLightShadows);
            bool hasShadowsOff = strippingData.IsShaderFeatureEnabled(ShaderFeatures.ShadowsKeepOffVariants);
            if (hasShadowsOff && areAdditionalShadowsEnabled)
            {
                bool isPerPixel    = strippingData.IsKeywordEnabled(m_AdditionalLightsPixel);
                bool isForwardPlus = strippingData.IsKeywordEnabled(m_ForwardPlus);
                bool isDeferred    = strippingData.IsShaderFeatureEnabled(ShaderFeatures.DeferredShading);
                if (!isPerPixel && !isForwardPlus && !isDeferred)
                    return true;
            }

            // Strip Soft Shadows if shadows are disabled for both Main and Additional Lights...
            bool isMainShadowNoCascades = strippingData.IsKeywordEnabled(m_MainLightShadows);
            bool isMainShadowCascades = strippingData.IsKeywordEnabled(m_MainLightShadowsCascades);
            bool isMainShadowScreen = strippingData.IsKeywordEnabled(m_MainLightShadowsScreen);
            bool isMainShadow = isMainShadowNoCascades || isMainShadowCascades || isMainShadowScreen;
            bool isShadowVariant = isMainShadow || areAdditionalShadowsEnabled;
            if (!isShadowVariant && strippingData.IsKeywordEnabled(m_SoftShadows))
                return true;

            return false;
        }

        internal bool StripInvalidVariants(ref IShaderScriptableStrippingData strippingData)
        {
            if (StripInvalidVariants_HDR(ref strippingData))
                return true;

            if (StripInvalidVariants_TerrainHoles(ref strippingData))
                return true;

            if (StripInvalidVariants_Shadows(ref strippingData))
                return true;

            return false;
        }

        /*********************************************************
                            Unused Passes
        *********************************************************/

        internal bool StripUnusedPass_2D(ref IShaderScriptableStrippingData strippingData)
        {
            // Strip 2D Passes if there are no 2D renderers...
            if (strippingData.passName == kPassNameUniversal2D && strippingData.strip2DPasses)
                return true;
            return false;
        }

        internal bool StripUnusedPass_Meta(ref IShaderScriptableStrippingData strippingData)
        {
            // Meta pass is needed in the player for Enlighten Precomputed Realtime GI albedo and emission.
            if (strippingData.passType == PassType.Meta)
            {
                if (SupportedRenderingFeatures.active.enlighten == false
                    || ((int)SupportedRenderingFeatures.active.lightmapBakeTypes | (int)LightmapBakeType.Realtime) == 0)
                    return true;
            }
            return false;
        }

        internal bool StripUnusedPass_ShadowCaster(ref IShaderScriptableStrippingData strippingData)
        {
            if (strippingData.passType == PassType.ShadowCaster)
            {
                if (   !strippingData.IsShaderFeatureEnabled(ShaderFeatures.MainLightShadows)
                    && !strippingData.IsShaderFeatureEnabled(ShaderFeatures.AdditionalLightShadows))
                    return true;
            }
            return false;
        }

        internal bool StripUnusedPass_Decals(ref IShaderScriptableStrippingData strippingData)
        {
            // Do not strip GL passes as there are only screen space forward
            if (strippingData.isGLDevice)
                return false;

            // DBuffer
            if (strippingData.passName == DecalShaderPassNames.DBufferMesh
                || strippingData.passName == DecalShaderPassNames.DBufferProjector
                || strippingData.passName == DecalShaderPassNames.DecalMeshForwardEmissive
                || strippingData.passName == DecalShaderPassNames.DecalProjectorForwardEmissive)
                if (!strippingData.IsShaderFeatureEnabled(ShaderFeatures.DBufferMRT1) && !strippingData.IsShaderFeatureEnabled(ShaderFeatures.DBufferMRT2) && !strippingData.IsShaderFeatureEnabled(ShaderFeatures.DBufferMRT3))
                    return true;

            // Decal Screen Space
            if (strippingData.passName == DecalShaderPassNames.DecalScreenSpaceMesh || strippingData.passName == DecalShaderPassNames.DecalScreenSpaceProjector)
                if (!strippingData.IsShaderFeatureEnabled(ShaderFeatures.DecalScreenSpace))
                    return true;

            // Decal GBuffer
            if (strippingData.passName == DecalShaderPassNames.DecalGBufferMesh || strippingData.passName == DecalShaderPassNames.DecalGBufferProjector)
                if (!strippingData.IsShaderFeatureEnabled(ShaderFeatures.DecalGBuffer))
                    return true;

            return false;
        }

        internal bool StripUnusedPass(ref IShaderScriptableStrippingData strippingData)
        {
            if (StripUnusedPass_2D(ref strippingData))
                return true;

            if (StripUnusedPass_Meta(ref strippingData))
                return true;

            if (StripUnusedPass_ShadowCaster(ref strippingData))
                return true;

            if (StripUnusedPass_Decals(ref strippingData))
                return true;

            return false;
        }

        /*********************************************************
                            Unused Shaders
        *********************************************************/

        internal bool StripUnusedShaders_Deferred(ref IShaderScriptableStrippingData strippingData)
        {
            if (!strippingData.stripUnusedVariants)
                return false;

            // Remove DeferredStencil if Deferred Rendering is not used
            if (strippingData.shader == m_StencilDeferred)
                if (!strippingData.IsShaderFeatureEnabled(ShaderFeatures.DeferredShading))
                    return true;

            return false;
        }

        internal bool StripUnusedShaders(ref IShaderScriptableStrippingData strippingData)
        {
            if (!strippingData.stripUnusedVariants)
                return false;

            // Remove DeferredStencil if Deferred Rendering is not used
            if (StripUnusedShaders_Deferred(ref strippingData))
                return true;

            return false;
        }


        /*********************************************************
                            Main Callbacks
        *********************************************************/

        public bool CanRemoveVariant([DisallowNull] Shader shader, ShaderSnippetData passData, ShaderCompilerData variantData)
        {
            IShaderScriptableStrippingData strippingData = new StrippingData()
            {
                volumeFeatures = ShaderBuildPreprocessor.volumeFeatures,
                strip2DPasses = ShaderBuildPreprocessor.s_Strip2DPasses,
                stripDebugDisplayShaders = ShaderBuildPreprocessor.s_StripDebugDisplayShaders,
                stripScreenCoordOverrideVariants = ShaderBuildPreprocessor.s_StripScreenCoordOverrideVariants,
                stripUnusedVariants = ShaderBuildPreprocessor.s_StripUnusedVariants,
                stripUnusedPostProcessingVariants = ShaderBuildPreprocessor.s_StripUnusedPostProcessingVariants,
                shader = shader,
                passData = passData,
                variantData = variantData
            };

            // All feature sets need to have this variant unused to be stripped out.
            bool removeInput = true;
            for (var index = 0; index < ShaderBuildPreprocessor.supportedFeaturesList.Count; index++)
            {
                strippingData.shaderFeatures = ShaderBuildPreprocessor.supportedFeaturesList[index];

                if (StripUnusedShaders(ref strippingData))
                    continue;

                if (StripUnusedPass(ref strippingData))
                    continue;

                if (StripInvalidVariants(ref strippingData))
                    continue;

                if (StripUnsupportedVariants(ref strippingData))
                    continue;

                if (StripUnusedFeatures(ref strippingData))
                    continue;

                removeInput = false;
                break;
            }

            // Check PostProcessing variants...
            if (!removeInput && strippingData.stripUnusedPostProcessingVariants)
                if (StripVolumeFeatures(ShaderBuildPreprocessor.volumeFeatures, ref strippingData))
                    removeInput = true;

            return removeInput;
        }

        public void BeforeShaderStripping(Shader shader)
        {
            if (shader != null)
                InitializeLocalShaderKeywords(shader);
        }

        public void AfterShaderStripping(Shader shader)
        {

        }
    }
}
