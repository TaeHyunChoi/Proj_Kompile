Shader "Custom/WorldSpaceAtlasShader"
{
    Properties
    {
        _Color ("Main Color (Dim Tint)", Color) = (1,1,1,1)
        _TopAtlas ("Top Atlas Texture", 2D) = "white" {}
        _SideAtlas ("Side Atlas Texture", 2D) = "white" {}
        [HideInInspector] _MainTex ("Fallback Texture", 2D) = "white" {} 
        _TextureScale ("Global Texture Scale", Float) = 1.0
        _SideBrightness ("Side Brightness", Range(0, 1)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // [핵심 1] vertex:vert 옵션을 추가하여 버텍스 데이터를 직접 핸들링합니다.
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0 

        sampler2D _TopAtlas;
        sampler2D _SideAtlas;
        
        fixed4 _Color; 
        float _TextureScale;
        float _SideBrightness;

        // [핵심 2] 글로벌 변수(_TopUVOffset 등)를 삭제하고, 버텍스에서 넘어올 UV를 받을 바구니를 만듭니다.
        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
            float2 customUV2; // 구워진 윗면(Top) Offset
            float2 customUV3; // 구워진 옆면(Side) Offset
        };

        // [핵심 3] C#에서 구워 넣은 uv2(texcoord1)와 uv3(texcoord2)를 Input 구조체로 넘겨줍니다.
        void vert (inout appdata_full v, out Input o) 
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.customUV2 = v.texcoord1.xy;
            o.customUV3 = v.texcoord2.xy;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 absNormal = abs(IN.worldNormal);
            float isTop = step(0.5, absNormal.y);
            float isXFace = step(absNormal.z, absNormal.x);

            // [핵심 4] 8x8 아틀라스의 고정 스케일(1/8)
            float atlasScale = 0.125;

            // 윗면(Top) UV 계산 (버텍스에 구워진 customUV2 사용)
            float2 topWorldUV = frac(IN.worldPos.xz * _TextureScale);
            float2 topAtlasUV = topWorldUV * atlasScale + IN.customUV2;
            fixed4 topColor = tex2D(_TopAtlas, topAtlasUV);

            // 옆면(Side) UV 계산 (버텍스에 구워진 customUV3 사용)
            float2 uvZFace = frac(IN.worldPos.xy * _TextureScale);
            float2 uvXFace = frac(IN.worldPos.zy * _TextureScale);
            float2 sideWorldUV = lerp(uvZFace, uvXFace, isXFace);
            
            float2 sideAtlasUV = sideWorldUV * atlasScale + IN.customUV3;
            fixed4 sideColor = tex2D(_SideAtlas, sideAtlasUV) * _SideBrightness;

            // 최종 색상 결정
            fixed4 finalColor = lerp(sideColor, topColor, isTop);
            
            o.Albedo = finalColor.rgb * _Color.rgb; 
            o.Alpha = finalColor.a * _Color.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}