namespace Kompile.Asset.Provider
{
    using Kompile.Asset.Data;
    using Kompile.Unit.Data;
    using Kompile.Unit.Entity;
    using MessagePack;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using Unity.Collections;
    using Kompile.Field.Entity;
    using Kompile.Field.Data;
    
    public static partial class AssetProvider // Field
    {
        /// <summary> 필드(Field) 컨텐츠 전용 에셋 공급자 </summary>
        public static class Field
        {
            public static async Awaitable<FieldEntity> GetOrNewEntityInstanceAsync(int index, Transform root, AnimatorOverrideController aoc, FieldMapQueryService mapQuery = null )
            {
                AssetKey prefabKey = new AssetKey(AssetConst.UNIT_PREFAB_FIELD);
                FieldEntity fieldEntity = await AssetProvider.GetOrNewEntityInstanceAsync<FieldEntity>(prefabKey, root);
                FieldUnitTableData data = FieldUnitTableProvider.GetData(index);
                FieldUnitAnimClipContext clip = await data.GetAnimClipsAsync();
                fieldEntity.Initialize(data, clip, aoc, mapQuery);
                
                return fieldEntity;
            }
        }
    }    
}