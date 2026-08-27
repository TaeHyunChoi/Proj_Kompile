namespace Kompile.Domain
{
    using System.Collections.Generic;
    using Data;
    using Entities;
    using Utility;

    public class BattleManager : ITimelineHandler
    {
        public const int   TARGET_FPS      = 24;
        public const int   FRAMES_PER_TICK = 6;
        public const float TARGET_DISTANCE = 10000f;

        private BattleTimelineManager   _timelineManager;
        private BattleSkillRepoProvider _skillProvider;

        private Dictionary<long, EntityBase>    _units    = new Dictionary<long, EntityBase>();
        private Dictionary<long, BattleUnitContext> _contexts = new Dictionary<long, BattleUnitContext>();

        private long _unitIDCounter     = 0;
        private bool _isInterrupting    = false;
        private long _activeAttackerID  = -1;
        private bool _isWaitingForCombo = false;

        public BattleManager()
        {
            _timelineManager = new BattleTimelineManager(this, TARGET_FPS, FRAMES_PER_TICK);
            _skillProvider   = new BattleSkillRepoProvider();
            _timelineManager.Play();
        }

        public void Update(float deltaTime)
        {
            if (true == _timelineManager.OnUpdateTick(deltaTime))
            {
                int currentFrame = _timelineManager.TotalFrames;

                // [시각] 매 프레임 부드럽게 재생
                UpdateVisuals(currentFrame);

                // [논리] 6프레임마다 틱 연산
                if (currentFrame % FRAMES_PER_TICK == 0)
                {
                    ProcessBattleLogic();
                }
            }
        }

        private void UpdateVisuals(int currentFrame)
        {
            // foreach (var unit in _units.Values)
            // {
            //     unit.Animation.Sample(currentFrame, FRAMES_PER_TICK);
            // }
        }

        private void ProcessBattleLogic()
        {
            if (true == _isInterrupting)
            {
                return;
            }

            BattleUnitContext context;
            foreach (var kvp in _contexts)
            {
                context = kvp.Value;
                float progress = BattleUtil.CalculateProgressPerTick(context.CurrentSpeed);

                if (context.Phase == BattlePhase.Wait)
                {
                    context.RemainingDistance -= progress;
                    if (context.RemainingDistance <= 0f)
                    {
                        context.RemainingDistance = 0f;
                        OnUnitWaitPhaseEnded(context);
                    }
                }
                else if (context.Phase == BattlePhase.Action)
                {
                    context.ActionDistance -= progress;

                    if (context.ActionDistance <= 0f)
                    {
                        context.ActionDistance = 0f;
                        OnUnitActionPhaseEnded(context);
                    }
                }
            }
        }

        /// <summary> 인터럽트 발동 시 호출 </summary>
        public void TriggerInterrupt(long unitID, int interruptSkillID)
        {
            if (!_contexts.TryGetValue(unitID, out var context)) return;

            _activeAttackerID = unitID;
            _isInterrupting = true;

            // 강제로 행동 페이즈 전환
            context.RemainingDistance = 0f;
            context.Phase = BattlePhase.Action;
            context.ActionDistance = 1500f;

            // if (_units.TryGetValue(unitID, out var unit))
            // {
            //     var cmd = _skillProvider.GetSkillCommand(interruptSkillID, _timelineManager.TotalFrames);
            //     unit.Animation.Play(cmd);
            // }
        }

        private void OnUnitWaitPhaseEnded(BattleUnitContext context)
        {
            _activeAttackerID = context.EntityID;
            context.Phase = BattlePhase.Action;
            context.ActionDistance = 2000f;
        }

        private void OnUnitActionPhaseEnded(BattleUnitContext context)
        {
            if (_isInterrupting && _activeAttackerID == context.EntityID)
            {
                _isInterrupting = false;
                _activeAttackerID = -1;
            }

            context.Phase = BattlePhase.Wait;
            context.RemainingDistance = TARGET_DISTANCE;
        }

        // public void RegisterUnit(BattleUnitEntity unit, int baseSpeed)
        // {
        //     long newID = ++_unitIDCounter;
        //     unit.EntityID = newID;
        //     _units.Add(newID, unit);
        //     _contexts.Add(newID, new BattleUnitContext(newID, baseSpeed));
        //     unit.Animation.Init();
        // }

        public void OnTargetFrameReached()
        {
             /* 필요 시 구현 */
        }
    }
}
