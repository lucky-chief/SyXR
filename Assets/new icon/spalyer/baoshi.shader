Shader "Custom/URP_Gem_Fresnel_Textured"
{
    Properties
    {
        [Header(Base Texture)]
        _MainTex ("宝石贴图 (Albedo)", 2D) = "white" {}
        _BaseColor ("染色/基础颜色", Color) = (0.6, 0.7, 1.0, 1.0)
        
        [Header(Fresnel Rim)]
        _RimColor ("菲涅尔边缘颜色", Color) = (0.8, 0.9, 1.0, 1.0)
        _RimPower ("边缘强度", Range(1.0, 10.0)) = 3.0
        _RimIntensity ("边缘发光亮度", Range(0.0, 5.0)) = 1.5
        
        [Header(Inner and Reflection)]
        _InnerGradient ("内部渐变强度", Range(0.0, 2.0)) = 0.5
        _ReflectionTex ("Cubemap 反射贴图", Cube) = "black" {}
        _ReflectionIntensity ("反射强度", Range(0.0, 2.0)) = 0.3
        
        [Header(Transparency)]
        _Transparency ("整体透明度", Range(0.0, 1.0)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _RimColor;
                float _RimPower;
                float _RimIntensity;
                float _InnerGradient;
                float _ReflectionIntensity;
                float _Transparency;
                float4 _MainTex_ST;
            CBUFFER_END

            TEXTURE2D(_MainTex); 
            SAMPLER(sampler_MainTex);
            TEXTURECUBE(_ReflectionTex); 
            SAMPLER(sampler_ReflectionTex);

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(i.positionWS));
                float3 normalWS = normalize(i.normalWS);

                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _RimPower);
                
                float3 rimLight = fresnel * _RimColor.rgb * _RimIntensity;

                float3 reflectDir = reflect(-viewDirWS, -normalWS);
                float3 refraction = SAMPLE_TEXTURECUBE(_ReflectionTex, sampler_ReflectionTex, reflectDir).rgb * _ReflectionIntensity;

                float3 innerColor = _BaseColor.rgb * texColor.rgb * (1.0 + _InnerGradient * fresnel);
                
                float3 finalColor = innerColor + refraction + rimLight;

                float alpha = texColor.a * lerp(_Transparency, 1.0, fresnel);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
}