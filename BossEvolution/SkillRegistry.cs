using Terraria;
using Terraria.ID;
using Microsoft.Xna.Framework;
using TShockAPI;
using Terraria.DataStructures;

namespace BossEvolution
{
    /// <summary>
    /// 技能注册部分：定义所有 Boss 的技能模式、Boss 分类集合。
    /// </summary>
    public partial class BossEvolutionPlugin
    {
        // 会飞的 Boss 集合
        private readonly HashSet<int> flyingBosses = new HashSet<int>
        {
            NPCID.EyeofCthulhu,
            NPCID.QueenBee,
            NPCID.TheDestroyer,
            NPCID.Spazmatism,
            NPCID.Retinazer,
            NPCID.DukeFishron,
            NPCID.MoonLordHead,
            NPCID.HallowBoss,     // Empress of Light
            NPCID.QueenSlimeBoss, // Queen Slime
            NPCID.DD2Betsy,
            NPCID.PirateShip,
            NPCID.IceQueen,
            NPCID.MartianSaucer
        };

        // 召唤型 Boss 集合
        private readonly HashSet<int> summonerBosses = new HashSet<int>
        {
            NPCID.BrainofCthulhu,  // Creeper
            NPCID.QueenBee,        // Bees
            NPCID.TheDestroyer,    // Probes
            NPCID.Plantera,        // Tentacles
            NPCID.Golem,           // Fists
            NPCID.DukeFishron,     // Sharkrons
            NPCID.DD2DarkMageT1,
            NPCID.Pumpking,
            NPCID.SantaNK1
        };

