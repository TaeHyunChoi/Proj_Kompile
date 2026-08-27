#if UNITY_EDITOR
namespace Kompile.Editor.Domain
{
    using System;
    using UnityEngine;

    /// <summary> 머티리얼 생성을 위해 텍스처 원본 참조를 일시적으로 보관합니다. </summary>
    public partial class EditMapSamplingProvider // BakeGroupKey
    {
        private struct EditBakeGroupKey : IEquatable<EditBakeGroupKey>
        {
            public ushort RenderLayer;
            public int GridKey;
            public string TopAtlas;
            public string SideAtlas;
            public Texture2D TopTexRef; // 머티리얼 자동 생성용 참조
            public Texture2D SideTexRef; // 머티리얼 자동 생성용 참조

            public bool Equals(EditBakeGroupKey other)
            {
                if (RenderLayer != other.RenderLayer) { return false; }

                if (GridKey != other.GridKey) { return false; }

                if (TopAtlas != other.TopAtlas) { return false; }

                if (SideAtlas != other.SideAtlas) { return false; }

                return true;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(RenderLayer, GridKey, TopAtlas, SideAtlas);
            }
        }
    }
}
#endif