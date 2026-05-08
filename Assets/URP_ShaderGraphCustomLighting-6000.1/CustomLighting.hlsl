#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

// @Cyanilux | https://github.com/Cyanilux/URP_ShaderGraphCustomLighting
// Fixed version for Unity 2021 - All features preserved

//------------------------------------------------------------------------------------------------------
// Keyword Pragmas
//------------------------------------------------------------------------------------------------------

#ifndef SHADERGRAPH_PREVIEW
	#if SHADERPASS != SHADERPASS_FORWARD && SHADERPASS != SHADERPASS_GBUFFER
		// #if to avoid "duplicate keyword" warnings if this is included in a Lit Graph

    	#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
    	#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
		#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
		#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
		#pragma multi_compile _ _CLUSTER_LIGHT_LOOP

		// Left some keywords (e.g. light layers, cookies) in subgraphs to help avoid unnecessary shader variants
		// But means if those subgraphs are nested in another, you'll need to copy the keywords from blackboard

	#endif
#endif

//------------------------------------------------------------------------------------------------------
// Main Light
//------------------------------------------------------------------------------------------------------

void MainLight_float(out float3 Direction, out float3 Color, out float DistanceAtten){
	#ifdef SHADERGRAPH_PREVIEW
		Direction = normalize(float3(1,1,-0.4));
		Color = float3(1,1,1);
		DistanceAtten = 1;
	#else
		Light mainLight = GetMainLight();
		Direction = mainLight.direction;
		Color = mainLight.color;
		DistanceAtten = mainLight.distanceAttenuation;
	#endif
}

void MainLight_half(out half3 Direction, out half3 Color, out half DistanceAtten){
    float3 dir, col;
    float atten;
    MainLight_float(dir, col, atten);
    Direction = dir;
    Color = col;
    DistanceAtten = atten;
}

void MainLightLayer_float(float3 Shading, out float3 Out){
	#ifdef SHADERGRAPH_PREVIEW
		Out = Shading;
	#else
		Out = 0;
		#ifdef _LIGHT_LAYERS
		uint meshRenderingLayers = GetMeshRenderingLayer();
		if (IsMatchingLightLayer(GetMainLight().layerMask, meshRenderingLayers))
		#endif
		{
			Out = Shading;
		}
	#endif
}

void MainLightLayer_half(half3 Shading, out half3 Out){
    float3 result;
    MainLightLayer_float(Shading, result);
    Out = result;
}

void MainLightCookie_float(float3 WorldPos, out float3 Cookie){
	Cookie = 1;
	#if defined(_LIGHT_COOKIES)
        Cookie = SampleMainLightCookie(WorldPos);
    #endif
}

void MainLightCookie_half(half3 WorldPos, out half3 Cookie){
    float3 result;
    MainLightCookie_float(WorldPos, result);
    Cookie = result;
}

//------------------------------------------------------------------------------------------------------
// Main Light Shadows - 修复版本
//------------------------------------------------------------------------------------------------------

// 核心函数 - 带Shadowmask参数
void MainLightShadows_float(float3 WorldPos, half4 Shadowmask, out float ShadowAtten){
	#ifdef SHADERGRAPH_PREVIEW
		ShadowAtten = 1;
	#else
		#if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
		float4 shadowCoord = ComputeScreenPos(TransformWorldToHClip(WorldPos));
		#else
		float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
		#endif
		ShadowAtten = MainLightShadow(shadowCoord, WorldPos, Shadowmask, _MainLightOcclusionProbes);
	#endif
}

// 简化版本 - 不带Shadowmask参数 (保持与旧版本兼容)
void MainLightShadows_float(float3 WorldPos, out float ShadowAtten){
	MainLightShadows_float(WorldPos, half4(1,1,1,1), ShadowAtten);
}

void MainLightShadows_half(half3 WorldPos, half4 Shadowmask, out half ShadowAtten){
    float result;
    MainLightShadows_float(WorldPos, Shadowmask, result);
    ShadowAtten = result;
}

// Shader Graph兼容版本 - 输出half4 (用于需要RGBA输出的情况)
void MainLightShadows_SG_float(float3 WorldPos, out half4 ShadowAttenRGBA){
    float shadow;
    MainLightShadows_float(WorldPos, half4(1,1,1,1), shadow);
    ShadowAttenRGBA = half4(shadow, shadow, shadow, 1);
}

//------------------------------------------------------------------------------------------------------
// Baked GI
//------------------------------------------------------------------------------------------------------

