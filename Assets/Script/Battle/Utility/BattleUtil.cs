namespace Script.Battle.Utility
{
    using UnityEngine;
    using Unity.Burst;
    using Script.Battle.Data;

    /// <summary> 전투 관련한 수학 연산 및 판정을 담당 (상태값을 가지지 않는다) </summary>
    [BurstCompile]
    public static class BattleUtil
    {
        public const float ORDER_PER_TICK = 10.0f;

        [BurstCompile]
        public static int CalculatorOrderToFrames(float orderDistance, int framePerTick)
        {
            float ticks = orderDistance / ORDER_PER_TICK;
            return Mathf.CeilToInt(ticks * framePerTick);
        }

        [BurstCompile]
        public static bool IsInsideComboWindow(int currentFrame, in BattleAnimationCommand cmd)
        {
            int hitFrame = cmd.StartFrame + cmd.HitFrameOffset;
            return hitFrame <= currentFrame && currentFrame <= (hitFrame + cmd.ComboWindow);
        }

        [BurstCompile]
        public static float GetRequiredTimeScale(float scale, int frameLeft, int buffer)
        {
            if (0 >= frameLeft)
            {
                return 1.0f;
            }

            return frameLeft > buffer ? scale : 1.0f;
        }
        
        [BurstCompile]
        public static float CalculateProgressPerTick(int currentSpeed)
        {
            return currentSpeed * ORDER_PER_TICK;
        }
        
        // [UI를 위한 잔여 틱 예측]
        public static int PredictRemainingTicks(float remainDist, float actionDist, int speed)
        {
            if (speed <= 0) return 999;
            float progress = CalculateProgressPerTick(speed);
            return Mathf.CeilToInt((remainDist + actionDist) / progress);
        }
    }
}
