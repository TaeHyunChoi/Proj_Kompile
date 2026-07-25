using System;

namespace Kompile.Manager
{
    using Data;
    using Provider;
    using Utility;
    using UnityEngine;
    using Unity.Mathematics;
    using Unity.Collections;
    using System.Collections.Generic;
    
    /// <summary> 맵 오브젝트 인스턴스 스폰, 시각적 트랜지션, 큐 기반 동기적 스트리밍 제어 (Instance-Centric) </summary>
    public class MapMgr : GameLogicMgrBase
    {
        private MapProvider _mapProvider;
        
        // --- Manage: Map ---
        private readonly Dictionary<int, List<MapChunkContext>> _spawnedMapObjects = new Dictionary<int, List<MapChunkContext>>();
        private Transform _rootTransform;
        
        
        public override Awaitable<bool> OnAwake()
        {
            throw new Exception();
        }

        public override void OnUpdate()
        {
            
        }
    }
}
