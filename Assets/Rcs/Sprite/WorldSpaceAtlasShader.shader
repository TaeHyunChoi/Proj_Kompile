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

        _TopUVOffset ("Top UV Offset", Vector) = (0,0,0,0)
        _SideUVOffset ("Side UV Offset", Vector) = (0,0,0,0)

        // [추가] 에디터 타일인지, 베이킹된 메쉬인지 구분하는 스위치 (기본값 1 = Bake)
        [HideInInspector] _IsBaked ("Is Baked", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0 

        sampler2D _TopAtlas;
        sampler2D _SideAtlas;
        
        fixed4 _Color; 
        float _TextureScale;
        float _SideBrightness;

        float2 _TopUVOffset;
        float2 _SideUVOffset;
        float _IsBaked; // [추가] 변수 선언

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
            float2 customUV2; 
            float2 customUV3; 
        };

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

            float atlasScale = 0.125;

            // [핵심 해결 구간] customUV 값에 _IsBaked를 곱합니다.
            // 에디터(_IsBaked=0)에서는 찌꺼기 값이 0이 되어 완벽히 차단됩니다!
            float2 topWorldUV = frac(IN.worldPos.xz * _TextureScale);
            float2 topAtlasUV = topWorldUV * atlasScale + (IN.customUV2 * _IsBaked) + _TopUVOffset;
            fixed4 topColor = tex2D(_TopAtlas, topAtlasUV);

            float2 uvZFace = frac(IN.worldPos.xy * _TextureScale);
            float2 uvXFace = frac(IN.worldPos.zy * _TextureScale);
            float2 sideWorldUV = lerp(uvZFace, uvXFace, isXFace);
            
            float2 sideAtlasUV = sideWorldUV * atlasScale + (IN.customUV3 * _IsBaked) + _SideUVOffset;
            fixed4 sideColor = tex2D(_SideAtlas, sideAtlasUV) * _SideBrightness;

            fixed4 finalColor = lerp(sideColor, topColor, isTop);
            
            o.Albedo = finalColor.rgb * _Color.rgb; 
            o.Alpha = finalColor.a * _Color.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}