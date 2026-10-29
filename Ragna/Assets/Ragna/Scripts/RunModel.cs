using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ragna
{
    // IDs 1..6 are retired save IDs. Keep Adventurer=7 to avoid reinterpreting old saves.
    public enum Job { Warrior = 0, Adventurer = 7 }
    public enum RunState { Camp, Fighting, Blessing, Promotion, Dead, Victory, Preparation }
    public enum Blessing { Power, Vitality, Haste, Skill, Leech, Precision, Supplies, Barrier, Multicast }

    public static class JobData
    {
        // Add future jobs here and author their CombatStats/Career assets; retired IDs stay reserved.
        public static readonly Job[] AvailableJobs = { Job.Warrior };
        public static bool IsSpecialized(Job job) => Array.IndexOf(AvailableJobs, job) >= 0;
        public static int PassiveIndex(Job job) => Array.IndexOf(AvailableJobs, job);
        public static readonly string[] Names = { "검사", "", "", "", "", "", "", "모험가" };
        public static readonly string[] Skills = { "월광 참격", "", "", "", "", "", "", "용기의 일격" };
        public static readonly string[] Passives = { "더블 어택" };
        public static readonly string[] PassiveDescriptions = { "단계당 6% 확률로 기본 공격이 두 번 적중" };
        public static readonly string[] Descriptions = { "검으로 싸우는 근접 전투 · 기사와 광전사의 길", "", "", "", "", "", "", "여정의 첫걸음 · 20레벨에 검사로 전직" };
        public static readonly Color[] Colors = { new Color32(109, 213, 255, 255), Color.clear, Color.clear, Color.clear, Color.clear, Color.clear, Color.clear, new Color32(190, 205, 153, 255) };
    }
    [Serializable] public sealed class MetaProgress
    {
        public int rebirths;
        public int bestFloor;
        public int victories;
        public int[] passives = new int[JobData.AvailableJobs.Length];
        public bool sound = true;
        public bool vibration = true;
        public bool tutorialSeen;
        public string[] preferredPlan = Array.Empty<string>();
        public void Sanitize()
        {
            if (passives == null) passives = new int[JobData.AvailableJobs.Length];
            else if (passives.Length != JobData.AvailableJobs.Length) Array.Resize(ref passives, JobData.AvailableJobs.Length);
            if (preferredPlan == null || preferredPlan.Length > 10002) preferredPlan = Array.Empty<string>();
            rebirths = Mathf.Clamp(rebirths, 0, 10000);
            bestFloor = Mathf.Clamp(bestFloor, 0, 30);
            for (int i = 0; i < passives.Length; i++) passives[i] = Mathf.Clamp(passives[i], 0, 5);
        }
    }

    [Serializable] public sealed class EnemyState
    {
        public int id, kind;
        public float hp, maxHp, x, y, attackTimer, flash;
        public bool boss;
    }

    [Serializable] public sealed class RunSnapshot
    {
        public Job job = Job.Adventurer;
        public string careerId = "";
        public RunState state;
        public int level = 1, floor = 1, kills, skillLevel = 1, promotion, potions = 5;
        public int spawned, floorKills, nextId;
        public float hp, xp, potionCd, guardCd, guardTime, attackTimer, skillTimer, spawnTimer, time;
        public float leechPool;
        public int[] upgrades = new int[9];
        public Blessing[] offers = Array.Empty<Blessing>();
        public List<EnemyState> enemies = new List<EnemyState>();
        public uint rng = 12345;
        public string[] attackPlan = Array.Empty<string>();
        public string[] activePlan = Array.Empty<string>();
        // Legacy turn fields retained only to read old saves; auto hunting never uses turn ownership.
        public TurnSide turnSide;
        public int comboIndex, enemyActorId, playerTurns, enemyTurns;
        public float turnTimer = .35f;
        public bool counterBoost;
        public float heroX = RunModel.HeroX, heroY = RunModel.GroundY;
        public bool heroMoving;
        public int facing = 1;
    }

    public readonly struct CombatEvent
    {
        public readonly string type;
        public readonly int enemy;
        public readonly float amount;
        public readonly bool critical;
        public readonly Vector3 position;
        public readonly int visualTier;
        public CombatEvent(string type, int enemy = 0, float amount = 0, bool critical = false, Vector3 position = default, int visualTier = -1)
        { this.type = type; this.enemy = enemy; this.amount = amount; this.critical = critical; this.position = position; this.visualTier = visualTier; }
    }

    // A deterministic simulation. UI, sprites, audio, and persistence observe its events.
    public sealed partial class RunModel
    {
        public RunSnapshot run;
        public MetaProgress meta;
        public readonly List<CombatEvent> events = new List<CombatEvent>(64);
        public bool paused;
        public RunModel(MetaProgress meta) { this.meta = meta; meta.Sanitize(); }
        int U(Blessing b) => run.upgrades[(int)b];
        int P(Job j) => JobData.IsSpecialized(j) ? meta.passives[JobData.PassiveIndex(j)] : 0;
        public CareerDefinition Career => CareerCatalog.Find(run?.careerId);
        public string JobName => Career != null ? Career.displayName : JobData.Names[(int)run.job];
        public Color Accent => Career != null ? Career.tint : JobData.Colors[(int)run.job];
        public float MaxHp => (180 + (run.level - 1) * 9) *
            (1 + .16f * U(Blessing.Vitality) + .02f * meta.rebirths) * (Career != null ? Career.healthMultiplier : 1);
        CombatStats Stats => CombatStats.For(run.job);
        public float Power => ((Stats != null ? Stats.baseDamage : 18) + run.level * (Stats != null ? Stats.damagePerLevel : 3.5f)) * (1 + .16f * U(Blessing.Power) + .2f * run.promotion + .025f * meta.rebirths) * (Career != null ? Career.damageMultiplier : 1);
        public float AttackInterval => Mathf.Max(.22f, (Stats != null ? 1f / Mathf.Max(.1f, Stats.attacksPerSecond) : .95f) /
            ((1 + .12f * U(Blessing.Haste)) * (Career != null ? Career.attackSpeedMultiplier : 1)));
        public float CritChance => Mathf.Min(.8f, .08f + .07f * U(Blessing.Precision));
        public int SkillTier => Mathf.Min(3, run.skillLevel < 4 ? 0 : run.skillLevel < 8 ? 1 : run.skillLevel < 12 ? 2 : 3);
        public float SkillMultiplier => ((Stats != null ? Stats.skillBaseMultiplier : 1.7f) + (Stats != null ? Stats.skillMultiplierPerLevel : .20f) * run.skillLevel);
        public float SkillCooldown => Mathf.Max(2.5f, 6 - .15f * U(Blessing.Skill));
        public float XpRequired => 12 + run.level * 4;
        public int FloorPopulation => run.floor % 5 == 0 ? 1 : 5 + run.floor / 6;
        public int NextPromotion => run.promotion == 0 ? 20 : run.promotion == 1 ? 60 : run.promotion == 2 ? 100 : 0;
        public bool CanRebirth => run != null && JobData.IsSpecialized(run.job) && run.level >= 100 && run.promotion == 3 && meta.rebirths < 10000;
        public bool IsActive => run != null && run.state == RunState.Fighting && !paused;
        public const float HeroX = -4.2f, GroundY = -3f, MeleeX = -2.25f;
        public string Region => BackgroundCatalog.Names[BackgroundCatalog.Index(run.floor)];
        float Random01()
        {
            uint x = run.rng;
            x ^= x << 13; x ^= x >> 17; x ^= x << 5;
            run.rng = x == 0 ? 12345u : x;
            return (run.rng & 0xFFFFFF) / 16777216f;
        }
        public void Start(uint seed = 12345)
        {
            run = new RunSnapshot { job = Job.Adventurer, state = RunState.Preparation, rng = seed == 0 ? 12345u : seed };
            run.hp = MaxHp;
            ResizePlan();
            if (meta.preferredPlan != null) ImportPreferredPlan();
            paused = false;
            events.Clear();
            events.Add(new CombatEvent("start"));
        }
        public void Tick(float delta)
        {
            events.Clear();
            if (!IsActive) return;
            float remaining = Mathf.Clamp(delta, 0, .5f);
            while (remaining > .0001f && IsActive)
            {
                float dt = Mathf.Min(.05f, remaining);
                Step(dt); remaining -= dt;
            }
        }
        void Step(float dt) => HuntingStep(dt);
        public EnemyState Nearest()
        {
            EnemyState result = null;
            foreach (var enemy in run.enemies)
                if (enemy.hp > 0 && (result == null || DistanceSquared(enemy) < DistanceSquared(result))) result = enemy;
            return result;
        }
        void SpawnEnemy()
        {
            bool boss = run.floor % 5 == 0;
            float hp = (38 + run.floor * 18 + run.floor * run.floor * 1.9f) * (boss ? (12 + run.floor * 1.5f) * HuntingMapSettings.Current.bossHealthMultiplier : 1);
            var enemy = new EnemyState {
                id = ++run.nextId, boss = boss, kind = boss ? 3 : (run.floor + run.spawned) % 3,
                y = Mathf.Lerp(HuntingMapSettings.Current.depthBounds.x, HuntingMapSettings.Current.depthBounds.y, Random01()), x = HuntingMapSettings.Current.horizontalBounds.y,
                hp = hp, maxHp = hp, attackTimer = HuntingMapSettings.Current.initialAttackDelay + Random01() * .2f };
            run.enemies.Add(enemy); run.spawned++;
            events.Add(new CombatEvent(boss ? "boss" : "spawn", enemy.id));
        }
        void Hit(EnemyState target, float damage, string type)
        {
            bool crit = Random01() < CritChance;
            if (crit) damage *= 1.8f;
            float actual = Mathf.Min(target.hp, damage);
            target.hp = Mathf.Max(0, target.hp - damage); target.flash = .12f;
            events.Add(new CombatEvent(type, target.id, damage, crit, new Vector3(target.x, target.y + .65f, 0)));
            float leech = .012f * U(Blessing.Leech);
            float recovered = Mathf.Min(actual * leech, run.leechPool);
            run.leechPool -= recovered;
            run.hp = Mathf.Min(MaxHp, run.hp + recovered);
            if (target.hp <= 0)
            {
                run.kills++; run.floorKills++;
                events.Add(new CombatEvent("kill", target.id, position: new Vector3(target.x, target.y, 0)));
                GainXp((24 + run.floor * 18) * (target.boss ? 6 : 1));
            }
        }
        public void GainXp(float amount)
        {
            run.xp += amount;
            while (run.xp >= XpRequired && run.level < 120)
            {
                run.xp -= XpRequired;
                float before = MaxHp;
                run.level++;
                if (run.level % 5 == 0) run.skillLevel++;
                run.hp = Mathf.Min(MaxHp, run.hp + MaxHp - before);
                events.Add(new CombatEvent("level", amount: run.level));
            }
            if (run.level >= 120) run.xp = 0;
        }
        void ClearFloor()
        {
            meta.bestFloor = Mathf.Max(meta.bestFloor, run.floor);
            if (run.floor % 5 == 0) run.potions = Mathf.Min(9, run.potions + 3);
            if (run.floor >= 30)
            { run.state = RunState.Victory; meta.victories++; events.Add(new CombatEvent("victory")); return; }
            run.offers = RollOffers(); run.state = RunState.Blessing;
            events.Add(new CombatEvent("blessingReady"));
        }
        Blessing[] RollOffers()
        {
            var pool = new List<Blessing>();
            for (int i = 0; i < 9; i++) pool.Add((Blessing)i);
            var result = new Blessing[3];
            for (int i = 0; i < 3; i++)
            { int index = Mathf.Min(pool.Count - 1, (int)(Random01() * pool.Count)); result[i] = pool[index]; pool.RemoveAt(index); }
            return result;
        }
        public bool ChooseBlessing(int index)
        {
            if (run.state != RunState.Blessing || index < 0 || index >= run.offers.Length) return false;
            float before = MaxHp;
            var blessing = run.offers[index]; run.upgrades[(int)blessing]++;
            if (blessing == Blessing.Vitality) run.hp += MaxHp - before;
            if (blessing == Blessing.Skill) run.skillLevel++;
            if (blessing == Blessing.Supplies) { run.potions = Mathf.Min(9, run.potions + 2); run.hp = Mathf.Min(MaxHp, run.hp + MaxHp * .2f); }
            run.floor++; run.floorKills = run.spawned = 0; run.spawnTimer = .6f;
            run.offers = Array.Empty<Blessing>(); run.state = RunState.Fighting;
            return true;
        }
        public bool Promote(string careerId = null)
        {
            if (run.state != RunState.Promotion || NextPromotion == 0 || run.level < NextPromotion) return false;
            if (run.job == Job.Adventurer)
            {
                foreach (var job in JobData.AvailableJobs) if (careerId == "job:" + job) return ChooseJob(job);
                return false;
            }
            var choices = CareerCatalog.Choices(run.job, run.level, run.promotion + 1, run.careerId);
            if (choices.Length > 0)
            {
                var choice = CareerCatalog.Find(careerId);
                if (choice == null || Array.IndexOf(choices, choice) < 0) return false;
                run.careerId = choice.id;
            }
            run.promotion++; run.skillLevel += 2;
            run.hp = MaxHp; run.potions = Mathf.Min(9, run.potions + 2);
            run.state = RunState.Preparation;
            ResizePlan();
            events.Add(new CombatEvent("promoted"));
            return true;
        }
        public bool ChooseJob(Job job)
        {
            if (run == null || run.state != RunState.Promotion || run.job != Job.Adventurer || run.promotion != 0 || run.level < 20 || !JobData.IsSpecialized(job)) return false;
            run.job = job; run.careerId = ""; run.promotion = 1; run.skillLevel += 2;
            run.hp = MaxHp; run.potions = Mathf.Min(9, run.potions + 2); run.skillTimer = 0;
            run.state = RunState.Preparation; ResizePlan(); events.Add(new CombatEvent("promoted")); return true;
        }
        public bool DrinkPotion()
        {
            if (!IsActive || run.potions <= 0 || run.potionCd > 0 || run.hp >= MaxHp) return false;
            run.potions--; run.potionCd = 5;
            float amount = MaxHp * .42f;
            run.hp = Mathf.Min(MaxHp, run.hp + amount);
            events.Add(new CombatEvent("potion", amount: amount)); return true;
        }
        public bool Guard()
        {
            if (!IsActive || run.guardCd > 0) return false;
            run.guardCd = 8; run.guardTime = 1.6f + (Career != null ? Career.guardDurationBonus : 0);
            events.Add(new CombatEvent("guard")); return true;
        }
        public bool Rebirth()
        {
            if (!CanRebirth) return false;
            int passiveIndex = JobData.PassiveIndex(run.job);
            meta.passives[passiveIndex] = Mathf.Min(5, meta.passives[passiveIndex] + 1); meta.rebirths++;
            meta.bestFloor = Mathf.Max(meta.bestFloor, run.floor - (run.state == RunState.Victory ? 0 : 1));
            run = null; paused = false; events.Clear(); return true;
        }
        public bool Restore(RunSnapshot saved)
        {
            if (saved == null || saved.state == RunState.Camp || !Enum.IsDefined(typeof(Job), saved.job) || !Enum.IsDefined(typeof(RunState), saved.state) ||
                saved.level < 1 || saved.level > 120 || saved.floor < 1 || saved.floor > 30 ||
                saved.promotion < 0 || saved.promotion > 3 || saved.skillLevel < 1 || saved.skillLevel > 100 ||
                saved.upgrades == null || saved.upgrades.Length != 9 || saved.enemies == null || saved.enemies.Count > 20 ||
                float.IsNaN(saved.hp) || float.IsInfinity(saved.hp) || saved.hp < 0) return false;
            if (saved.job == Job.Adventurer && (saved.promotion != 0 || !string.IsNullOrEmpty(saved.careerId))) return false;
            if (JobData.IsSpecialized(saved.job) && (saved.promotion == 0 || saved.level < 20)) return false;
            if (saved.promotion >= 2 && saved.level < 60 || saved.promotion == 3 && saved.level < 100) return false;
            foreach (int u in saved.upgrades) if (u < 0 || u > 100) return false;
            if (saved.state == RunState.Blessing && (saved.offers == null || saved.offers.Length != 3)) return false;
            if (saved.offers != null) foreach (var b in saved.offers) if (!Enum.IsDefined(typeof(Blessing), b)) return false;
            if (saved.spawned < 0 || saved.floorKills < 0 || saved.nextId < 0 || !Finite(saved.xp) || !Finite(saved.time) ||
                !Finite(saved.attackTimer) || !Finite(saved.skillTimer) || !Finite(saved.potionCd) || !Finite(saved.guardCd) ||
                !Finite(saved.guardTime) || !Finite(saved.spawnTimer) || !Finite(saved.leechPool) || saved.leechPool < 0) return false;
            foreach (var e in saved.enemies)
                if (e == null || e.id < 1 || e.kind < 0 || e.kind > 3 || !Finite(e.hp) || !Finite(e.maxHp) || e.maxHp <= 0 ||
                    e.hp < 0 || e.hp > e.maxHp || !Finite(e.x) || !Finite(e.y) || !Finite(e.attackTimer) || !Finite(e.flash)) return false;
            run = saved; run.hp = Mathf.Min(run.hp, MaxHp); run.potions = Mathf.Clamp(run.potions, 0, 9);
            run.careerId ??= "";
            if (!ValidateHuntingState()) { run = null; return false; }
            paused = true; return true;
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
