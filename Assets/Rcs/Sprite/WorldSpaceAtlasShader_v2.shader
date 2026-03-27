Shader "Custom/WorldSpaceAtlasShader_v3"
{
    Properties
    {
        // 에디터에서 타일을 어둡게 만들기 위해 주입받을 틴트 컬러 (기본값: 흰색)
        _Color ("Main Color (Dim Tint)", Color) = (1,1,1,1)
        
        // [핵심 추가] 윗면과 옆면의 아틀라스를 각각 독립적으로 받을 수 있도록 개방합니다.
        _TopAtlas ("Top Atlas Texture", 2D) = "white" {}
        _SideAtlas ("Side Atlas Texture", 2D) = "white" {}

        // 기존 시스템이나 에러를 방지하기 위한 더미 변수
        [HideInInspector] _MainTex ("Fallback Texture", 2D) = "white" {} 

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
        #pragma target 3.0 // 다중 텍스처 샘플링을 위해 Shader Model 3.0 타겟

        // C#에서 받아올 두 개의 아틀라스 텍스처
        sampler2D _TopAtlas;
        sampler2D _SideAtlas;
        
        fixed4 _Color; 
        float _TextureScale;
        float _SideBrightness;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };

        // C#에서 받아오는 윗면/옆면의 아틀라스 좌표(UV) 정보
        float2 _TopUVOffset;
        float2 _TopUVScale;
        float2 _SideUVOffset;
        float2 _SideUVScale;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 absNormal = abs(IN.worldNormal);

            // =====================================================================
            // [최적화 로직] 
            // GPU는 if-else와 같은 동적 분기문(Branching)을 만나면 성능이 저하됩니다.
            // 따라서 조건문 대신 step()과 lerp() 함수를 사용하여 픽셀을 병렬 연산합니다.
            // =====================================================================

            // 1. 윗면(Top) 여부 판별
            // absNormal.y가 0.5보다 크면 1(윗면), 아니면 0(옆면)을 반환
            float isTop = step(0.5, absNormal.y);

            // 2. 옆면(Side) 중 X축 면인지 Z축 면인지 판별
            // absNormal.x가 absNormal.z보다 크면 1, 아니면 0을 반환
            float isXFace = step(absNormal.z, absNormal.x);

            // 3. 윗면(Top) UV 계산 및 색상 추출
            float2 topWorldUV = frac(IN.worldPos.xz * _TextureScale);
            float2 topAtlasUV = topWorldUV * _TopUVScale + _TopUVOffset;
            fixed4 topColor = tex2D(_TopAtlas, topAtlasUV);

            // 4. 옆면(Side) UV 계산 및 색상 추출 (Triplanar)
            // isXFace가 0이면 XY 투영(Z면), 1이면 ZY 투영(X면)을 선택
            float2 uvZFace = frac(IN.worldPos.xy * _TextureScale);
            float2 uvXFace = frac(IN.worldPos.zy * _TextureScale);
            float2 sideWorldUV = lerp(uvZFace, uvXFace, isXFace);
            
            float2 sideAtlasUV = sideWorldUV * _SideUVScale + _SideUVOffset;
            fixed4 sideColor = tex2D(_SideAtlas, sideAtlasUV) * _SideBrightness;

            // 5. 최종 색상 결정
            // isTop이 1이면 윗면 색상, 0이면 옆면 색상을 선택합니다.
            fixed4 finalColor = lerp(sideColor, topColor, isTop);
            
            // 6. 틴트 컬러(_Color) 적용하여 최종 출력
            o.Albedo = finalColor.rgb * _Color.rgb; 
            o.Alpha = finalColor.a * _Color.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}