void Shadowmask_half(float2 lightmapUV, out half4 Shadowmask){
	#ifdef SHADERGRAPH_PREVIEW
		Shadowmask = half4(1,1,1,1);
	#else
		OUTPUT_LIGHTMAP_UV(lightmapUV, unity_LightmapST, lightmapUV);
		Shadowmask = SAMPLE_SHADOWMASK(lightmapUV);
	#endif
}

void SubtractiveGI_float(float ShadowAtten, float3 NormalWS, float3 BakedGI, out half3 result){
	#ifdef SHADERGRAPH_PREVIEW
		result = half3(1,1,1);
	#else
		Light mainLight = GetMainLight();
		mainLight.shadowAttenuation = ShadowAtten;
		MixRealtimeAndBakedGI(mainLight, NormalWS, BakedGI);
		result = BakedGI;
	#endif
}

void SubtractiveGI_half(half ShadowAtten, half3 NormalWS, half3 BakedGI, out half3 result){
    float3 res;
    SubtractiveGI_float(ShadowAtten, NormalWS, BakedGI, res);
    result = res;
}

//------------------------------------------------------------------------------------------------------
// Half Lambert Function
//------------------------------------------------------------------------------------------------------

void HalfLambert_float(float3 Normal, float3 LightDirection, out float Out)
{
    float NdotL = dot(Normal, LightDirection);
    Out = (NdotL * 0.5) + 0.5;
}

void HalfLambert_half(half3 Normal, half3 LightDirection, out half Out)
{
    float result;
    HalfLambert_float(Normal, LightDirection, result);
    Out = result;
}

void HalfLambert_SG_float(float3 Normal, float3 LightDirection, out half4 OutRGBA)
{
    float result;
    HalfLambert_float(Normal, LightDirection, result);
    OutRGBA = half4(result, result, result, 1);
}

//------------------------------------------------------------------------------------------------------
// Additional Lights
//------------------------------------------------------------------------------------------------------

void AdditionalLights_float(float3 SpecColor, float Smoothness, float3 WorldPosition, float3 WorldNormal, float3 WorldView, half4 Shadowmask,
							out float3 Diffuse, out float3 Specular) {
	float3 diffuseColor = 0;
	float3 specularColor = 0;
#ifndef SHADERGRAPH_PREVIEW
	Smoothness = exp2(10 * Smoothness + 1);
	uint pixelLightCount = GetAdditionalLightsCount();

	#if USE_CLUSTER_LIGHT_LOOP
	for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++) {
		CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
		Light light = GetAdditionalLight(lightIndex, WorldPosition, Shadowmask);
		#ifdef _LIGHT_LAYERS
		if (IsMatchingLightLayer(light.layerMask, GetMeshRenderingLayer()))
		#endif
		{
			float3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
			diffuseColor += LightingLambert(attenuatedLightColor, light.direction, WorldNormal);
			specularColor += LightingSpecular(attenuatedLightColor, light.direction, WorldNormal, WorldView, float4(SpecColor, 0), Smoothness);
		}
	}
	#endif

	InputData inputData = (InputData)0;
	float4 screenPos = ComputeScreenPos(TransformWorldToHClip(WorldPosition));
	inputData.normalizedScreenSpaceUV = screenPos.xy / screenPos.w;
	inputData.positionWS = WorldPosition;

	LIGHT_LOOP_BEGIN(pixelLightCount)
		Light light = GetAdditionalLight(lightIndex, WorldPosition, Shadowmask);
		#ifdef _LIGHT_LAYERS
		if (IsMatchingLightLayer(light.layerMask, GetMeshRenderingLayer()))
		#endif
		{
			float3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
			diffuseColor += LightingLambert(attenuatedLightColor, light.direction, WorldNormal);
			specularColor += LightingSpecular(attenuatedLightColor, light.direction, WorldNormal, WorldView, float4(SpecColor, 0), Smoothness);
		}
	LIGHT_LOOP_END
#endif

	Diffuse = diffuseColor;
	Specular = specularColor;
}

// 向后兼容版本 - 不带Shadowmask
void AdditionalLights_float(float3 SpecColor, float Smoothness, float3 WorldPosition, float3 WorldNormal, float3 WorldView, 
							out float3 Diffuse, out float3 Specular) {
	AdditionalLights_float(SpecColor, Smoothness, WorldPosition, WorldNormal, WorldView, half4(1,1,1,1), Diffuse, Specular);
}

// 简化版本
void AdditionalLights_Simple_float(float3 WorldPosition, float3 WorldNormal, out float3 Diffuse) {
	float3 diffuseColor = 0;
#ifndef SHADERGRAPH_PREVIEW
	WorldNormal = normalize(WorldNormal);
	int pixelLightCount = GetAdditionalLightsCount();
	
	for (int i = 0; i < pixelLightCount; ++i) {
		Light light = GetAdditionalLight(i, WorldPosition);
		float3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
		
		float NdotL = saturate(dot(WorldNormal, light.direction));
		diffuseColor += attenuatedLightColor * NdotL;
	}
#endif

	Diffuse = diffuseColor;
}

