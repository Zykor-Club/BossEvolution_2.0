using Terraria;
using Terraria.ID;
using Microsoft.Xna.Framework;
using TShockAPI;

namespace BossEvolution
{
    /// <summary>
    /// 技能执行部分：主循环更新、技能随机化、相位管理、冷却管理、昵称生成。
    /// </summary>
    public partial class BossEvolutionPlugin
    {
        private void OnNPCAI(EventArgs args)
        {
            try
            {
                foreach (NPC npc in Main.npc)
                {
                    if (npc.boss && npc.active)
                    {
                        Player target = Main.player[npc.target];
                        UpdateCooldowns(npc);
                        ApplySkills(npc, target);
                    }
                }
            }
            catch (Exception)
            {
                // 吞掉异常以避免单次技能执行错误导致主循环崩溃
            }
        }

        private void RandomizeSkills(NPC boss)
        {
            if (currentSkills.ContainsKey(boss.whoAmI))
            {
                currentSkills.Remove(boss.whoAmI);
                skillCooldowns.Remove(boss.whoAmI);
                customAI.Remove(boss.whoAmI);
                bossPhases.Remove(boss.whoAmI);
            }

            var allBosses = GetBossList().Where(x => x != boss.type).ToList();

            bool isHardmodeBoss = IsHardmodeBoss(boss.type);

            int phase1Skills = 5;
            int phase2Skills = isHardmodeBoss ? 6 : 6;
            int phase3Skills = isHardmodeBoss ? 8 : 7;

            var phase1 = RandomizePhaseSkills(allBosses, phase1Skills);
            var phase2 = RandomizePhaseSkills(allBosses, phase2Skills);
            var phase3 = RandomizePhaseSkills(allBosses, phase3Skills);

            var allSkills = new List<BossSkillPattern>();
            allSkills.AddRange(phase1);
            allSkills.AddRange(phase2);
            allSkills.AddRange(phase3);

            currentSkills.Add(boss.whoAmI, allSkills.Select(s => s.BossType).ToArray());
            customAI.Add(boss.whoAmI, new float[4]);

            bossPhases[boss.whoAmI] = new List<BossPhase>
            {
                new BossPhase {
                    HealthPercentage = 1.0f,
                    PhaseSkills = phase1
                },
                new BossPhase {
                    HealthPercentage = PHASE2_HP,
                    PhaseSkills = phase2
                },
                new BossPhase {
                    HealthPercentage = PHASE3_HP,
                    PhaseSkills = phase3
                }
            };

            skillCooldowns.Add(boss.whoAmI, new int[isHardmodeBoss ? 8 : 7]);

            bool hasFlyingSkill = phase1.Any(s => flyingBosses.Contains(s.BossType)) ||
                                 phase2.Any(s => flyingBosses.Contains(s.BossType)) ||
                                 phase3.Any(s => flyingBosses.Contains(s.BossType));

            bool hasSummonSkill = phase1.Any(s => summonerBosses.Contains(s.BossType)) ||
                                 phase2.Any(s => summonerBosses.Contains(s.BossType)) ||
                                 phase3.Any(s => summonerBosses.Contains(s.BossType));

            customAI[boss.whoAmI] = new float[4] {
                hasFlyingSkill ? 1f : 0f,  // AI[0]: 可飞行
                hasSummonSkill ? 1f : 0f,  // AI[1]: 可控制 minion
                0f,                        // AI[2]: 保留
                0f                         // AI[3]: 保留
            };
        }

        private List<BossSkillPattern> RandomizePhaseSkills(List<int> bossList, int skillCount)
        {
            var skills = new List<BossSkillPattern>();
            for (int i = 0; i < skillCount; i++)
            {
                int randomBossIndex = random.Next(bossList.Count);
                int bossType = bossList[randomBossIndex];

                if (bossSkillPatterns.ContainsKey(bossType))
                {
                    var bossSkills = bossSkillPatterns[bossType];
                    if (bossSkills.Count > 0)
                    {
                        // Wall of Flesh 不能使用位移类技能
                        var availableSkills = bossSkills;
                        if (bossType == NPCID.WallofFlesh)
                        {
                            availableSkills = bossSkills.Where(s =>
                                !s.SkillName.Contains("Teleport") &&
                                !s.SkillName.Contains("Dash") &&
                                !s.SkillName.Contains("Jump") &&
                                !s.SkillName.Contains("Charge")
                            ).ToList();
                        }

                        if (availableSkills.Count > 0)
                        {
                            int randomSkillIndex = random.Next(availableSkills.Count);
                            skills.Add(availableSkills[randomSkillIndex]);
                        }
                    }
                }
            }
            return skills;
        }

        private void UpdateCooldowns(NPC boss)
        {
            if (!skillCooldowns.ContainsKey(boss.whoAmI)) return;

            var cooldowns = skillCooldowns[boss.whoAmI];
            for (int i = 0; i < cooldowns.Length; i++)
            {
                if (cooldowns[i] > 0)
                {
                    cooldowns[i]--;
                }
            }
        }

