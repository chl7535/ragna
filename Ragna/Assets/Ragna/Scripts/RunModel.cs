using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ragna
{
    // Keep the seven existing IDs stable for saves and inherited passive ranks.
    public enum Job { Warrior, Archer, Mage, Gunner, Fighter, Cleric, Rogue, Adventurer }
    public enum RunState { Camp, Fighting, Blessing, Promotion, Dead, Victory }
    public enum Blessing { Power, Vitality, Haste, Skill, Leech, Precision, Supplies, Barrier, Multicast }

    public static class JobData
    {
        public static bool IsSpecialized(Job job) => (int)job >= 0 && (int)job < 7;
        public static readonly string[] Names = { "검사", "궁수", "법사", "거너", "격투가", "성직자", "도적", "모험가" };
        public static readonly string[] Skills = { "월광 참격", "폭풍 화살", "유성 낙하", "탄막 폭격", "파쇄 연타", "심판의 빛", "그림자 비수", "용기의 일격" };
        public static readonly string[] Passives = { "더블 어택", "매의 눈", "비전 증폭", "속사", "강인한 육체", "생명의 축복", "흡혈" };
        public static readonly string[] PassiveDescriptions = {
            "단계당 6% 확률로 기본 공격이 두 번 적중", "단계당 치명타 확률 +4%", "단계당 스킬 피해 +8%",
            "단계당 공격 속도 +4%", "단계당 최대 HP +6%", "단계당 물약 회복 +5%, 층마다 HP 1% 회복", "단계당 가한 피해의 1% 회복" };
        public static readonly string[] Descriptions = {
            "균형 잡힌 근접 전투 · 넓은 참격", "높은 치명타 · 여러 발의 화살", "강력한 범위 스킬 · 유성 폭발",
            "빠른 연사 · 전방 탄막", "높은 체력 · 근거리 연타", "회복 특화 · 빛의 광역 심판", "빠른 공격 · 흡혈과 비수", "여정의 첫걸음 · 20레벨에 일곱 직업 중 선택" };
        public static readonly Color[] Colors = {
            new Color32(109, 213, 255, 255), new Color32(157, 235, 151, 255), new Color32(189, 152, 255, 255),
            new Color32(255, 192, 110, 255), new Color32(255, 131, 132, 255), new Color32(255, 228, 159, 255), new Color32(183, 162, 234, 255), new Color32(190, 205, 153, 255) };
    }

    [Serializable] public sealed class MetaProgress
    {
        public int rebirths;
        public int bestFloor;
        public int victories;
        public int[] passives = new int[7];
        public bool sound = true;
        public bool vibration = true;
        public bool tutorialSeen;
        public void Sanitize()
        {
            if (passives == null || passives.Length != 7) passives = new int[7];
            rebirths = Mathf.Clamp(rebirths, 0, 10000);
            bestFloor = Mathf.Clamp(bestFloor, 0, 30);
            for (int i = 0; i < 7; i++) passives[i] = Mathf.Clamp(passives[i], 0, 5);
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
    }

    public readonly struct CombatEvent
    {
        public readonly string type;
        public readonly int enemy;
        public readonly float amount;
        public readonly bool critical;
        public readonly Vector3 position;
        public CombatEvent(string type, int enemy = 0, float amount = 0, bool critical = false, Vector3 position = default)
        { this.type = type; this.enemy = enemy; this.amount = amount; this.critical = critical; this.position = position; }
    }

    // A deterministic simulation. UI, sprites, audio, and persistence observe its events.
    public sealed class RunModel
    {
        public RunSnapshot run;
        public MetaProgress meta;
        public readonly List<CombatEvent> events = new List<CombatEvent>(64);
        public bool paused;
        public RunModel(MetaProgress meta) { this.meta = meta; meta.Sanitize(); }
        int U(Blessing b) => run.upgrades[(int)b];
        int P(Job j) => JobData.IsSpecialized(j) ? meta.passives[(int)j] : 0;
        public CareerDefinition Career => CareerCatalog.Find(run?.careerId);
        public string JobName => Career != null ? Career.displayName : JobData.Names[(int)run.job];
        public Color Accent => Career != null ? Career.tint : JobData.Colors[(int)run.job];
        public float MaxHp => (180 + (run.level - 1) * 9) * (run.job == Job.Fighter ? 1.25f : 1) *
            (1 + .16f * U(Blessing.Vitality) + .06f * P(Job.Fighter) + .02f * meta.rebirths) * (Career != null ? Career.healthMultiplier : 1);
        public float Power => (18 + run.level * 3.5f) * (1 + .16f * U(Blessing.Power) + .2f * run.promotion + .025f * meta.rebirths) * (Career != null ? Career.damageMultiplier : 1);
        public float AttackInterval => Mathf.Max(.22f, (run.job == Job.Gunner ? .65f : run.job == Job.Rogue ? .72f : .95f) /
            ((1 + .12f * U(Blessing.Haste) + .04f * P(Job.Gunner)) * (Career != null ? Career.attackSpeedMultiplier : 1)));
        public float CritChance => Mathf.Min(.8f, .08f + .04f * P(Job.Archer) + .07f * U(Blessing.Precision) + (run.job == Job.Archer ? .12f : 0));
        public int SkillTier => Mathf.Min(3, run.skillLevel < 4 ? 0 : run.skillLevel < 8 ? 1 : run.skillLevel < 12 ? 2 : 3);
        public float SkillMultiplier => (1.7f + .20f * run.skillLevel) * (1 + .08f * P(Job.Mage)) * (run.job == Job.Mage ? 1.3f : 1);
        public float SkillCooldown => Mathf.Max(2.5f, 6 - .15f * U(Blessing.Skill));
        public float XpRequired => 12 + run.level * 4;
        public int FloorPopulation => run.floor % 5 == 0 ? 1 : 5 + run.floor / 6;
        public int NextPromotion => run.promotion == 0 ? 20 : run.promotion == 1 ? 60 : run.promotion == 2 ? 100 : 0;
        public bool CanRebirth => run != null && JobData.IsSpecialized(run.job) && run.level >= 100 && run.promotion == 3 && P(run.job) < 5;
        public bool IsActive => run != null && run.state == RunState.Fighting && !paused;
        public const float HeroX = -4.2f, GroundY = -3f, MeleeX = -2.25f;
        public static bool CanEnemyAttack(EnemyState enemy) => enemy.boss || enemy.kind == 1 || enemy.kind == 2 || enemy.x <= MeleeX;
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
            run = new RunSnapshot { job = Job.Adventurer, state = RunState.Fighting, rng = seed == 0 ? 12345u : seed };
            run.hp = MaxHp;
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
        void Step(float dt)
        {
            run.time += dt;
            run.leechPool = Mathf.Min(MaxHp * .03f, run.leechPool + MaxHp * .02f * dt);
            run.potionCd = Mathf.Max(0, run.potionCd - dt);
            run.guardCd = Mathf.Max(0, run.guardCd - dt);
            run.guardTime = Mathf.Max(0, run.guardTime - dt);
            run.attackTimer -= dt; run.skillTimer -= dt; run.spawnTimer -= dt;
            if (run.spawned < FloorPopulation && run.spawnTimer <= 0)
            { SpawnEnemy(); run.spawnTimer = run.floor % 5 == 0 ? 4 : 1.7f; }
            foreach (var enemy in run.enemies)
            {
                if (enemy.hp <= 0) continue;
                enemy.flash = Mathf.Max(0, enemy.flash - dt);
                float target = enemy.boss ? -1.35f : MeleeX;
                if (enemy.x > target) enemy.x = Mathf.Max(target, enemy.x - dt * (enemy.boss ? .4f : .6f + .015f * run.floor));
                if (CanEnemyAttack(enemy))
                {
                    enemy.attackTimer -= dt;
                    if (enemy.attackTimer <= 0)
                    {
                        float amount = (9 + run.floor * 3.5f + run.floor * run.floor * .055f) * (enemy.boss ? 2 : 1);
                        if (enemy.boss) amount += MaxHp * .10f;
                        if (enemy.boss && run.floor == 30) amount *= 1.65f;
                        if (run.guardTime > 0)
                        {
                            amount *= .15f;
                            run.skillTimer = 0; // A timed block earns an immediate counter-skill.
                            events.Add(new CombatEvent("blocked", enemy.id));
                        }
                        amount *= Mathf.Max(.45f, 1 - .06f * U(Blessing.Barrier));
                        amount *= Career != null ? Career.damageTakenMultiplier : 1;
                        run.hp = Mathf.Max(0, run.hp - amount);
                        events.Add(new CombatEvent("hurt", enemy.id, amount));
                        enemy.attackTimer = enemy.boss ? 2.8f : 2.6f + Random01() * 1.1f;
                        if (run.hp <= 0)
                        { run.state = RunState.Dead; meta.bestFloor = Mathf.Max(meta.bestFloor, run.floor - 1); events.Add(new CombatEvent("death")); return; }
                    }
                }
            }
            if (run.attackTimer <= 0)
            {
                var target = Nearest();
                if (target != null)
                {
                    Hit(target, Power, "attack");
                    if (target.hp > 0 && Random01() < .06f * P(Job.Warrior)) Hit(target, Power, "double");
                    run.attackTimer = AttackInterval;
                }
            }
            if (run.skillTimer <= 0 && Nearest() != null)
            {
                int baseHits = run.job == Job.Fighter ? 4 : run.job == Job.Gunner ? 3 : run.job == Job.Archer || run.job == Job.Rogue ? 2 : 1;
                int hits = baseHits + SkillTier + U(Blessing.Multicast);
                events.Add(new CombatEvent("skill"));
                float damage = Power * SkillMultiplier / (1 + SkillTier * .45f) / Mathf.Sqrt(baseHits);
                if (run.job == Job.Mage || run.job == Job.Cleric || run.job == Job.Warrior)
                {
                    // Area skills retain a modest boss hit count but hit every enemy on screen.
                    foreach (var target in run.enemies)
                        if (target.hp > 0) Hit(target, damage * (run.job == Job.Cleric ? .8f : 1), "skillhit");
                    hits -= 1;
                }
                for (int i = 0; i < hits; i++)
                {
                    var target = Nearest();
                    if (target == null) break;
                    Hit(target, damage, "skillhit");
                }
                if (run.job == Job.Cleric) run.hp = Mathf.Min(MaxHp, run.hp + MaxHp * .035f);
                run.skillTimer = SkillCooldown;
            }
            run.enemies.RemoveAll(e => e.hp <= 0);
            if (NextPromotion > 0 && run.level >= NextPromotion)
            { run.state = RunState.Promotion; events.Add(new CombatEvent("promotionReady")); return; }
            if (run.floorKills >= FloorPopulation) ClearFloor();
        }
        public EnemyState Nearest()
        {
            EnemyState result = null;
            foreach (var enemy in run.enemies)
                if (enemy.hp > 0 && (result == null || enemy.x < result.x)) result = enemy;
            return result;
        }
        void SpawnEnemy()
        {
            bool boss = run.floor % 5 == 0;
            float hp = (38 + run.floor * 18 + run.floor * run.floor * 1.9f) * (boss ? 12 + run.floor * 1.5f : 1);
            var enemy = new EnemyState {
                id = ++run.nextId, boss = boss, kind = boss ? 3 : (run.floor + run.spawned) % 3,
                y = boss ? -1.5f : -2f + (Random01() - .5f) * .2f, x = boss ? 4.55f : 5.15f,
                hp = hp, maxHp = hp, attackTimer = boss ? 2.8f : 2.5f + Random01() };
            run.enemies.Add(enemy); run.spawned++;
            events.Add(new CombatEvent(boss ? "boss" : "spawn", enemy.id));
        }
        void Hit(EnemyState target, float damage, string type)
        {
            bool crit = Random01() < CritChance;
            if (crit) damage *= 1.8f;
            float actual = Mathf.Min(target.hp, damage);
            target.hp = Mathf.Max(0, target.hp - damage); target.flash = .12f;
            events.Add(new CombatEvent(type, target.id, damage, crit, new Vector3(target.x, target.y, 0)));
            float leech = .01f * P(Job.Rogue) + .012f * U(Blessing.Leech) + (run.job == Job.Rogue ? .015f : 0);
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
            run.hp = Mathf.Min(MaxHp, run.hp + MaxHp * .01f * P(Job.Cleric));
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
                for (int i = 0; i < 7; i++) if (careerId == "job:" + (Job)i) return ChooseJob((Job)i);
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
            run.state = RunState.Fighting;
            events.Add(new CombatEvent("promoted"));
            return true;
        }
        public bool ChooseJob(Job job)
        {
            if (run == null || run.state != RunState.Promotion || run.job != Job.Adventurer || run.promotion != 0 || run.level < 20 || !JobData.IsSpecialized(job)) return false;
            run.job = job; run.careerId = ""; run.promotion = 1; run.skillLevel += 2;
            run.hp = MaxHp; run.potions = Mathf.Min(9, run.potions + 2); run.skillTimer = 0;
            run.state = RunState.Fighting; events.Add(new CombatEvent("promoted")); return true;
        }
        public bool DrinkPotion()
        {
            if (!IsActive || run.potions <= 0 || run.potionCd > 0 || run.hp >= MaxHp) return false;
            run.potions--; run.potionCd = 5;
            float amount = MaxHp * Mathf.Min(.85f, .42f + .05f * P(Job.Cleric) + (run.job == Job.Cleric ? .10f : 0));
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
            meta.passives[(int)run.job]++; meta.rebirths++;
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
            paused = true; return true;
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
