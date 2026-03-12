namespace Script.Battle.Manager
{
    using System;
    using UnityEngine;
    using Script.Battle.Utility;

    /// <summary> 현실 시간(deltaTime)을 누적해 논리 프레임을 굴리는 구동(Engine) 매니저 </summary>
    public class BattleTimelineManager
    {
        private int _currentTotalFrames = 0;
        private bool _isPlaying = false;
        private float _accumulatedTime = 0f;

        private int _targetEventFrame = -1;
        private int _decelerationBuffer = 0;

        private float _hitStopTimer = 0f;
        private float _hitStopScale = 1.0f;

        public Action OnTargetFrameReached;
        public int TotalFrames => _currentTotalFrames;

        public bool OnUpdate(float deltaTime, int targetFps, int framesPerTick)
        {
            if (false == _isPlaying)
            {
                return false;
            }

            bool isFrameUpdated = false;
            float currentScale = DetermineCurrentTimeScale();

            _accumulatedTime += (deltaTime * currentScale);
            float timePerFrame = 1.0f / targetFps;
            int processedFrames = 0;

            while (_accumulatedTime >= timePerFrame)
            {
                _accumulatedTime -= timePerFrame;
                ++_currentTotalFrames;
                isFrameUpdated = true;
                ++processedFrames;

                if (_targetEventFrame > 0 && _currentTotalFrames >= _targetEventFrame)
                {
                    StopAtTarget();
                    break;
                }

                if (processedFrames >= framesPerTick + 2)
                {
                    _accumulatedTime = 0f;
                    break;
                }
            }

            return isFrameUpdated;
        }

        private float DetermineCurrentTimeScale()
        {
            if (0f < _hitStopTimer)
            {
                _hitStopTimer -= Time.unscaledDeltaTime;
                if (0f > _hitStopTimer)
                {
                    _hitStopTimer = 0f;
                }

                return _hitStopScale;
            }

            if (0 < _targetEventFrame)
            {
                int framesLeft = _targetEventFrame - _currentTotalFrames;
                return BattleUtil.GetRequiredTimeScale(8.0f, framesLeft, _decelerationBuffer);
            }

            return 1f;
        }

        public void RequestFastForwardTo(int targetFrame, int bufferFrames = 12)
        {
            _targetEventFrame = targetFrame;
            _decelerationBuffer = bufferFrames;
            _isPlaying = true;
        }

        public void ApplyHitStop(float durationSeconds, float scale = 0.1f)
        {
            _hitStopTimer = durationSeconds;
            _hitStopScale = scale;
        }

        private void StopAtTarget()
        {
            _isPlaying = false;
            _targetEventFrame = -1;
            _accumulatedTime = 0f;
            OnTargetFrameReached?.Invoke();
        }

        public void Play()
        {
            _isPlaying = true;
        }
    }
}