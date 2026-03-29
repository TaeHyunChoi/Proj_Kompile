Shader "Kompile/WorldSpaceAtlasShader"
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
        // [핵심] 정점 데이터를 커스텀으로 엮어주기 위해 vertex:vert 함수를 추가합니다.
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0 

        sampler2D _TopAtlas;
        sampler2D _SideAtlas;
        
        fixed4 _Color; 
        float _TextureScale;
        float _SideBrightness;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
            // C#에서 구워넣은 보조 UV 채널을 받기 위한 변수
            float2 uv2;
            float2 uv3;
        };

        // Vertex Shader: 정점 데이터(appdata_full)에서 uv2(texcoord1)와 uv3(texcoord2)를 빼냅니다.
        void vert (inout appdata_full v, out Input o) {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.uv2 = v.texcoord1.xy;
            o.uv3 = v.texcoord2.xy;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 absNormal = abs(IN.worldNormal);
            float isTop = step(0.5, absNormal.y);
            float isXFace = step(absNormal.z, absNormal.x);

            // C#에서 넘겨준 오프셋 데이터(uv2, uv3)와 1/8(0.125) 스케일을 사용하여 최종 텍스처 위치를 잡습니다.
            float2 topUVOffset = IN.uv2;
            float2 sideUVOffset = IN.uv3;
            float2 atlasScale = float2(0.125, 0.125);

            float2 topWorldUV = frac(IN.worldPos.xz * _TextureScale);
            float2 topAtlasUV = topWorldUV * atlasScale + topUVOffset;
            fixed4 topColor = tex2D(_TopAtlas, topAtlasUV);

            float2 uvZFace = frac(IN.worldPos.xy * _TextureScale);
            float2 uvXFace = frac(IN.worldPos.zy * _TextureScale);
            float2 sideWorldUV = lerp(uvZFace, uvXFace, isXFace);
            
            float2 sideAtlasUV = sideWorldUV * atlasScale + sideUVOffset;
            fixed4 sideColor = tex2D(_SideAtlas, sideAtlasUV) * _SideBrightness;

            fixed4 finalColor = lerp(sideColor, topColor, isTop);
            
            o.Albedo = finalColor.rgb * _Color.rgb; 
            o.Alpha = finalColor.a * _Color.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}