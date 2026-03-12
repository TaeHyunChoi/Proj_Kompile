namespace Script.Battle.Manager
{
    using System.Collections.Generic;
    using UnityEngine;
    using Script.Battle.Data;
    using Script.Battle.Entity;
    using Script.Battle.RepoProvider;
    using Script.Battle.Utility;


    public class BattleManager : MonoBehaviour
    {
        public const int TARGET_FPS = 24;
        public const int FRAMES_PER_TICK = 6;

        private BattleTimelineManager _timelineManager;
        private BattleSkillRepoProvider _skillProvider;

        // Instance-Centric 규칙: 인스턴스는 참조 및 식별을 위해 Dictionary 관리
        private Dictionary<long, BattleUnitEntity> _units = new Dictionary<long, BattleUnitEntity>();

        private bool _isWaitingForCombo = false;
        private long _activeAttackerID = -1; // Entity 레퍼런스 대신 ID 저장

        // (상상하여 작성된 부분) 유닛 등록 시 할당할 고유 ID 발급기
        private long _unitIDCounter = 0;

        private void Awake()
        {
            _timelineManager = new BattleTimelineManager();
            _skillProvider = new BattleSkillRepoProvider();
            _timelineManager.Play();
        }

        private void Update()
        {
            // 상위 Manager에서 Time을 컨트롤하며 하위 객체들을 Polling 업데이트
            if (_timelineManager.OnUpdate(Time.deltaTime, TARGET_FPS, FRAMES_PER_TICK))
            {
                ProcessBattleFrames();
            }
        }

        public void RegisterUnit(BattleUnitEntity unit)
        {
            long newID = ++_unitIDCounter;
            unit.EntityID = newID;
            _units.Add(newID, unit);

            unit.Animation.Init();
        }

        private void ProcessBattleFrames()
        {
            int currentFrame = _timelineManager.TotalFrames;

            // Dictionary 순회하며 Entity들의 상태 일괄 처리
            foreach (var kvp in _units)
            {
                var unit = kvp.Value;

                // C# 이벤트(Event) 대신 Manager가 판정 시점을 능동적으로 감지 (Polling)
                if (unit.Animation.CheckHitTriggered(currentFrame))
                {
                    HandleUnitHit(unit.EntityID);
                }

                unit.Animation.Sample(currentFrame, FRAMES_PER_TICK);
            }

            if (_isWaitingForCombo && _activeAttackerID != -1)
            {
                ExecuteComboSequence();
            }
        }

        private void HandleUnitHit(long attackerID)
        {
            _timelineManager.ApplyHitStop(0.15f, 0.05f);
            _isWaitingForCombo = true;
            _activeAttackerID = attackerID;
        }

        private void ExecuteComboSequence()
        {
            if (_units.TryGetValue(_activeAttackerID, out BattleUnitEntity attacker))
            {
                var currentCmd = attacker.Animation.CurrentCmd;

                // 수학적 판정은 Utility로 위임
                if (BattleUtil.IsInsideComboWindow(_timelineManager.TotalFrames, currentCmd))
                {
                    attacker.Animation.ForceStop();

                    // 값(Value) 생성은 Provider로 위임
                    BattleAnimationCommand nextCmd = _skillProvider.GetSkillCommand(1002, _timelineManager.TotalFrames);

                    attacker.Animation.Play(nextCmd);
                    _isWaitingForCombo = false;
                }
            }
        }
    }
}