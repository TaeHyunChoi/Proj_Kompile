namespace Kompile.Map.Data
{
    using UnityEngine;
    using System;
    using System.Collections.Generic;
    
    [Serializable]
    public class MapTextureData
    {
        public int GlobalIndex;
        public string TextureName;
    }

    [CreateAssetMenu(fileName = "MapTextureTable", menuName = "Framework/Map/MapTextureTable")]
    public class MapTextureTable : ScriptableObject
    {
        public List<MapTextureData> TextureList = new List<MapTextureData>();

        public int GetOrAssignIndex(string textureName)
        {
            // 1. 이미 등록된 텍스처 반환 (대소문자 무시)
            foreach (var data in TextureList)
            {
                // 대소문자 무시 비교
                if (true == data.TextureName.Equals(textureName, StringComparison.OrdinalIgnoreCase))
                {
                    return data.GlobalIndex;
                }
            }
            
            // 2. 신규 발급
            int maxIndex = -1;
            foreach (MapTextureData item in TextureList)
            {
                if (item.GlobalIndex > maxIndex)
                {
                    maxIndex = item.GlobalIndex;
                }
            }
            
            int newIndex = maxIndex + 1;
            
            // [핵심 픽스] Unity 엔진에 데이터 변경을 명확히 신고하여 직렬화(저장) 누락 방지
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Assign Texture Index");
#endif

            TextureList.Add(new MapTextureData 
                { 
                    GlobalIndex = newIndex, 
                    TextureName = textureName 
                });

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[Framework] 텍스처 인덱스 자동 발급 완료: {textureName} -> {newIndex}");
#endif
            return newIndex;
        }
    }
}