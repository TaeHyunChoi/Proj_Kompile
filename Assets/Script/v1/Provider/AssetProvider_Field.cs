namespace Kompile.Provider
{
    using Kompile.Data;
    using Kompile.Entity;
    using UnityEngine;
    
    public static partial class AssetProvider // Field
    {
        /// <summary> 필드(Field) 컨텐츠 전용 에셋 공급자 </summary>
        public static class Field
        {
            public static async Awaitable<x_FieldEntity> GetOrNewEntityInstanceAsync(int index, Transform root, AnimatorOverrideController aoc, x_FieldMapQueryService mapQuery = null )
            {
                AssetKey prefabKey = new AssetKey(AssetConst.ACTOR_PREFAB);
                x_FieldEntity fieldEntity = await AssetProvider.GetOrNewEntityInstanceAsync<x_FieldEntity>(prefabKey, root);
                FieldUnitTableData data = FieldUnitTableProvider.GetData(index);
                FieldUnitAnimClipContext clip = await data.GetAnimClipsAsync();
                fieldEntity.Initialize(data, clip, aoc, mapQuery);
                
                return fieldEntity;
            }
        }
    }    
}