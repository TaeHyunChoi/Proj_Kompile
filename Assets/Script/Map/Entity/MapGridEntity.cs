namespace Script.Map.Entity
{
    using System.Collections.Generic;
    using UnityEngine;
    using Script.Global.Entity.Data;

    /// <summary>
    /// [Framework] 핵심 계층: Entity
    /// 기존 MapGridObject를 대체하는 맵 그리드 엔티티입니다.
    /// </summary>
    public class MapGridEntity : Entity
    {
        /// <summary> Key:LayerIndex, Value:해당 레이어의 부모 오브젝트 </summary>
        private Dictionary<int, GameObject> _layerRoots = new Dictionary<int, GameObject>();

        /// <summary> 로드한 메쉬들을 레이어별로 그룹화하여 계층 구조를 생성합니다. </summary>
        /// <param name="layerGroups">Key: LayerIndex, Value: 메쉬 리스트</param>
        public void Initialize(Dictionary<int, List<Mesh>> layerGroups)
        {
            // (pooling) 재사용 시 기존 자식을 제거
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
            _layerRoots.Clear();

            foreach (var kvp in layerGroups)
            {
                int layerIndex = kvp.Key;
                List<Mesh> meshes = kvp.Value;
                
                // 레이어 루트 생성 (해당 오브젝트를 껐다 켜서 필터링)
                GameObject layerRoot = new GameObject($"Layer_{layerIndex}");
                layerRoot.transform.SetParent(transform, false);
                
                _layerRoots[layerIndex] =  layerRoot;
                
                // root 아래의 mesh parts를 생성 및 부착
                for (int i = 0; i < meshes.Count; ++i)
                {
                    Mesh mesh = meshes[i];
                    GameObject meshPart = new GameObject(mesh.name);
                    meshPart.transform.SetParent(layerRoot.transform, false);
                    
                    MeshFilter mf = meshPart.AddComponent<MeshFilter>();
                    MeshRenderer mr = meshPart.AddComponent<MeshRenderer>();

                    mf.mesh = mesh;
                    
                    //TODO: 필요한 material 할당 로직이 있다면 추가
                    //ex. 쉐이더 설정?
                }
            }
        }

        /// <summary> 타겟 마스크에 포함된 레이어만 활성화 </summary>
        public void UpdateLayerVisibility(int targetLayerMask)
        {
            foreach (var kvp in _layerRoots)
            {
                int layerIndex = kvp.Key;
                GameObject root = kvp.Value;
                
                // TODO로 남겨두신 LayerMask 확인 로직을 비트 시프트 연산으로 해결했습니다.
                // targetLayerMask의 layerIndex번째 비트가 1인지 검사합니다.
                bool shouldShow = (targetLayerMask & (1 << layerIndex)) != 0;
                if (shouldShow != root.activeSelf)
                {
                    root.SetActive(shouldShow);
                }
            }
        }

        public void Dispose()
        {
            _layerRoots.Clear();
        }
    }
}