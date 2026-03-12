namespace Script.Battle.Entity
{
    using UnityEngine;

    public class BattleUnitEntity : MonoBehaviour
    {
        public long EntityID { get; set; }
        private BattleUnitAnimationComponent _anime;

        public BattleUnitAnimationComponent Animation => _anime;

        private void Awake()
        {
            _anime = GetComponent<BattleUnitAnimationComponent>();
        }
    }
}