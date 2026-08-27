// Dikey gradyan gokyuzu.
//
// Neden hazir bir skybox dokusu degil: ihtiyacim olan sey iki renk arasinda
// dikey bir gecis. Doku kullanmak hem birkac megabaytlik varlik hem de paleti
// degistirdigimde yeniden uretilmesi gereken bir sey demek. Burada iki renk
// paletten geliyor ve gokyuzu onlarla birlikte degisiyor.
//
// Gokyuzu materyali oldugu icin derinlik yazmiyor ve arka planda kaliyor.
Shader "PhysicsStack/GradientSky"
{
    Properties
    {
        _TopColor ("Ust renk", Color) = (0.86, 0.91, 0.95, 1)
        _BottomColor ("Alt renk", Color) = (0.98, 0.93, 0.89, 1)
        _Exponent ("Gecis sertligi", Range(0.2, 4)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 directionOS : TEXCOORD0;
            };

            float4 _TopColor;
            float4 _BottomColor;
            float _Exponent;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);

                // Gokyuzu kubesinin nesne uzayindaki yonu; yukseklik bilgisi bunun y'si.
                OUT.directionOS = IN.positionOS.xyz;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float height = normalize(IN.directionOS).y * 0.5 + 0.5;
                height = pow(saturate(height), _Exponent);
                return half4(lerp(_BottomColor.rgb, _TopColor.rgb, height), 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
