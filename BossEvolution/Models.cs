using Terraria;

namespace BossEvolution
{
    /// <summary>
    /// 描述一个 Boss 技能模式（技能定义 + 执行逻辑）。
    /// </summary>
    public class BossSkillPattern
    {
        public int BossType { get; set; }
        public string SkillName { get; set; }
        public Action<NPC, Player> ExecuteSkill { get; set; }
        public int Difficulty { get; set; } // 1-5: 1 最简单，5 最难
    }

    /// <summary>
    /// 描述 Boss 的一个相位（阶段）：达到指定血量百分比后激活的技能集合。
    /// </summary>
    public class BossPhase
    {
        public float HealthPercentage { get; set; }
        public List<BossSkillPattern> PhaseSkills { get; set; }
    }
}
