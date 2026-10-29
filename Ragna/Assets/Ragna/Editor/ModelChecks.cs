using System;
using System.IO;
using System.Text;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

namespace Ragna.Editor
{
    public static class ModelChecks
    {
        static int count;
        static void Check(bool condition, string name)
        { if (!condition) throw new Exception("FAIL: " + name); count++; }
        static RunModel Fresh(uint seed = 731)
        { var model = new RunModel(new MetaProgress()); model.Start(seed); model.BeginCombat(); return model; }
        [MenuItem("Ragna/Run model checks")]
        public static void Menu() => Debug.Log(Run());
        [CliCommand("ragna_checks", "Run deterministic combat, progression, save and rebirth checks", MainThreadRequired = true)]
        public static string Run()
        {
            count = 0;
            CareerCatalog.Reload();
            Check(CareerCatalog.Find("knight") != null && CareerCatalog.Find("berserker") != null, "starter career assets exist");
            Check(Fresh().run.job == Job.Adventurer && Fresh().run.promotion == 0, "every new run starts as adventurer");
            var gated = Fresh();
            Check(!gated.ChooseJob(Job.Warrior), "cannot choose class before level 20");
            var invalidJobSave = Fresh(); invalidJobSave.run.job = Job.Warrior;
            Check(!Fresh().Restore(invalidJobSave.run), "unpromoted specialized class save is rejected");
            gated.run.level = 20; gated.run.state = RunState.Promotion;
            Check(!gated.Promote() && !gated.Promote("knight") && !gated.ChooseJob(Job.Adventurer) && !gated.ChooseJob((Job)99), "first promotion requires a valid specialized class");
            for (int i = 0; i < JobData.AvailableJobs.Length; i++)
            {
                var first = Fresh(); first.run.level = 20; first.run.state = RunState.Promotion;
                int oldSkill = first.run.skillLevel;
                Check(first.ChooseJob((Job)i) && first.run.job == (Job)i && first.run.promotion == 1 && first.run.skillLevel == oldSkill + 2 && first.run.hp == first.MaxHp, "first class choice " + i);
                Check(!first.ChooseJob((Job)((i + 1) % 7)), "class cannot be changed after choosing " + i);
            }
            var a = Fresh(); var b = Fresh();
            for (int i = 0; i < 300; i++) { a.Tick(.05f); b.Tick(.05f); }
            Check(JsonUtility.ToJson(a.run) == JsonUtility.ToJson(b.run), "seed determinism");
            var m = Fresh(); m.paused = true; string before = JsonUtility.ToJson(m.run); m.Tick(10);
            Check(before == JsonUtility.ToJson(m.run), "pause freezes combat and cooldowns");
            m.paused = false; Check(!m.DrinkPotion(), "no potion waste at full health");
            m.run.hp = m.MaxHp * .2f; Check(m.DrinkPotion(), "potion heals");
            float hp = m.run.hp; Check(hp > m.MaxHp * .2f && hp <= m.MaxHp, "healing bounded");
            Check(!m.DrinkPotion() && m.run.potions == 4, "potion cooldown prevents repeats");
            Check(m.Guard() && !m.Guard(), "guard cooldown prevents repeats");
            m.run.enemies.Clear(); m.run.spawned = m.FloorPopulation;
            m.run.enemies.Add(new EnemyState { id = 99, hp = 99999, maxHp = 99999, x = m.run.heroX + 1, y = m.run.heroY, attackTimer = .01f });
            m.run.turnSide = TurnSide.Enemy; m.run.enemyActorId = 99; m.run.playerTurns = 1; m.run.turnTimer = .01f;
            m.run.attackTimer = m.run.skillTimer = 100; hp = m.run.hp; m.Tick(.05f);
            Check(hp - m.run.hp > 0 && hp - m.run.hp < 5, "guard reduces incoming damage");
            m = Fresh();
            foreach (int target in new[] { 20, 60, 100 })
            {
                m.run.level = target; m.Tick(.05f);
                Check(m.run.state == RunState.Promotion, "promotion gate " + target);
                Check(target == 20 ? m.ChooseJob(Job.Warrior) : m.Promote(target == 60 ? "knight" : null), "promotion applies " + target);
                Check(m.run.state == RunState.Preparation && m.BeginCombat(), "promotion opens preparation " + target);
            }
            Check(m.run.promotion == 3 && m.CanRebirth, "third promotion unlocks rebirth");
            var meta = m.meta; Check(m.Rebirth(), "rebirth accepted");
            Check(meta.passives[0] == 1 && meta.rebirths == 1 && m.run == null, "passive retained and run cleared");
            m.Start(); Check(m.run.level == 1 && m.run.job == Job.Adventurer && meta.passives[0] == 1, "rebirth restarts as adventurer with inherited passive");
            m.run.job = Job.Warrior; m.run.level = 100; m.run.promotion = 3; meta.passives[(int)Job.Warrior] = 5;
            Check(m.CanRebirth && m.Rebirth() && meta.passives[(int)Job.Warrior] == 5 && meta.rebirths == 2, "capped passive still allows permanent slot inheritance");
            var knight = Fresh(); knight.run.level = 20; knight.run.state = RunState.Promotion; knight.ChooseJob(Job.Warrior);
            knight.run.level = 60; knight.run.state = RunState.Promotion;
            Check(!knight.Promote() && !knight.Promote("invalid"), "career choice is required and validated");
            Check(knight.Promote("knight") && knight.JobName == "기사", "knight branch");
            var berserker = Fresh(); berserker.run.level = 20; berserker.run.state = RunState.Promotion; berserker.ChooseJob(Job.Warrior);
            berserker.run.level = 60; berserker.run.state = RunState.Promotion;
            Check(berserker.Promote("berserker") && berserker.JobName == "광전사", "berserker branch");
            Check(knight.MaxHp > berserker.MaxHp && berserker.Power > knight.Power && berserker.AttackInterval < knight.AttackInterval, "career roles differ");
            var careerRestore = new RunModel(knight.meta);
            Check(careerRestore.Restore(JsonUtility.FromJson<RunSnapshot>(JsonUtility.ToJson(knight.run))) && careerRestore.run.careerId == "knight", "career ID survives save");
            m = Fresh(); m.run.skillLevel = 1; Check(m.SkillTier == 0, "skill tier 1");
            m.run.skillLevel = 4; Check(m.SkillTier == 1, "skill tier 2");
            m.run.skillLevel = 8; Check(m.SkillTier == 2, "skill tier 3");
            m.run.skillLevel = 12; Check(m.SkillTier == 3, "skill tier 4");
            for (int i = 0; i < 200; i++) m.Tick(.05f);
            var restored = new RunModel(m.meta);
            Check(restored.Restore(JsonUtility.FromJson<RunSnapshot>(JsonUtility.ToJson(m.run))), "save roundtrip valid");
            Check(restored.paused, "restore is paused"); restored.paused = false;
            for (int i = 0; i < 100; i++) { m.Tick(.05f); restored.Tick(.05f); }
            Check(JsonUtility.ToJson(m.run) == JsonUtility.ToJson(restored.run), "restore preserves RNG and enemy simulation");
            Check(!restored.Restore(new RunSnapshot { hp = float.NaN }), "corrupt HP rejected");
            var emptySave = SaveStore.Parse(JsonUtility.ToJson(new SaveEnvelope { meta = new MetaProgress(), hasRun = false }));
            Check(emptySave.run == null, "empty save cannot materialize a ghost run");
            var activeSave = SaveStore.Parse(JsonUtility.ToJson(new SaveEnvelope { meta = m.meta, run = m.run, hasRun = true }));
            Check(activeSave.run != null && activeSave.run.level == m.run.level, "active save discriminator roundtrip");
            var side = Fresh(); side.run.attackTimer = side.run.skillTimer = 100;
            side.Tick(.05f); var approaching = side.run.enemies[0];
            float startX = approaching.x, startY = approaching.y; side.Tick(.5f);
            Check(approaching.x < startX && approaching.y != startY, "enemies approach in horizontal and ground depth axes");
            side.run.enemies.Clear();
            side.run.enemies.Add(new EnemyState { id = 1, hp = 10, maxHp = 10, x = 3, y = -3 });
            side.run.enemies.Add(new EnemyState { id = 2, hp = 10, maxHp = 10, x = 0, y = -1 });
            Check(side.Nearest().id == 2, "target selection follows horizontal distance");
            Check(!side.InHuntingRange(new EnemyState { hp = 10, x = side.run.heroX + 1, y = side.run.heroY + 1 }), "melee requires depth alignment");
            var old = Fresh(); old.run.enemies.Add(new EnemyState { id = 8, hp = 10, maxHp = 10, x = 1.5f, y = 4 });
            string legacy = JsonUtility.ToJson(new SaveEnvelope { version = 2, meta = old.meta, run = old.run, hasRun = true });
            var migrated = SaveStore.Parse(legacy);
            Check(migrated.version == 7 && Mathf.Abs(migrated.run.enemies[0].x - 3.35f) < .001f &&
                Mathf.Abs(migrated.run.enemies[0].y + 2.60f) < .001f, "vertical save migrates to horizontal coordinates");
            var migratedAgain = SaveStore.Parse(JsonUtility.ToJson(migrated));
            Check(migratedAgain.run.enemies[0].x == migrated.run.enemies[0].x, "horizontal saves are not migrated twice");
            var legacyClass = Fresh(); legacyClass.run.job = Job.Warrior; legacyClass.run.level = 10; legacyClass.meta.passives[0] = 2;
            var converted = SaveStore.Parse(JsonUtility.ToJson(new SaveEnvelope { version = 3, meta = legacyClass.meta, run = legacyClass.run, hasRun = true }));
            Check(converted.run.job == Job.Adventurer && converted.run.level == 10 && converted.meta.passives[0] == 2, "legacy early class becomes adventurer without losing progress");
            legacyClass.run.level = 30;
            converted = SaveStore.Parse(JsonUtility.ToJson(new SaveEnvelope { version = 3, meta = legacyClass.meta, run = legacyClass.run, hasRun = true }));
            Check(converted.run.job == Job.Warrior && converted.run.promotion == 1, "legacy level 20+ chosen class is preserved");
            legacyClass.run.level = 20; legacyClass.run.state = RunState.Promotion;
            converted = SaveStore.Parse(JsonUtility.ToJson(new SaveEnvelope { version = 3, meta = legacyClass.meta, run = legacyClass.run, hasRun = true }));
            Check(converted.run.state == RunState.Preparation && converted.run.promotion == 1, "legacy level 20 promotion returns to valid preparation");
            var inherited = Fresh(); inherited.meta.passives[0] = 2; inherited.meta.Sanitize();
            Check(inherited.meta.passives.Length == 1 && inherited.meta.passives[0] == 2, "only warrior passive is retained");
            m = Fresh(); m.run.floor = 2; m.run.floorKills = m.FloorPopulation; m.run.spawned = m.FloorPopulation; m.Tick(.05f);
            Check(m.run.state == RunState.Blessing && m.run.offers.Length == 3, "floor offers three blessings");
            Check(m.run.offers[0] != m.run.offers[1] && m.run.offers[0] != m.run.offers[2] && m.run.offers[1] != m.run.offers[2], "offers unique");
            Check(!m.ChooseBlessing(3) && !m.ChooseBlessing(-1), "invalid choices rejected");
            Check(m.ChooseBlessing(0) && m.run.floor == 3, "choice resumes next floor");
            var report = new StringBuilder();
            bool unattendedLethal = true;
            bool activeViable = true;
            foreach (Job job in Enum.GetValues(typeof(Job)))
            {
                if (!JobData.IsSpecialized(job)) continue;
                var active = Simulate(job, true); var idle = Simulate(job, false);
                unattendedLethal &= idle.run.state == RunState.Dead;
                activeViable &= active.run.state == RunState.Victory;
                report.AppendLine($"{job}: active={active.run.state} floor={active.run.floor} level={active.run.level} skill={active.run.skillLevel} time={active.run.time:0}s; idle={idle.run.state} floor={idle.run.floor}");
            }
            var berserkerRun = Simulate(Job.Warrior, true, "berserker");
            activeViable &= berserkerRun.run.state == RunState.Victory;
            report.AppendLine($"Berserker: active={berserkerRun.run.state} floor={berserkerRun.run.floor} level={berserkerRun.run.level} time={berserkerRun.run.time:0}s");
            string result = (unattendedLethal && activeViable ? "PASS: " : "BALANCE FAILURE: ") + (count + 2) + " checks\n" + report;
            Directory.CreateDirectory("../Docs"); File.WriteAllText("../Docs/MODEL_CHECKS.txt", result);
            Check(unattendedLethal, "unattended play must be lethal\n" + report);
            Check(activeViable, "each job must be viable with intervention\n" + report);
            return result;
        }
        static RunModel Simulate(Job job, bool intervention, string firstCareer = "knight")
        {
            var m = Fresh();
            m.PrepareCombat();
            for (int i = 0; i < 100000; i++)
            {
                if (m.run.state == RunState.Dead || m.run.state == RunState.Victory) break;
                if (m.run.state == RunState.Preparation)
                {
                    string action = m.SlotCapacity >= 2 && m.run.skillLevel >= 4 ? "skill2" : "skill1";
                    for (int slot = 0; slot < m.SlotCapacity; slot++)
                    {
                        if (action == "skill2" && slot + 2 > m.SlotCapacity) action = "skill1";
                        if (m.SetPlanAction(slot, action)) slot += CombatActionCatalog.Find(action).slotCost - 1;
                    }
                    m.BeginCombat();
                }
                if (m.run.state == RunState.Promotion)
                {
                    if (m.run.job == Job.Adventurer) m.ChooseJob(job);
                    else
                    {
                        var options = CareerCatalog.Choices(job, m.run.level, m.run.promotion + 1, m.run.careerId);
                        m.Promote(options.Length > 0 ? firstCareer : null);
                    }
                }
                if (m.run.state == RunState.Blessing)
                {
                    int best = 0; float bestScore = -999;
                    for (int k = 0; k < 3; k++)
                    {
                        var offer = m.run.offers[k];
                        float score = offer == Blessing.Leech ? 7 - m.run.upgrades[(int)offer] * 2 :
                            offer == Blessing.Supplies ? (m.run.potions < 3 ? 10 : 0) :
                            offer == Blessing.Skill ? 6 - m.run.upgrades[(int)offer] * .5f :
                            offer == Blessing.Power ? 5 : offer == Blessing.Multicast ? 4 : 2;
                        if (score > bestScore) { best = k; bestScore = score; }
                    }
                    m.ChooseBlessing(best);
                }
                if (intervention)
                {
                    if (m.run.hp < m.MaxHp * .55f) m.DrinkPotion();
                    if (m.run.enemies.Exists(e => e.attackTimer < .45f && m.InHuntingRange(e))) m.Guard();
                }
                m.Tick(.1f);
            }
            return m;
        }
    }
}
