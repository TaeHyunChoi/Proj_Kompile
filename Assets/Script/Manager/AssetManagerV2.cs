namespace Script.Manager
{
    using UnityEngine;
    using System.Collections.Generic;
    using System;
    
    /// <summary>
    /// 역할: (1)자원 로딩 (2)서브 시스템 초기화 (3)시스템간 통신 중재
    /// </summary>
    public static class AssetManagerV2
    {
        private static Dictionary<Type, ScriptableObject> assetReferenceCache;

        public static void Initialize()
        {
            assetReferenceCache = new Dictionary<Type, ScriptableObject>();
            LoadAllAssetMaps();
        }


        // Binary File
        // ...


        // Addressable Asset Map
        private static void LoadAllAssetMaps()
        {
            ScriptableObject[] maps = Resources.LoadAll<ScriptableObject>("AssetMap");
            if (maps.Length == 0)
            {

                return;
            }

            ScriptableObject map;
            Type mapType, baseType, enumType;
            for (int i = 0; i < maps.Length; ++i)
            {
                map = maps[i];

                mapType = map.GetType();
                baseType = mapType.BaseType; //상속한 부모 타입 찾는거구나?

                // 조건문 무슨 말인지 잘 모르겠네
                while (baseType != null
                    && baseType.IsGenericType
                    && baseType.GetGenericTypeDefinition() != typeof(AssetMapBase<>))
                {
                    baseType = baseType.BaseType;
                }

                if (baseType != null
                    && baseType.IsGenericType)
                {
                    enumType = baseType.GetGenericArguments()[0];

                    if (true == assetReferenceCache.ContainsKey(enumType))
                    {
                        // 중복 무시
                        continue;
                    }

                    assetReferenceCache.Add(enumType, map);
                    if (map is IInitializable initializeMap)
                    {
                        initializeMap.Initialize();
                    }
                }
            }
        }
        public static string GetAssetAddress<TEnum>(TEnum id) where TEnum : Enum
        {
            Type enumType = typeof(TEnum);

            if (true == assetReferenceCache.TryGetValue(enumType, out ScriptableObject map))
            {
                var assetMap = map as AssetMapBase<TEnum>;
                if (assetMap != null)
                {
                    return assetMap.GetAddressKey(id);
                }
            }

            return null;
        }
        //public bool TryGetAssetRefMap<TEnum, TMap>(out TMap map)
        //    where TEnum : Enum
        //    where TMap : AssetMapBase<TEnum>
        //{
        //    Type enumType = typeof(TEnum);
        //    if (_mapCache.TryGetValue(enumType, out ScriptableObject foundMap))
        //    {
        //        map = foundMap as TMap;
        //        return map != null;
        //    }

        //    map = null;
        //    return false;
        //}


        // ??
        // ...
    }
}
