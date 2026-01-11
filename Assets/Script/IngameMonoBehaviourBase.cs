namespace Script.Asset
{
    using UnityEngine;

    public abstract class IngameMonoBehaviourBase : MonoBehaviour
    {
        public abstract PrefabID PrefabID { get; }
    }
}