        private void ApplySkills(NPC boss, Player target)
        {
            if (!bossPhases.ContainsKey(boss.whoAmI)) return;
            if (target == null || !target.active || target.dead) return;

            float healthPercentage = (float)boss.life / boss.lifeMax;
            float distanceToTarget = Vector2.Distance(boss.Center, target.Center);

            // 根据 HP 取当前相位（血量百分比最高的那个满足条件的相位）
            var currentPhase = bossPhases[boss.whoAmI]
                .OrderByDescending(p => p.HealthPercentage)
                .FirstOrDefault(p => healthPercentage <= p.HealthPercentage);

            if (currentPhase == null) return;

            for (int i = 0; i < currentPhase.PhaseSkills.Count; i++)
            {
                if (!skillCooldowns.ContainsKey(boss.whoAmI)) continue;

                var cooldowns = skillCooldowns[boss.whoAmI];
                if (i >= cooldowns.Length || cooldowns[i] > 0) continue;

                var skill = currentPhase.PhaseSkills[i];
                int baseChance = 180; // 基础 1/180 每帧

                // HP 越低，触发越频繁
                if (healthPercentage < 0.5f) baseChance = (int)(baseChance * 0.8f);
                if (healthPercentage < 0.3f) baseChance = (int)(baseChance * 0.7f);

                // 中距离增加触发率
                if (distanceToTarget > 300 && distanceToTarget < 600)
                {
                    baseChance = (int)(baseChance * 0.9f);
                }

                // 相位越高，触发越频繁
                if (healthPercentage <= PHASE2_HP) baseChance = (int)(baseChance * 0.85f);
                if (healthPercentage <= PHASE3_HP) baseChance = (int)(baseChance * 0.7f);

                // 技能难度越高，触发越频繁
                baseChance = (int)(baseChance * (1f - skill.Difficulty * 0.1f));

                if (Main.rand.Next(baseChance) == 0)
                {
                    Console.WriteLine($"[Boss Evolution] {bossNicknames[boss.whoAmI]} uses {skill.SkillName}!");
                    Console.WriteLine($"[Boss Evolution] Chance: 1/{baseChance} (HP: {healthPercentage*100}%, Distance: {distanceToTarget})");
                    skill.ExecuteSkill(boss, target);
                    cooldowns[i] = COOLDOWN_TIME;

                    // HP 低于 30% 时缩短冷却
                    if (healthPercentage < 0.3f) cooldowns[i] = (int)(COOLDOWN_TIME * 0.7f);
                    break;
                }
            }

            // 若 Boss 本身不会飞但获得了飞行技能，则允许高跳
            if (!flyingBosses.Contains(boss.type) && customAI[boss.whoAmI][0] == 1f)
            {
                if (boss.velocity.Y == 0 && Main.rand.Next(120) == 0)
                {
                    boss.velocity.Y = -12f;
                    Console.WriteLine($"[Boss Evolution] {bossNicknames[boss.whoAmI]} performs a high jump!");
                }
            }

            // minion 控制逻辑
            if (customAI[boss.whoAmI][1] == 1f)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC minion = Main.npc[i];
                    if (!minion.active) continue;

                    if (IsMinion(minion.type) && Vector2.Distance(boss.Center, minion.Center) < 800f)
                    {
                        minion.ai[0] = 1;
                        minion.ai[1] = target.whoAmI;

                        Vector2 direction = target.Center - minion.Center;
                        direction = direction.SafeNormalize(Vector2.Zero);

                        float speed = 8f;
                        if (minion.type == NPCID.Probe) speed = 10f;
                        if (minion.type == NPCID.ServantofCthulhu) speed = 12f;

                        minion.velocity = direction * speed;

                        TSPlayer.All.SendData(PacketTypes.NpcUpdate, "", i);

                        if (Main.rand.Next(30) == 0)
                        {
                            int dustType = DustID.PurpleTorch;
                            if (minion.type == NPCID.Probe) dustType = DustID.Electric;
                            if (minion.type == NPCID.ServantofCthulhu) dustType = DustID.Blood;

                            Dust.NewDust(minion.position, minion.width, minion.height, dustType);
                        }
                    }
                }
            }
        }

        private void GenerateBossNickname(NPC boss, int[] selectedSkills)
        {
            try
            {
                string originalName = Lang.GetNPCNameValue(boss.type);
                var skillNames = selectedSkills.Select(type => Lang.GetNPCNameValue(type)).ToList();

                string[] prefixes = {
                    "Mutant", "Evolved", "Hybrid", "Corrupted", "Ascended",
                    "Legendary", "Divine", "Immortal", "Supreme", "Ancient",
                    "Chaotic", "Mystic", "Cosmic", "Primal", "Twisted"
                };

                string[] suffixes = {
                    $"of {skillNames[0]}",
                    $"with Power of {skillNames[1]}",
                    $"Fusion with {skillNames[2]}",
                    $"Inheritor of {skillNames[3]}",
                    $"Combined with {skillNames[0]}",
                    $"Wielding {skillNames[1]}'s Might",
                    $"Infused by {skillNames[2]}'s Essence",
                    $"Enhanced by {skillNames[3]}'s DNA"
                };

                string nickname = $"{prefixes[random.Next(prefixes.Length)]} {originalName} {suffixes[random.Next(suffixes.Length)]}";

                if (bossNicknames.ContainsKey(boss.whoAmI))
                {
                    bossNicknames[boss.whoAmI] = nickname;
                }
                else
                {
                    bossNicknames.Add(boss.whoAmI, nickname);
                }
            }
            catch (Exception)
            {
                // selectedSkills 长度不足时索引越界，吞掉异常
            }
        }
    }
}
