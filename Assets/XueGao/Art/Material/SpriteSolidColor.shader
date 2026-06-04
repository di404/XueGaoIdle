Shader "XueGao/Sprite Solid Color"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _SolidColor ("Solid Color", Color) = (1, 1, 1, 1)
        [MaterialToggle] PixelSnap ("Pixel Snap", Float) = 0
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _Flip ("Flip", Vector) = (1, 1, 1, 1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #include "UnityCG.cginc"
            #include "UnitySprites.cginc"

            fixed4 _SolidColor;

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                input.vertex = UnityFlipSprite(input.vertex, _Flip);
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;

                #ifdef PIXELSNAP_ON
                output.vertex = UnityPixelSnap(output.vertex);
                #endif

                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed alpha = SampleSpriteTexture(input.texcoord).a * _SolidColor.a;
                fixed4 color = fixed4(_SolidColor.rgb, alpha);
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
