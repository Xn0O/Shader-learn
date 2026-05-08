Shader "Custom/Grass"
{
    Properties
    {
        [NoScaleOffset] _MainTex("Grass Texture", 2D) = "white" {}
        [NoScaleOffset] _MapTex("Control Map", 2D) = "white" {}
        _MapSize("Map Size", Float) = 0
        _MapPos("Map Position", Vector) = (0, 0, 0, 0)
        _WindDir("Wind Direction", Vector) = (1, 0, 0, 0)
        _WindPow("Wind Power", Float) = 0
        _WindSpeed("Wind Speed", Float) = 0
        _PushPow("Push Power", Float) = 0
        _ShadowColor("Shadow Color", Color) = (0.3, 0.4, 0.5, 1)
        _ShadowThreshold("Shadow Threshold", Range(0, 1)) = 0.5
        [HideInInspector] _QueueOffset("_QueueOffset", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "QUEUE" = "AlphaTest"
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off

        Pass
        {
            Name "Universal Forward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 texcoord : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float4 shadowCoord : TEXCOORD2;
                float4 positionCS : SV_POSITION;
            };

            TEXTURE2D(_MainTex);       SAMPLER(sampler_MainTex);
            TEXTURE2D(_MapTex);        SAMPLER(sampler_MapTex);

            CBUFFER_START(UnityPerMaterial)
            float _MapSize;
            float2 _MapPos;
            float2 _WindDir;
            float _WindPow;
            float _WindSpeed;
            float _PushPow;
            half4 _ShadowColor;
            float _ShadowThreshold;
            CBUFFER_END

            float3 _PlayerPos;

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.546875);
            }

            float smoothNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(hash(i + float2(0, 0)), hash(i + float2(1, 0)), u.x),
                    lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), u.x),
                    u.y
                );
            }

            Varyings vert(Attributes input)
            {
                Varyings o;

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);

                // Wind displacement
                float2 windOffset = _WindDir * (_TimeParameters.x * _WindSpeed);
                float2 noiseUV = worldPos.xz - windOffset;

                float noise = smoothNoise(noiseUV) * 0.5
                            + smoothNoise(noiseUV * 0.5) * 0.25
                            + smoothNoise(noiseUV * 0.25) * 0.125;

                float windMag = _WindPow * input.color.r;
                float2 windDirNorm = normalize(_WindDir);
                float3 windDisp = float3(
                    noise * windMag * windDirNorm.x,
                    noise * windMag * 0.2,
                    noise * windMag * windDirNorm.y
                );
                worldPos += windDisp;

                // Player push displacement
                float3 pushVec = worldPos - _PlayerPos;
                float pushDist = length(pushVec);
                float pushStr = max(0, _PushPow - pushDist);
                pushStr = min(pushStr, _PushPow);

                float3 pushDir = normalize(pushVec);
                float3 pushDisp = float3(
                    pushStr * pushDir.x * input.color.r,
                    -0.2 * pushStr * pushStr * input.color.g,
                    pushStr * pushDir.z * input.color.b
                );
                worldPos += pushDisp;

                o.positionCS = TransformWorldToHClip(worldPos);
                o.uv = input.texcoord.xy;
                o.worldPos = worldPos;
                o.shadowCoord = TransformWorldToShadowCoord(worldPos);

                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(mainTex.a - 0.5);

                float2 mapUV = (input.worldPos.xz / max(_MapSize, 0.001)) + _MapPos;
                half3 albedo = SAMPLE_TEXTURE2D(_MapTex, sampler_MapTex, mapUV).rgb;

                // Main light shadow
#if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                Light mainLight = GetMainLight(input.shadowCoord);
                float shadow = mainLight.shadowAttenuation;
#else
                float shadow = 1.0;
#endif

                // Soft shadow transition
                float toonLight = smoothstep(_ShadowThreshold - 0.3, _ShadowThreshold + 0.3, shadow);
                half3 finalColor = lerp(_ShadowColor.rgb, albedo, toonLight);

                return half4(finalColor, mainTex.a);
            }
            ENDHLSL
        }
    }
}