        private void InitializeBossSkills()
        {
            // Bug 9 修复：移除从未被调用的 DetectSkill 字段及 detector 参数，仅保留执行器。
            void AddBossSkill(int bossType, string skillName, int difficulty,
                Action<NPC, Player> executor)
            {
                if (!bossSkillPatterns.ContainsKey(bossType))
                {
                    bossSkillPatterns[bossType] = new List<BossSkillPattern>();
                }
                bossSkillPatterns[bossType].Add(new BossSkillPattern
                {
                    BossType = bossType,
                    SkillName = skillName,
                    Difficulty = difficulty,
                    ExecuteSkill = executor
                });
            }

            // King Slime - Royal Teleport
            AddBossSkill(NPCID.KingSlime, "Royal Teleport", 2,
                (npc, target) => {
                    npc.position = target.Center + new Vector2(0, -200);
                    for (int i = 0; i < 50; i++) {
                        Dust.NewDust(npc.position, npc.width, npc.height, DustID.BlueTorch);
                    }
                });

            // Eye of Cthulhu - Dash Strike
            AddBossSkill(NPCID.EyeofCthulhu, "Dash Strike", 3,
                (npc, target) => {
                    Vector2 direction = target.Center - npc.Center;
                    direction = direction.SafeNormalize(Vector2.Zero);
                    npc.velocity = direction * 20f;

                    for (int i = 0; i < 20; i++) {
                        Dust.NewDust(npc.position, npc.width, npc.height, DustID.Blood);
                    }
                });

            // Brain of Cthulhu - Clone Army
            AddBossSkill(NPCID.BrainofCthulhu, "Clone Army", 4,
                (npc, target) => {
                    for (int i = 0; i < 4; i++) {
                        Vector2 position = target.Center + new Vector2(Main.rand.Next(-200, 200), Main.rand.Next(-200, 200));
                        // Bug 4 修复：使用 EntitySource_SpawnNPC 替代 null
                        int npcId = NPC.NewNPC(new EntitySource_SpawnNPC(), (int)position.X, (int)position.Y, NPCID.Creeper, 0);

                        Main.npc[npcId].realLife = npc.whoAmI;
                        Main.npc[npcId].defense = npc.defense / 2;

                        TSPlayer.All.SendData(PacketTypes.NpcUpdate, "", npcId);
                    }
                });

            // Queen Bee - Stinger Storm
            AddBossSkill(NPCID.QueenBee, "Stinger Storm", 3,
                (npc, target) => {
                    for (int i = 0; i < 8; i++) {
                        Vector2 direction = target.Center - npc.Center;
                        direction = direction.SafeNormalize(Vector2.Zero);

                        float spread = (float)(Main.rand.NextDouble() * 0.6 - 0.3);
                        direction = direction.RotatedBy(spread);

                        Vector2 velocity = direction * 12f;
                        // Bug 5 修复：使用 EntitySource_Parent(npc) 替代 null
                        int projId = Projectile.NewProjectile(new EntitySource_Parent(npc), npc.Center, velocity, ProjectileID.Stinger, 20, 2f, Main.myPlayer);

                        Main.projectile[projId].hostile = true;
                        Main.projectile[projId].friendly = false;
                        Main.projectile[projId].owner = Main.myPlayer;

                        TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", projId);
                    }
                });

            // Skeletron - Bone Throw
            AddBossSkill(NPCID.SkeletronHead, "Bone Throw", 3,
                (npc, target) => {
                    for (int i = 0; i < 5; i++) {
                        Vector2 velocity = new Vector2(Main.rand.Next(-10, 11), Main.rand.Next(-10, 11));
                        velocity = velocity.SafeNormalize(Vector2.Zero) * 15f;
                        int projId = Projectile.NewProjectile(new EntitySource_Parent(npc), npc.Center, velocity, ProjectileID.BoneGloveProj, 25, 2f, Main.myPlayer);

                        Main.projectile[projId].hostile = true;
                        Main.projectile[projId].friendly = false;
                        Main.projectile[projId].owner = Main.myPlayer;

                        TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", projId);
                    }
                });

            // Wall of Flesh - Laser Barrage
            AddBossSkill(NPCID.WallofFlesh, "Laser Barrage", 4,
                (npc, target) => {
                    for (int i = 0; i < 3; i++) {
                        Vector2 position = npc.Center + new Vector2(0, Main.rand.Next(-200, 201));
                        Vector2 direction = target.Center - position;
                        direction = direction.SafeNormalize(Vector2.Zero);
                        Vector2 velocity = direction * 12f;

                        int projId = Projectile.NewProjectile(new EntitySource_Parent(npc), position, velocity, ProjectileID.EyeLaser, 30, 2f, Main.myPlayer);

                        Main.projectile[projId].hostile = true;
                        Main.projectile[projId].friendly = false;
                        Main.projectile[projId].owner = Main.myPlayer;

                        TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", projId);
                    }
                });

            // The Destroyer - Probe Spawn
            AddBossSkill(NPCID.TheDestroyer, "Probe Spawn", 4,
                (npc, target) => {
                    for (int i = 0; i < 2; i++) {
                        int npcId = NPC.NewNPC(new EntitySource_SpawnNPC(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.Probe, 0);
                        TSPlayer.All.SendData(PacketTypes.NpcUpdate, "", npcId);
                    }
                });

            // The Twins - Death Laser
            AddBossSkill(NPCID.Retinazer, "Death Laser", 5,
                (npc, target) => {
                    for (int i = 0; i < 3; i++) {
                        Vector2 direction = target.Center - npc.Center;
                        direction = direction.SafeNormalize(Vector2.Zero);

                        float spread = (float)(Main.rand.NextDouble() * 0.4 - 0.2);
                        direction = direction.RotatedBy(spread);

                        Vector2 velocity = direction * 15f;
                        int projId = Projectile.NewProjectile(new EntitySource_Parent(npc), npc.Center, velocity, ProjectileID.DeathLaser, 35, 2f, Main.myPlayer);

                        Main.projectile[projId].hostile = true;
                        Main.projectile[projId].friendly = false;
                        Main.projectile[projId].owner = Main.myPlayer;

                        TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", projId);
                    }
                });

            // Plantera - Seed Barrage
            AddBossSkill(NPCID.Plantera, "Seed Barrage", 4,
                (npc, target) => {
                    for (int i = 0; i < 8; i++) {
                        Vector2 velocity = new Vector2(Main.rand.Next(-10, 11), Main.rand.Next(-10, 11));
                        velocity = velocity.SafeNormalize(Vector2.Zero) * 14f;
                        int projId = Projectile.NewProjectile(new EntitySource_Parent(npc), npc.Center, velocity, ProjectileID.SeedPlantera, 25, 2f, Main.myPlayer);
                        TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", projId);
                    }
                });

            // Duke Fishron - Bubble Attack
            AddBossSkill(NPCID.DukeFishron, "Bubble Attack", 5,
                (npc, target) => {
                    for (int i = 0; i < 6; i++) {
                        Vector2 velocity = new Vector2(Main.rand.Next(-10, 11), Main.rand.Next(-10, 11));
                        velocity = velocity.SafeNormalize(Vector2.Zero) * 16f;
                        int projId = Projectile.NewProjectile(new EntitySource_Parent(npc), npc.Center, velocity, ProjectileID.Bubble, 30, 2f, Main.myPlayer);
                        TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", projId);
                    }
                });

            // Moon Lord - Phantasmal Sphere
            AddBossSkill(NPCID.MoonLordHead, "Phantasmal Sphere", 5,
                (npc, target) => {
                    for (int i = 0; i < 3; i++) {
                        Vector2 direction = target.Center - npc.Center;
                        direction = direction.SafeNormalize(Vector2.Zero);

                        float spread = (float)(Main.rand.NextDouble() * 0.3 - 0.15);
                        direction = direction.RotatedBy(spread);

                        Vector2 velocity = direction * 10f;
                        int projId = Projectile.NewProjectile(new EntitySource_Parent(npc), npc.Center, velocity, ProjectileID.PhantasmalSphere, 40, 2f, Main.myPlayer);

                        Main.projectile[projId].hostile = true;
                        Main.projectile[projId].friendly = false;
                        Main.projectile[projId].owner = Main.myPlayer;

                        TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", projId);
                    }
                });

            // King Slime - Royal Dash
            // Bug 7 修复：原代码在游戏主循环中调用 Thread.Sleep(10) 会卡住整个服务器主线程。
            // 改为单次冲刺（原循环 3 次仅靠覆盖 velocity 实际等效一次），保留冲刺与特效，不再阻塞主线程。
            AddBossSkill(NPCID.KingSlime, "Royal Dash", 3,
                (npc, target) => {
                    Vector2 direction = target.Center - npc.Center;
                    direction = direction.SafeNormalize(Vector2.Zero);
                    npc.velocity = direction * 18f;

                    for (int d = 0; d < 15; d++) {
                        Dust.NewDust(npc.position, npc.width, npc.height, DustID.BlueTorch);
                    }
                });

            // Duke Fishron - Shark Charge
            AddBossSkill(NPCID.DukeFishron, "Shark Charge", 4,
                (npc, target) => {
                    Vector2 targetPos = target.Center + target.velocity * 20f;
                    Vector2 direction = targetPos - npc.Center;
                    direction = direction.SafeNormalize(Vector2.Zero);
                    npc.velocity = direction * 25f;

                    for (int i = 0; i < 10; i++) {
                        int projId = Projectile.NewProjectile(new EntitySource_Parent(npc), npc.Center, Vector2.Zero, ProjectileID.Bubble, 20, 2f, Main.myPlayer);
                        TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", projId);
                    }
                });

            // Moon Lord - Cosmic Dash
            AddBossSkill(NPCID.MoonLordHead, "Cosmic Dash", 5,
                (npc, target) => {
                    Vector2 behindPlayer = target.Center - Vector2.Normalize(target.velocity) * 200f;
                    npc.Center = behindPlayer;

                    Vector2 direction = target.Center - npc.Center;
                    direction = direction.SafeNormalize(Vector2.Zero);
                    npc.velocity = direction * 22f;

                    for (int i = 0; i < 30; i++) {
                        Dust.NewDust(npc.position, npc.width, npc.height, DustID.PurpleTorch);
                    }
                });

            // Deerclops - Ice Spike Storm
            AddBossSkill(NPCID.Deerclops, "Ice Spike Storm", 4,
                (npc, target) => {
                    for (int i = 0; i < 5; i++) {
                        Vector2 position = target.Center + new Vector2(Main.rand.Next(-200, 201), -600);
                        Vector2 velocity = new Vector2(Main.rand.Next(-3, 4), 12f);
                        int projId = Projectile.NewProjectile(new EntitySource_Parent(npc), position, velocity, ProjectileID.IceSickle, 30, 2f, Main.myPlayer);
                        Main.projectile[projId].hostile = true;
                        Main.projectile[projId].friendly = false;
                        TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", projId);
                    }
                });

            // King Slime - Crystal Rain
            AddBossSkill(NPCID.KingSlime, "Crystal Rain", 4,
                (npc, target) => {
                    for (int i = 0; i < 8; i++) {
                        Vector2 velocity = new Vector2(Main.rand.Next(-10, 11), Main.rand.Next(-10, 11));
                        velocity = velocity.SafeNormalize(Vector2.Zero) * 14f;
                        int projId = Projectile.NewProjectile(new EntitySource_Parent(npc), npc.Center, velocity, ProjectileID.CrystalShard, 25, 2f, Main.myPlayer);
                        Main.projectile[projId].hostile = true;
                        Main.projectile[projId].friendly = false;
                        TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", projId);
                    }
                });

            // Dark Mage - Dark Energy
            AddBossSkill(NPCID.DD2DarkMageT1, "Dark Energy", 3,
                (npc, target) => {
                    Vector2 direction = target.Center - npc.Center;
                    direction = direction.SafeNormalize(Vector2.Zero);
                    int projId = Projectile.NewProjectile(new EntitySource_Parent(npc), npc.Center, direction * 12f, ProjectileID.DD2DarkMageHeal, 25, 2f, Main.myPlayer);
                    Main.projectile[projId].hostile = true;
                    Main.projectile[projId].friendly = false;
                    TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", projId);
                });

            // Betsy - Dragon Breath
            AddBossSkill(NPCID.DD2Betsy, "Dragon Breath", 5,
                (npc, target) => {
                    Vector2 direction = target.Center - npc.Center;
                    direction = direction.SafeNormalize(Vector2.Zero);
                    for (int i = -2; i <= 2; i++) {
                        Vector2 velocity = direction.RotatedBy(MathHelper.ToRadians(i * 15)) * 14f;
                        int projId = Projectile.NewProjectile(new EntitySource_Parent(npc), npc.Center, velocity, ProjectileID.DD2BetsyFireball, 30, 2f, Main.myPlayer);
                        Main.projectile[projId].hostile = true;
                        Main.projectile[projId].friendly = false;
                        TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", projId);
                    }
                });

            // Flying Dutchman - Cannon Barrage
            AddBossSkill(NPCID.PirateShip, "Cannon Barrage", 4,
                (npc, target) => {
                    for (int i = 0; i < 5; i++) {
                        Vector2 position = npc.Center + new Vector2(Main.rand.Next(-100, 101), 0);
                        Vector2 velocity = new Vector2(0, 10f);
                        int projId = Projectile.NewProjectile(new EntitySource_Parent(npc), position, velocity, ProjectileID.CannonballHostile, 30, 2f, Main.myPlayer);
                        Main.projectile[projId].hostile = true;
                        Main.projectile[projId].friendly = false;
                        TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", projId);
                    }
                });

            // Pumpking - Flaming Scythe
            AddBossSkill(NPCID.Pumpking, "Flaming Scythe", 4,
                (npc, target) => {
                    Vector2 direction = target.Center - npc.Center;
                    direction = direction.SafeNormalize(Vector2.Zero);
                    int projId = Projectile.NewProjectile(new EntitySource_Parent(npc), npc.Center, direction * 16f, ProjectileID.FlamingScythe, 35, 2f, Main.myPlayer);
                    Main.projectile[projId].hostile = true;
                    Main.projectile[projId].friendly = false;
                    TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", projId);
                });

            // Ice Queen - Frost Wave
            AddBossSkill(NPCID.IceQueen, "Frost Wave", 5,
                (npc, target) => {
                    for (int i = -2; i <= 2; i++) {
                        Vector2 velocity = new Vector2(8f * i, -10f);
                        int projId = Projectile.NewProjectile(new EntitySource_Parent(npc), npc.Center, velocity, ProjectileID.IceSpike, 30, 2f, Main.myPlayer);
                        Main.projectile[projId].hostile = true;
                        Main.projectile[projId].friendly = false;
                        TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", projId);
                    }
                });

            // Martian Saucer - Death Ray
            AddBossSkill(NPCID.MartianSaucer, "Death Ray", 5,
                (npc, target) => {
                    Vector2 direction = target.Center - npc.Center;
                    direction = direction.SafeNormalize(Vector2.Zero);
                    int projId = Projectile.NewProjectile(new EntitySource_Parent(npc), npc.Center, direction * 20f, ProjectileID.MartianTurretBolt, 40, 2f, Main.myPlayer);
                    Main.projectile[projId].hostile = true;
                    Main.projectile[projId].friendly = false;
                    TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", projId);
                });

            // Empress of Light - Prismatic Bolts
            AddBossSkill(NPCID.HallowBoss, "Prismatic Bolts", 5,
                (npc, target) => {
                    for (int i = 0; i < 8; i++) {
                        float rotation = MathHelper.TwoPi * i / 8f;
                        Vector2 velocity = new Vector2((float)Math.Cos(rotation), (float)Math.Sin(rotation)) * 16f;
                        int projId = Projectile.NewProjectile(new EntitySource_Parent(npc), npc.Center, velocity, ProjectileID.HallowBossRainbowStreak, 35, 2f, Main.myPlayer);
                        Main.projectile[projId].hostile = true;
                        Main.projectile[projId].friendly = false;
                        TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", projId);
                    }
                });
        }

        private List<int> GetBossList()
        {
            return new List<int>
            {
                // Pre-hardmode
                NPCID.KingSlime,
                NPCID.EyeofCthulhu,
                NPCID.EaterofWorldsBody,
                NPCID.BrainofCthulhu,
                NPCID.QueenBee,
                NPCID.SkeletronHead,
                NPCID.WallofFlesh,
                NPCID.Deerclops,
                NPCID.QueenSlimeBoss,

                // Hardmode
                NPCID.TheDestroyer,
                NPCID.Spazmatism,
                NPCID.Retinazer,
                NPCID.SkeletronPrime,
                NPCID.Plantera,
                NPCID.Golem,
                NPCID.DukeFishron,
                NPCID.HallowBoss,
                NPCID.CultistBoss,
                NPCID.MoonLordHead,

                // Event Bosses
                NPCID.DD2DarkMageT1,
                NPCID.DD2OgreT2,
                NPCID.DD2Betsy,
                NPCID.PirateShip,
                NPCID.MourningWood,
                NPCID.Pumpking,
                NPCID.IceQueen,
                NPCID.SantaNK1,
                NPCID.Everscream,
                NPCID.MartianSaucer
            };
        }

        private bool IsHardmodeBoss(int bossType)
        {
            return bossType == NPCID.TheDestroyer ||
                   bossType == NPCID.Spazmatism ||
                   bossType == NPCID.Retinazer ||
                   bossType == NPCID.SkeletronPrime ||
                   bossType == NPCID.Plantera ||
                   bossType == NPCID.Golem ||
                   bossType == NPCID.DukeFishron ||
                   bossType == NPCID.CultistBoss ||
                   bossType == NPCID.MoonLordHead;
        }

        private bool IsMinion(int npcType)
        {
            return npcType == NPCID.Creeper ||
                   npcType == NPCID.Probe ||
                   npcType == NPCID.Bee ||
                   npcType == NPCID.GolemFistLeft ||
                   npcType == NPCID.GolemFistRight ||
                   npcType == NPCID.Sharkron ||
                   npcType == NPCID.Sharkron2 ||
                   npcType == NPCID.ServantofCthulhu ||
                   npcType == NPCID.EaterofSouls ||
                   npcType == NPCID.VileSpitEaterOfWorlds ||
                   npcType == NPCID.GolemHead ||
                   npcType == NPCID.CultistDragonHead ||
                   npcType == NPCID.BeeSmall ||
                   npcType == NPCID.MoonLordFreeEye ||
                   npcType == NPCID.MoonLordHead ||
                   npcType == NPCID.SkeletronHand ||
                   npcType == NPCID.TheDestroyerBody ||
                   npcType == NPCID.PlanterasTentacle ||
                   npcType == NPCID.QueenSlimeMinionBlue ||
                   npcType == NPCID.QueenSlimeMinionPink ||
                   npcType == NPCID.SkeletronPrime ||
                   npcType == NPCID.PrimeCannon ||
                   npcType == NPCID.PrimeLaser ||
                   npcType == NPCID.PrimeSaw ||
                   npcType == NPCID.PrimeVice;
        }
    }
}
