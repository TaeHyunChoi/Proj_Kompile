using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine;

namespace Script.Index
{
    public readonly struct IngameAsset_t
    {
        public readonly AssetCode Code;
        public readonly AsyncOperationHandle Handle;

        public IngameAsset_t(AssetCode code, AsyncOperationHandle handle)
        {
            Code = code;
            Handle = handle;
        }
        public T GetComponent<T>() where T : MonoBehaviour
        {
            return (Handle.Result as GameObject).GetComponent<T>();
        }
        public T AddComponent<T>() where T : MonoBehaviour
        {
            return (Handle.Result as GameObject).AddComponent<T>();
        }
        public void Dispose()
        {
            if (Handle.Result is GameObject)
            {
                if (false == Addressables.ReleaseInstance(Handle))
                {
                    // error?
                }
            }
            else
            {
                Handle.Release();
            }
        }
    }
}
