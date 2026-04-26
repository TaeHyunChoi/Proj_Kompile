using Kompile.Battle.Data;

namespace Kompile.Battle.Manager
{
    using UnityEngine;
    using Kompile.Battle.Utility;

    /// <summary> 현실 시간(deltaTime)을 누적해 논리 프레임을 굴리는 구동(Engine) 매니저 </summary>
    public class BattleTimelineManager
    {
        // --- 상태 관리 변수 ---
        private int _currentTotalFrames = 0;    // 게임 시작 후 지금까지 흐른 총 논리 프레임(누적 틱의 기초)
        private bool _isPlaying = false;         // 타임라인 재생 여부 (Pause 시 시간 정지)
        private float _accumulatedTime = 0f;    // 프레임으로 변환되지 못하고 남은 자투리 시간을 모아두는 '저금통'

        // --- 연출 및 제어 변수 ---
        private int _targetEventFrame = -1;     // 특정 프레임까지 빠르게 진행한 뒤 멈추고 싶을 때 사용하는 목표 지점
        private int _decelerationBuffer = 0;    // 목표 지점에 도달하기 전, 감속을 시작할 여유 프레임 구간
        private float _timePerFrame;
        private int _framesPerTick;

        // --- 히트스톱(Hit-Stop) 변수 ---
        private float _hitStopTimer = 0f;       // 히트스톱이 유지될 남은 시간
        private float _hitStopScale = 1.0f;     // 히트스톱 중 적용될 시간 배율 (보통 0에 가까운 값)

        private ITimelineHandler _timelineHandler;
        
        public BattleTimelineManager(ITimelineHandler handler, int targetFps, int framesPerTick)
        {
            _timelineHandler = handler;
            _timePerFrame    = 1.0f / targetFps;
            _framesPerTick   = framesPerTick;
        }

        // --- 이벤트 및 프로퍼티 ---
        public int TotalFrames => _currentTotalFrames; // 외부에서 현재 논리 시간을 참조하기 위한 통로

        /// <summary> deltaTime을 누적하여 틱이 갱신되었을 때만 true를 반환 </summary>
        public bool OnUpdateTick(float deltaTime)
        {
            // 1. 재생 중이 아니면 아무것도 하지 않음 (전투 일시정지 상태)
            if (false == _isPlaying)
            {
                return false;
            }

            bool isTickUpdated = false; // 이번 Update에서 논리 프레임이 1개라도 상승했는지 여부
            
            // 2. 현재 시간의 흐름 배율을 결정 (히트스톱이나 감속 연출 반영)
            float currentScale = DetermineCurrentTimeScale();

            // 3. '저금통'에 현실 시간 누적 (배율이 적용된 시간이 쌓임)
            _accumulatedTime += (deltaTime * currentScale);
            
            // 4. 목표하는 1프레임당 시간 (24 FPS라면 약 0.0416초)
            int processedFrames = 0; // 한 Update(한 프레임) 내에서 처리된 논리 프레임 수

            // 5. 저금통에 쌓인 시간이 1프레임의 시간보다 많다면, 그만큼 논리 프레임을 깎아서 전진시킴 (Accumulator 패턴)
            while (_accumulatedTime >= _timePerFrame)
            {
                _accumulatedTime -= _timePerFrame; // 1프레임치 시간을 소모
                ++_currentTotalFrames;            // 논리 프레임 카운트 증가
                isTickUpdated = true;            // 프레임 갱신이 일어났음을 기록
                ++processedFrames;                // 루프 횟수 기록

                // 6. 목표 프레임 기능이 켜져 있고, 그 지점에 도달했다면 즉시 정지
                if (_targetEventFrame > 0 && _currentTotalFrames >= _targetEventFrame)
                {
                    StopAtTarget();
                    break;
                }

                // 7. 과부하 방지 (Panic Threshold): 
                // 컴퓨터가 너무 느려 한 번에 너무 많은 프레임을 따라잡아야 할 경우, 
                // 게임이 멈추는 것을 막기 위해 '프레임 스킵'을 발생시키고 저금통을 비움.
                if (processedFrames >= _framesPerTick + 2)
                {
                    _accumulatedTime = 0f;
                    break;
                }
            }

            // 틱(Tick) 단위 갱신이 발생했는지 여부를 반환
            return isTickUpdated;
        }

        /// <summary> 현재 시간의 흐름 속도를 계산 </summary>
        private float DetermineCurrentTimeScale()
        {
            // 1순위: 히트스톱 처리
            if (0f < _hitStopTimer)
            {
                // 히트스톱은 현실 시간(unscaledDeltaTime) 기준으로 차감하여 정확한 초 단위 유지
                _hitStopTimer -= Time.unscaledDeltaTime;
                if (0f > _hitStopTimer)
                {
                    _hitStopTimer = 0f;
                }

                return _hitStopScale; // 히트스톱용 느린 배율 반환
            }

            // 2순위: 목표 지점이 설정된 경우의 감속 처리
            if (0 < _targetEventFrame)
            {
                int framesLeft = _targetEventFrame - _currentTotalFrames;
                // 유틸리티를 호출해 목표 지점까지 부드럽게 멈추기 위한 배율 계산 (배틀 연출용)
                return BattleUtil.GetRequiredTimeScale(8.0f, framesLeft, _decelerationBuffer);
            }

            // 평상시: 정속도(1.0배속)
            return 1f;
        }

        /// <summary> 특정 프레임까지 배속으로 진행한 뒤 멈추도록 요청 </summary>
        public void RequestFastForwardTo(int targetFrame, int bufferFrames = 12)
        {
            _targetEventFrame = targetFrame;
            _decelerationBuffer = bufferFrames;
            _isPlaying = true;
        }

        /// <summary> 타격감 연출을 위한 히트스톱 적용 </summary>
        public void ApplyHitStop(float durationSeconds, float scale = 0.1f)
        {
            _hitStopTimer = durationSeconds; // 지속 시간 설정
            _hitStopScale = scale;           // 멈춤 수준 설정 (0.0이면 완전 정지)
        }

        /// <summary> 목표 지점 도달 시 상태 초기화 </summary>
        private void StopAtTarget()
        {
            _isPlaying = false;
            _targetEventFrame = -1;
            _accumulatedTime = 0f;
            _timelineHandler.OnTargetFrameReached(); // 등록된 콜백(UI 닫기, 행동 개시 등) 실행
        }

        public void Play() => _isPlaying = true;
        public void Pause() => _isPlaying = false;
    }
}