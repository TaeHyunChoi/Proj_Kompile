namespace Kompile.Entities
{
    using UnityEngine;
    using Data;

    [RequireComponent(typeof(Animator))]
    public class BattleUnitAnimationComponent : MonoBehaviour
    {
        private Animator _animator;
        private BattleAnimationCommand _currentCmd;
        private bool _isActive = false;
        private int _lastSampleFrame = -1;

        public BattleAnimationCommand CurrentCmd => _currentCmd;

        public void Init()
        {
            _animator = GetComponent<Animator>();
            _animator.speed = 0f;
        }

        public void Play(BattleAnimationCommand cmd)
        {
            _currentCmd = cmd;
            _isActive = true;
            _lastSampleFrame = -1;
        }

        public void ForceStop()
        {
            _isActive = false;
        }

        // C# Event를 대체하는 폴링용 메서드
        public bool CheckHitTriggered(int currentFrame)
        {
            if (false == _isActive) return false;

            int hitTarget = _currentCmd.StartFrame + _currentCmd.HitFrameOffset;
            if (_lastSampleFrame < hitTarget && hitTarget <= currentFrame)
            {
                return true;
            }
            return false;
        }

        // fpt: frame per tick
        public void Sample(int currentFrame, int fpt)
        {
            if (false == _isActive)
            {
                return;
            }

            _lastSampleFrame = currentFrame;

            int elapsed = currentFrame - _currentCmd.StartFrame;
            int total = _currentCmd.TotalTicks * fpt;

            if (0 <= elapsed && elapsed < total)
            {
                _animator.Play(_currentCmd.StateHash, 0, (float)elapsed / total);
            }
            else if (elapsed >= total)
            {
                _isActive = false;
            }
        }
    }
}