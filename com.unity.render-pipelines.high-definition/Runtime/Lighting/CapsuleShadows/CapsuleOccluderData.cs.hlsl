//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef CAPSULEOCCLUDERDATA_CS_HLSL
#define CAPSULEOCCLUDERDATA_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.CapsuleShadowMethod:  static fields
//
#define CAPSULESHADOWMETHOD_FLATTEN_THEN_CLOSEST_SPHERE (0)
#define CAPSULESHADOWMETHOD_CLOSEST_SPHERE (1)
#define CAPSULESHADOWMETHOD_ELLIPSOID (2)

//
// UnityEngine.Rendering.HighDefinition.CapsuleIndirectShadowMethod:  static fields
//
#define CAPSULEINDIRECTSHADOWMETHOD_AMBIENT_OCCLUSION (0)
#define CAPSULEINDIRECTSHADOWMETHOD_DIRECTION_AT_SURFACE (1)
#define CAPSULEINDIRECTSHADOWMETHOD_DIRECTION_AT_CAPSULE (2)

//
// UnityEngine.Rendering.HighDefinition.CapsuleAmbientOcclusionMethod:  static fields
//
#define CAPSULEAMBIENTOCCLUSIONMETHOD_CLOSEST_SPHERE (0)
#define CAPSULEAMBIENTOCCLUSIONMETHOD_LINE_AND_CLOSEST_SPHERE (1)

//
// UnityEngine.Rendering.HighDefinition.CapsuleIndirectShadowFlags:  static fields
//
#define CAPSULEINDIRECTSHADOWFLAGS_COUNT_MASK (65535)
#define CAPSULEINDIRECTSHADOWFLAGS_METHOD_SHIFT (16)
#define CAPSULEINDIRECTSHADOWFLAGS_METHOD_MASK (983040)
#define CAPSULEINDIRECTSHADOWFLAGS_EXTRA_SHIFT (20)
#define CAPSULEINDIRECTSHADOWFLAGS_EXTRA_MASK (15728640)
#define CAPSULEINDIRECTSHADOWFLAGS_LIGHT_LOOP_BIT (16777216)
#define CAPSULEINDIRECTSHADOWFLAGS_HALF_RES_BIT (33554432)

// Generated from UnityEngine.Rendering.HighDefinition.CapsuleOccluderData
// PackingRules = Exact
struct CapsuleOccluderData
{
    float3 centerRWS;
    float radius;
    float3 axisDirWS;
    float offset;
    float3 indirectDirWS;
    uint lightLayers;
};


#endif
