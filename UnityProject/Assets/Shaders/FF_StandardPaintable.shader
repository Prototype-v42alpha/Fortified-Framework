Shader "Fortified/StandardPaintable"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _MaskTex ("Mask Texture (R=Paintable)", 2D) = "black" {}
        _CamoTex ("Camo Texture (RGB Mask)", 2D) = "white" {}
        _IconTex ("Icon (Decal)", 2D) = "black" {}
        _PaintMask ("Paint Mask (R: Wear)", 2D) = "white" {}
        
        _Color ("Master Tint (Vanilla Flash)", Color) = (1,1,1,1)
        _ColorOne ("Color One (R)", Color) = (1,1,1,1)
        _ColorTwo ("Color Two (G)", Color) = (1,1,1,1)
        _ColorThree ("Color Three (B)", Color) = (1,1,1,1)
        
        _UseCamo ("Use Camo (0=off 1=on)", Float) = 0
        _OverlayTex ("Overlay Texture", 2D) = "black" {}
        _UseOverlay ("Use Overlay (0=off 1=on)", Float) = 0
        _OverlayMultiply ("Overlay Multiply Base (0=alpha 1=multiply)", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _MaskTex;
            sampler2D _CamoTex;
            sampler2D _IconTex;
            sampler2D _PaintMask;
            
            float4 _Color;      // 原版主颜色（受击变红控制项）
            float4 _ColorOne;   // 涂装第一色
            float4 _ColorTwo;   // 涂装第二色
            float4 _ColorThree; // 涂装第三色
            
            float _UseCamo;
            float _Cutoff;
            float _CamoRotation;
            sampler2D _OverlayTex;
            float _UseOverlay;
            float _OverlayMultiply;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 mainTexSample = tex2D(_MainTex, i.uv);
                fixed4 maskSample = tex2D(_MaskTex, i.uv);
                
                float2 camoUV = i.uv;
                if (_UseCamo > 0.5)
                {
                    float s, c;
                    sincos(_CamoRotation, s, c);
                    float2 centered = i.uv - 0.5;
                    camoUV.x = centered.x * c - centered.y * s + 0.5;
                    camoUV.y = centered.x * s + centered.y * c + 0.5;
                }
                fixed4 camoSample = tex2D(_CamoTex, camoUV);
                fixed4 iconSample = tex2D(_IconTex, i.uv);
                fixed4 paintMaskSample = tex2D(_PaintMask, i.uv);

                fixed3 diffuse = mainTexSample.rgb;

                // 亮度调节由ColorTwo.a控制
                float baseBright = _ColorTwo.a;
                if (baseBright > 0.001) {
                    float lum = dot(diffuse, float3(0.299, 0.587, 0.114));
                    float mask = smoothstep(0.008, 0.02, lum);
                    float gamma = 1.0 / (1.0 + baseBright * 3.0);
                    fixed3 brightened = pow(max(diffuse, 0.001), gamma);
                    diffuse = lerp(diffuse, brightened, mask);
                }

                // 计算涂装
                // 标准模式：应用 ColorOne
                fixed3 standardPaint = diffuse * _ColorOne.rgb;

                // 迷彩模式：分配 R/G/B 通道颜色
                fixed3 camoPaint = diffuse * (_ColorOne.rgb * camoSample.r
                                            + _ColorTwo.rgb * camoSample.g
                                            + _ColorThree.rgb * camoSample.b);

                fixed3 paintedBase = lerp(diffuse, lerp(standardPaint, camoPaint, _UseCamo), maskSample.r);

                // 喷漆图标
                float iconLum = dot(mainTexSample.rgb, float3(0.3, 0.59, 0.11));
                float blend = iconSample.a * iconLum * paintMaskSample.r;
                fixed3 colorWithIcon = lerp(paintedBase, iconSample.rgb, blend);

                // 叠加层
                fixed3 finalColor = colorWithIcon;
                if (_UseOverlay > 0.5)
                {
                    fixed4 overlaySample = tex2D(_OverlayTex, i.uv);
                    fixed3 overlayColor = lerp(overlaySample.rgb, overlaySample.rgb * mainTexSample.rgb * 2.0, _OverlayMultiply);
                    finalColor = lerp(finalColor, overlayColor, overlaySample.a);
                }

                finalColor *= _Color.rgb;

                clip(mainTexSample.a - _Cutoff);
                return fixed4(finalColor, mainTexSample.a);
            }
            ENDCG
        }
    }
}
