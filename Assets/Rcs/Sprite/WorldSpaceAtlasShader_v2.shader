Shader "Custom/WorldSpaceAtlasShader_v2"
{
    Properties
    {
        // [추가됨] 에디터에서 타일을 어둡게 만들기 위해 주입받을 틴트 컬러 (기본값: 흰색)
        _Color ("Main Color (Dim Tint)", Color) = (1,1,1,1)
        
        _MainTex ("Atlas Texture", 2D) = "white" {}
        _TextureScale ("Global Texture Scale", Float) = 1.0
        // 옆면의 빛 세기를 조절하여 입체감을 줌
        _SideBrightness ("Side Brightness", Range(0, 1)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color; // [추가됨] Properties의 _Color와 연결될 변수 선언
        float _TextureScale;
        float _SideBrightness;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };

        // C#에서 받아오는 윗면/옆면의 아틀라스 좌표 정보
        float2 _TopUVOffset;
        float2 _TopUVScale;
        float2 _SideUVOffset;
        float2 _SideUVScale;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 absNormal = abs(IN.worldNormal);
            float blend = 0; // 윗면과 옆면을 섞을 비율
            float2 worldUV = float2(0, 0);
            float2 offset = float2(0, 0);
            float2 scale = float2(1, 1);
            float brightness = 1.0;

            // --- 윗면 vs 옆면 판단 로직 ---
            // Y축(위쪽)을 강하게 바라보는 면 (윗면)
            if (absNormal.y > 0.5) 
            {
                worldUV = frac(IN.worldPos.xz * _TextureScale);
                offset = _TopUVOffset;
                scale = _TopUVScale;
            }
            // 옆쪽을 바라보는 면 (옆면)
            else
            {
                // X축과 Z축 투영 중 더 평평한 쪽을 선택 (Triplanar 단순화)
                if (absNormal.x > absNormal.z)
                    worldUV = frac(IN.worldPos.zy * _TextureScale);
                else
                    worldUV = frac(IN.worldPos.xy * _TextureScale);
                
                offset = _SideUVOffset;
                scale = _SideUVScale;
                brightness = _SideBrightness; // 옆면은 좀 더 어둡게
            }

            // 최종 아틀라스 UV 계산
            float2 atlasUV = worldUV * scale + offset;

            fixed4 c = tex2D(_MainTex, atlasUV);
            
            // [수정됨] 최종 텍스처 색상에 _Color(틴트)를 곱해주어 어둡게 만들 수 있도록 적용
            o.Albedo = c.rgb * brightness * _Color.rgb; 
            o.Alpha = c.a * _Color.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}