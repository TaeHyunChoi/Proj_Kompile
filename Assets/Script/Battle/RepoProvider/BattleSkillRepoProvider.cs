namespace Script.Battle.RepoProvider
{
    using System.Collections.Generic;
    using UnityEngine;
    using Script.Battle.Data;

    public class BattleSkillRepoProvider
    {
        private List<BattleSkillData> _skillDatas = new List<BattleSkillData>();

        public void LoadSkillDatas(List<BattleSkillData> rawDatas)
        {
            _skillDatas.Clear();
            _skillDatas.AddRange(rawDatas);
        }

        public BattleAnimationCommand GetSkillCommand(int skillID, int currentTotalFrames)
        {
            int index = _skillDatas.FindIndex(x => x.ID == skillID);
            if (index < 0)
            {
                return new BattleAnimationCommand { StartFrame = currentTotalFrames };
            }

            BattleSkillData rawData = _skillDatas[index];
            return new BattleAnimationCommand()
            {
                StateHash = Animator.StringToHash(rawData.AnimationStateName),
                StartFrame = currentTotalFrames,
                StartupTicks = rawData.StartupTicks,
                ActiveTicks = rawData.ActiveTicks,
                RecoveryTicks = rawData.RecoveryTicks,
                HitFrameOffset = rawData.HitFrameOffset,
                ComboWindow = rawData.ComboWindow
            };
        }
    }
}