//------------------------------------------------------------------------------------------------------
// Complete Lighting Solution
//------------------------------------------------------------------------------------------------------

void CompleteLighting_float(float3 WorldPos, float3 WorldNormal, float3 BaseColor, out float3 FinalColor)
{
    // Get ambient lighting
    float3 ambient;
    #ifdef SHADERGRAPH_PREVIEW
        ambient = float3(0.1, 0.1, 0.1);
    #else
        ambient = SampleSH(WorldNormal);
    #endif
    
    // Get main light
    float3 mainLightDir;
    float3 mainLightColor;
    float mainLightAtten;
    MainLight_float(mainLightDir, mainLightColor, mainLightAtten);
    
    // Get shadows
    float shadowAtten;
    MainLightShadows_float(WorldPos, shadowAtten);
    
    // Calculate half lambert
    float halfLambert;
    HalfLambert_float(WorldNormal, mainLightDir, halfLambert);
    
    // Get additional lights
    float3 additionalLights;
    AdditionalLights_Simple_float(WorldPos, WorldNormal, additionalLights);
    
    // Combine all lighting
    float3 directLighting = mainLightColor * halfLambert * mainLightAtten * shadowAtten;
    FinalColor = BaseColor * (ambient + directLighting + additionalLights);
}

void CompleteLighting_SG_float(float3 WorldPos, float3 WorldNormal, float3 BaseColor, out half4 FinalColorRGBA)
{
    float3 result;
    CompleteLighting_float(WorldPos, WorldNormal, BaseColor, result);
    FinalColorRGBA = half4(result, 1);
}

//------------------------------------------------------------------------------------------------------
// Texture with Half Lambert Lighting (主要使用这个)
//------------------------------------------------------------------------------------------------------

void TextureWithHalfLambert_float(float3 WorldPos, float3 WorldNormal, float3 TextureColor, out float3 Output)
{
    #ifdef SHADERGRAPH_PREVIEW
        // Preview fallback
        float3 lightDir = normalize(float3(1,1,-0.4));
        float halfLambert;
        HalfLambert_float(WorldNormal, lightDir, halfLambert);
        Output = TextureColor * halfLambert;
    #else
        // Get main light
        float3 mainLightDir;
        float3 mainLightColor;
        float mainLightAtten;
        MainLight_float(mainLightDir, mainLightColor, mainLightAtten);
        
        // Get shadows - 使用修复后的函数
        float shadowAtten;
        MainLightShadows_float(WorldPos, shadowAtten);
        
        // Calculate half lambert
        float halfLambert;
        HalfLambert_float(WorldNormal, mainLightDir, halfLambert);
        
        // Get additional lights
        float3 additionalLights;
        AdditionalLights_Simple_float(WorldPos, WorldNormal, additionalLights);
        
        // Get ambient
        float3 ambient = SampleSH(WorldNormal);
        
        // Combine everything
        float3 directLighting = mainLightColor * halfLambert * mainLightAtten * shadowAtten;
        Output = TextureColor * (ambient + directLighting + additionalLights);
    #endif
}

void TextureWithHalfLambert_SG_float(float3 WorldPos, float3 WorldNormal, float3 TextureColor, out half4 OutputRGBA)
{
    float3 result;
    TextureWithHalfLambert_float(WorldPos, WorldNormal, TextureColor, result);
    OutputRGBA = half4(result, 1);
}

//------------------------------------------------------------------------------------------------------
// Ambient/Baked GI
//------------------------------------------------------------------------------------------------------

void AmbientGI_float(float3 WorldNormal, out float3 Ambient)
{
    #ifdef SHADERGRAPH_PREVIEW
        Ambient = float3(0.1, 0.1, 0.1);
    #else
        Ambient = SampleSH(WorldNormal);
    #endif
}

void AmbientGI_half(half3 WorldNormal, out half3 Ambient)
{
    float3 ambient;
    AmbientGI_float(WorldNormal, ambient);
    Ambient = ambient;
}

void MixFog_float(float3 Colour, float Fog, out float3 Out){
	#ifdef SHADERGRAPH_PREVIEW
		Out = Colour;
	#else
		Out = MixFog(Colour, Fog);
	#endif
}

void MixFog_half(half3 Colour, half Fog, out half3 Out){
    float3 result;
    MixFog_float(Colour, Fog, result);
    Out = result;
}

#endif // CUSTOM_LIGHTING_INCLUDED