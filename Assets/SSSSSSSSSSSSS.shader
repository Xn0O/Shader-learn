Shader "Custom/Grass"
{
    Properties
    {
        [NoScaleOffset] _MapTex("Control Map", 2D) = "white" {}
        _MapSize("Map Size", Float) = 0
        _MapPos("Map Position", Vector) = (0, 0, 0, 0)
        [NoScaleOffset] _MainTex("Grass Texture", 2D) = "white" {}
        _WindDir("Wind Direction", Vector) = (1, 0, 0, 0)
        _WindPow("Wind Power", Float) = 0
        _WindSpeed("Wind Speed", Float) = 0
        _PushPow("Push Power", Float) = 0
        [HideInInspector] _QueueOffset("_QueueOffset", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "QUEUE" = "AlphaTest"
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Unlit"
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float4 texcoord : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float4 uv : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
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
                float3 worldTangent = TransformObjectToWorldDir(input.tangentOS.xyz);

                // Wind displacement
                float2 windOffset = _WindDir * (_TimeParameters.x * _WindSpeed);
                float2 noiseUV = worldPos.xz * 0.5 - windOffset;

                float noise = smoothNoise(noiseUV) * 0.125
                            + smoothNoise(noiseUV * 0.5) * 0.25
                            + smoothNoise(noiseUV * 0.25) * 0.5;

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
                o.worldNormal = normalize(worldTangent);
                o.uv = input.texcoord;
                o.worldPos = worldPos;

                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 mapUV = (input.worldPos.xz / max(_MapSize, 0.001)) + _MapPos;
                half4 mapTex = SAMPLE_TEXTURE2D(_MapTex, sampler_MapTex, mapUV);
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv.xy);

                clip(mainTex.a - 0.5);
                return half4(mapTex.rgb, mainTex.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 texcoord : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);       SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float _MapSize;
            float2 _MapPos;
            float2 _WindDir;
            float _WindPow;
            float _WindSpeed;
            float _PushPow;
            CBUFFER_END

            float3 _PlayerPos;
            float3 _LightDirection;

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.546875);
            }

            Varyings vert(Attributes input)
            {
                Varyings o;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);

                float2 windOffset = _WindDir * (_TimeParameters.x * _WindSpeed);
                float2 noiseUV = worldPos.xz * 0.5 - windOffset;

                float2 i = floor(noiseUV);
                float2 f = frac(noiseUV);
                float2 u = f * f * (3.0 - 2.0 * f);
                float noise = lerp(
                    lerp(hash(i + float2(0, 0)), hash(i + float2(1, 0)), u.x),
                    lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), u.x),
                    u.y
                );

                float windMag = _WindPow * input.color.r;
                float2 windDirNorm = normalize(_WindDir);
                float3 windDisp = float3(
                    noise * windMag * windDirNorm.x,
                    noise * windMag * 0.2,
                    noise * windMag * windDirNorm.y
                );
                worldPos += windDisp;

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
                o.uv = input.texcoord;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv.xy);
                clip(mainTex.a - 0.5);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 texcoord : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);       SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float _MapSize;
            float2 _MapPos;
            float2 _WindDir;
            float _WindPow;
            float _WindSpeed;
            float _PushPow;
            CBUFFER_END

            float3 _PlayerPos;

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.546875);
            }

            Varyings vert(Attributes input)
            {
                Varyings o;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);

                float2 windOffset = _WindDir * (_TimeParameters.x * _WindSpeed);
                float2 noiseUV = worldPos.xz * 0.5 - windOffset;

                float2 i = floor(noiseUV);
                float2 f = frac(noiseUV);
                float2 u = f * f * (3.0 - 2.0 * f);
                float noise = lerp(
                    lerp(hash(i + float2(0, 0)), hash(i + float2(1, 0)), u.x),
                    lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), u.x),
                    u.y
                );

                float windMag = _WindPow * input.color.r;
                float2 windDirNorm = normalize(_WindDir);
                float3 windDisp = float3(
                    noise * windMag * windDirNorm.x,
                    noise * windMag * 0.2,
                    noise * windMag * windDirNorm.y
                );
                worldPos += windDisp;

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
                o.uv = input.texcoord;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv.xy);
                clip(mainTex.a - 0.5);
                return 0;
            }
            ENDHLSL
        }
    }
}
