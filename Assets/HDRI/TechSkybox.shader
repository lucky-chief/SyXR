Shader "URP/TechSkyboxVR"
{
    Properties
    {
        _BaseColor    ("底色", Color)       = (0.02,0.05,0.12,1)
        _GridColor    ("网格色", Color)     = (0.20,0.85,1.00,1)
        _GlowColor    ("辉光色", Color)     = (0.00,0.65,1.00,1)
        _AccentColor  ("星点色", Color)     = (0.55,0.30,1.00,1)

        _Speed        ("整体速度", Range(0,2))   = 0.35
        _GridDensity  ("网格密度", Range(4,100))  = 22
        _LineWidth    ("线宽", Range(0.005,0.06))= 0.022

        _ScanSpeed    ("扫描带速度", Range(0,1)) = 0.15
        _ScanWidth    ("扫描带宽度", Range(0.02,0.4)) = 0.10
        _ScanIntensity("扫描强度", Range(0,2))   = 0.6

        _StarDensity  ("星点密度", Range(1,16))  = 7
        _RingSpeed    ("脉冲环速度", Range(0,1)) = 0.08
        _HorizonFog   ("地平线雾强", Range(0,1)) = 0.30
        _Exposure     ("曝光", Range(0.2,2.5))   = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Background"
            "RenderType"="Background"
            "PreviewType"="Skybox"
            "RenderPipeline"="UniversalPipeline"
        }

        // VR 单通道/多视图安全：不写深度，关闭裁剪避免远/近裁把天空裁掉
        Cull Off
        ZWrite Off
        ZTest LEqual
        // 某些 OpenXR/模拟VR自写skybox出现单眼黑或天空被裁，可取消下一行注释
        // ZClip False

        Pass
        {
            HLSLPROGRAM

            // XR stereo variants：Instanced + Multiview 都覆盖
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor, _GridColor, _GlowColor, _AccentColor;
                half _Speed, _GridDensity, _LineWidth;
                half _ScanSpeed, _ScanWidth, _ScanIntensity;
                half _StarDensity, _RingSpeed, _HorizonFog, _Exposure;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dirOS      : TEXCOORD0; // 用对象空间方向，VR不依赖相机
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // 天空盒网格顶点位置即方向，归一化后不随相机平移产生视差
                o.dirOS = IN.positionOS.xyz;
                o.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return o;
            }

            half GridLine(half coord, half density, half width, half t, half flow)
            {
                half v = sin(coord * density + t * flow);
                return 1.0 - smoothstep(0.0, width, abs(v));
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 d = normalize(i.dirOS);
                half t = _Time.y * _Speed;

                half el = asin(clamp(d.y, -1.0, 1.0)); // 仰角
                half az = atan2(d.z, d.x);             // 方位角
                half hemi = d.y * 0.5 + 0.5;

                half3 col = _BaseColor.rgb;

                // 1) 经纬网格（慢速流动）
                // half lat  = GridLine(el, _GridDensity, _LineWidth, t, 0.4);
                // half lon  = GridLine(az, _GridDensity, _LineWidth, t, 0.6);
                // half diag = GridLine(el + az, _GridDensity * 0.7, _LineWidth * 0.8, t, 0.9);
                // half grid = max(max(lat, lon), 0.0) * 0.3;
                // col += _GridColor.rgb * grid * (0.5 + 0.5 * sin(_Time.y * 0.5));

                // 2) 仰角扫描带（很慢，避免晕）
                half scanPos = frac(hemi + t * _ScanSpeed);
                half scan = 1.0 - smoothstep(0.0, _ScanWidth, abs(scanPos - 0.5) * 2.0);
                scan *= 0.7 + 0.3 * sin(_Time.y * 1.5); // 弱脉冲
                col += _GlowColor.rgb * scan * _ScanIntensity * 0.2;

                // 3) 数据星点（稀疏、慢闪）
                // half cell = az * _StarDensity + el * _StarDensity * 0.5;
                // half h1 = frac(sin(cell * 12.9898) * 43758.5453);
                // half h2 = frac(sin(cell * 78.2330) * 12543.1234);
                // half spark = step(0.98, h1) * step(0.98, h2);
                // half blink = 0.5 + 0.4 * sin(_Time.y * 2.2 + h1 * 80.0);
                // col += _AccentColor.rgb * spark * blink * 15.8;

                // 4) 天顶脉冲环（极慢）
                half r = abs(el);
                half ringR = frac(t * _RingSpeed) * 1.3;
                half ring = 1.0 - smoothstep(0.0, 0.05, abs(r - ringR));
                ring *= 0.5 + 0.5 * sin(_Time.y * 1.0);
                col += _GlowColor.rgb * ring * 0.18;

                // 5) 地平线冷雾
                half horizon = 1.0 - abs(d.y);
                col += _GlowColor.rgb * pow(horizon, 3.0) * _HorizonFog;

                // 顶亮底暗
                col *= lerp(0.75, 1.1, hemi);
                col *= _Exposure;

                return half4(col, 1.0);
            }

            ENDHLSL
        }
    }
}