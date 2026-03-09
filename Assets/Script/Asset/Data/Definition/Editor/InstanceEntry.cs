namespace Script.Asset.Provider
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.ResourceManagement.AsyncOperations;

    public static partial class AssetProvider
    {
        private class InstanceEntry
        {
            public AsyncOperationHandle<GameObject> Handle { get; }
            public Queue<GameObject> Pool { get; }
            public bool UsePooling { get; }
            public int ReferenceCount { get; private set; }

            public InstanceEntry(AsyncOperationHandle<GameObject> handle, bool usePooling)
            {
                Handle = handle;
                UsePooling = usePooling;
                Pool = new Queue<GameObject>();
                ReferenceCount = 0;
            }

            public bool HasPooledInstance()
            {
                while (Pool.Count > 0)
                {
                    if (Pool.Peek() != null) return true;
                    Pool.Dequeue();
                }
                return false;
            }

            public void AddReference() => ReferenceCount++;
            public void RemoveReference() => ReferenceCount = Mathf.Max(0, ReferenceCount - 1);
            public bool ShouldRelease() => ReferenceCount <= 0 && Pool.Count == 0;
        }    
    }
}



