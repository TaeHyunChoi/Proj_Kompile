namespace Kompile.Battle.RepoProvider
{
    using System.Collections.Generic;
    using UnityEngine;
    using Kompile.Battle.Data;

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
            int index = -1;
            for (int i = 0; i < _skillDatas.Count; i++)
            {
                if (_skillDatas[i].ID == skillID)
                {
                    index = i;
                    break;
                }
            }

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
