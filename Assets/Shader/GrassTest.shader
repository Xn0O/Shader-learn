// Shader created with Shader Forge v1.40 
// Shader Forge (c) Freya Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.40;sub:START;pass:START;ps:flbk:,iptp:0,cusa:False,bamd:0,cgin:,cpap:True,lico:1,lgpr:1,limd:0,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:False,hqlp:False,rprd:False,enco:False,rmgx:True,imps:True,rpth:0,vtps:0,hqsc:True,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,olmd:1,culm:0,bsrc:0,bdst:1,dpts:2,wrdp:True,dith:0,atcv:False,rfrpo:True,rfrpn:Refraction,coma:15,ufog:False,aust:True,igpj:False,qofs:0,qpre:1,rntp:1,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,stcl:False,atwp:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:False,fnsp:False,fnfb:False,fsmp:False;n:type:ShaderForge.SFN_Final,id:3138,x:33386,y:32718,varname:node_3138,prsc:2|normal-4565-RGB;n:type:ShaderForge.SFN_Transform,id:2125,x:32142,y:32772,varname:node_2125,prsc:2,tffrom:1,tfto:0|IN-4485-OUT;n:type:ShaderForge.SFN_ComponentMask,id:421,x:32310,y:32772,varname:node_421,prsc:2,cc1:0,cc2:2,cc3:-1,cc4:-1|IN-2125-XYZ;n:type:ShaderForge.SFN_Append,id:1993,x:32479,y:32792,varname:node_1993,prsc:2|A-421-R,B-421-G;n:type:ShaderForge.SFN_Divide,id:4822,x:32680,y:32840,varname:node_4822,prsc:2|A-1993-OUT,B-7982-OUT;n:type:ShaderForge.SFN_Vector3,id:4485,x:31815,y:32960,varname:node_4485,prsc:2,v1:0,v2:0,v3:0;n:type:ShaderForge.SFN_ValueProperty,id:7982,x:32479,y:32977,ptovrint:False,ptlb:MapSize,ptin:_MapSize,varname:node_7982,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_ValueProperty,id:9634,x:32481,y:32542,ptovrint:False,ptlb:MapPos_x,ptin:_MapPos_x,varname:_MapSize_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_ValueProperty,id:9926,x:32481,y:32604,ptovrint:False,ptlb:MapPos_y,ptin:_MapPos_y,varname:_MapSize_copy_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_Add,id:4428,x:32890,y:32818,varname:node_4428,prsc:2|A-317-OUT,B-4822-OUT;n:type:ShaderForge.SFN_Append,id:317,x:32639,y:32560,varname:node_317,prsc:2|A-9634-OUT,B-9926-OUT;n:type:ShaderForge.SFN_Tex2d,id:4565,x:33063,y:32818,ptovrint:False,ptlb:node_4565,ptin:_node_4565,varname:node_4565,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False|UVIN-4428-OUT;proporder:7982-9634-9926-4565;pass:END;sub:END;*/

Shader "Shader Forge/GrassTest" {
    Properties {
        _MapSize ("MapSize", Float ) = 0
        _MapPos_x ("MapPos_x", Float ) = 0
        _MapPos_y ("MapPos_y", Float ) = 0
        _node_4565 ("node_4565", 2D) = "white" {}
    }
    SubShader {
        Tags {
            "RenderType"="Opaque"
        }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma target 3.0
            uniform sampler2D _node_4565; uniform float4 _node_4565_ST;
            UNITY_INSTANCING_BUFFER_START( Props )
                UNITY_DEFINE_INSTANCED_PROP( float, _MapSize)
                UNITY_DEFINE_INSTANCED_PROP( float, _MapPos_x)
                UNITY_DEFINE_INSTANCED_PROP( float, _MapPos_y)
            UNITY_INSTANCING_BUFFER_END( Props )
            struct VertexInput {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float3 normalDir : TEXCOORD0;
                float3 tangentDir : TEXCOORD1;
                float3 bitangentDir : TEXCOORD2;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                UNITY_SETUP_INSTANCE_ID( v );
                UNITY_TRANSFER_INSTANCE_ID( v, o );
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                o.tangentDir = normalize( mul( unity_ObjectToWorld, float4( v.tangent.xyz, 0.0 ) ).xyz );
                o.bitangentDir = normalize(cross(o.normalDir, o.tangentDir) * v.tangent.w);
                o.pos = UnityObjectToClipPos( v.vertex );
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                UNITY_SETUP_INSTANCE_ID( i );
                i.normalDir = normalize(i.normalDir);
                float3x3 tangentTransform = float3x3( i.tangentDir, i.bitangentDir, i.normalDir);
                float _MapPos_x_var = UNITY_ACCESS_INSTANCED_PROP( Props, _MapPos_x );
                float _MapPos_y_var = UNITY_ACCESS_INSTANCED_PROP( Props, _MapPos_y );
                float2 node_421 = mul( unity_ObjectToWorld, float4(float3(0,0,0),0) ).xyz.rgb.rb;
                float _MapSize_var = UNITY_ACCESS_INSTANCED_PROP( Props, _MapSize );
                float2 node_4428 = (float2(_MapPos_x_var,_MapPos_y_var)+(float2(node_421.r,node_421.g)/_MapSize_var));
                float4 _node_4565_var = tex2D(_node_4565,TRANSFORM_TEX(node_4428, _node_4565));
                float3 normalLocal = _node_4565_var.rgb;
                float3 normalDirection = normalize(mul( normalLocal, tangentTransform )); // Perturbed normals
////// Lighting:
                float3 finalColor = 0;
                return fixed4(finalColor,1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
