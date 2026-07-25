using TerrariaApi.Server;
using Terraria;

namespace BossEvolution
{
    /// <summary>
    /// Boss Evolution 插件主入口。使用 partial class 将逻辑分散到多个文件：
    /// - Plugin.cs：入口、Hook 注册、生命周期、共享字段
    /// - SkillRegistry.cs：技能定义注册
    /// - BossSkillExecutor.cs：技能执行、相位管理、随机化
    /// </summary>
    [ApiVersion(2, 1)]
    public partial class BossEvolutionPlugin : TerrariaPlugin
    {
        // 共享状态字段（所有 partial 部分均可访问）
        private static readonly Random random = new Random();
        private Dictionary<int, int[]> currentSkills = new Dictionary<int, int[]>();
        private Dictionary<int, int[]> skillCooldowns = new Dictionary<int, int[]>();
        private const int COOLDOWN_TIME = 30; // 0.5 秒（30 帧）
        private Dictionary<int, string> bossNicknames = new Dictionary<int, string>();
        private Dictionary<int, List<BossSkillPattern>> bossSkillPatterns = new Dictionary<int, List<BossSkillPattern>>();
        private Dictionary<int, List<BossPhase>> bossPhases = new Dictionary<int, List<BossPhase>>();
        private Dictionary<int, float[]> customAI = new Dictionary<int, float[]>();
        private const float PHASE2_HP = 0.5f; // 50% HP
        private const float PHASE3_HP = 0.3f; // 30% HP

        public override string Name => "Boss Evolution";
        public override string Author => "GILX_DevTERRARIAVUI";
        public override string Description => "Allows bosses to evolve with random skills";

        public BossEvolutionPlugin(Main game) : base(game)
        {
        }

        public override void Initialize()
        {
            ServerApi.Hooks.NpcSpawn.Register(this, OnNPCSpawn);
            ServerApi.Hooks.GameUpdate.Register(this, OnNPCAI);
            ServerApi.Hooks.NpcKilled.Register(this, OnNPCKilled);
            InitializeBossSkills();
        }

        private void OnNPCSpawn(NpcSpawnEventArgs args)
        {
            NPC npc = Main.npc[args.NpcId];
            if (npc.boss)
            {
                RandomizeSkills(npc);
                if (!currentSkills.ContainsKey(npc.whoAmI))
                {
                    currentSkills.Add(npc.whoAmI, new int[8]); // 最多 8 个技能
                }
                GenerateBossNickname(npc, currentSkills[npc.whoAmI]);

                // Bug 8 修复：在 Boss 生成时设置 npc.value（此时生效），而非在 OnNPCKilled 中修改（已无效）。
                // 难度倍率基于 RandomizeSkills 已填充的 bossPhases 计算。
                ApplyDifficultyValueMultiplier(npc);
            }
        }

        /// <summary>
        /// 根据技能平均难度调整 Boss 的金钱掉落价值。
        /// 必须在 Boss 生成时调用（npc.value 在生成时确定掉落物价值）。
        /// </summary>
        private void ApplyDifficultyValueMultiplier(NPC npc)
        {
            if (!bossPhases.ContainsKey(npc.whoAmI)) return;

            float totalDifficulty = 0;
            int skillCount = 0;
            foreach (var phase in bossPhases[npc.whoAmI])
            {
                foreach (var skill in phase.PhaseSkills)
                {
                    totalDifficulty += skill.Difficulty;
                    skillCount++;
                }
            }

            if (skillCount > 0)
            {
                float avgDifficulty = totalDifficulty / skillCount;
                float multiplier = 1.0f + (avgDifficulty * 0.5f); // 难度 5 → x3.5 金钱
                npc.value *= multiplier;
            }
        }

        private void OnNPCKilled(NpcKilledEventArgs args)
        {
            NPC npc = Main.npc[args.npc.whoAmI];
            if (npc.boss)
            {
                // 清理 Boss 数据
                currentSkills.Remove(npc.whoAmI);
                skillCooldowns.Remove(npc.whoAmI);
                customAI.Remove(npc.whoAmI);
                bossPhases.Remove(npc.whoAmI);
                bossNicknames.Remove(npc.whoAmI);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NpcSpawn.Deregister(this, OnNPCSpawn);
                ServerApi.Hooks.GameUpdate.Deregister(this, OnNPCAI);
                ServerApi.Hooks.NpcKilled.Deregister(this, OnNPCKilled);
                bossNicknames.Clear();
                currentSkills.Clear();
                skillCooldowns.Clear();
                customAI.Clear();
                bossPhases.Clear();
                bossSkillPatterns.Clear();
            }
            base.Dispose(disposing);
        }
    }
}
