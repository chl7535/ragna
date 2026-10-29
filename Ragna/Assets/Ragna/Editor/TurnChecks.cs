using System;
using System.IO;
using Unity.Pipeline.Commands;
using UnityEngine;

namespace Ragna.Editor
{
    public static class TurnChecks
    {
        static int checks;
        static void Check(bool condition, string label) { if (!condition) throw new Exception("Hunting check failed: " + label); checks++; }
        static RunModel Ready()
        {
            var model = new RunModel(new MetaProgress { rebirths = 1 }); model.Start(731);
            model.run.level = 20; model.run.job = Job.Warrior; model.run.promotion = 1;
            model.run.skillLevel = 12; model.ResizePlan(); model.run.hp = model.MaxHp;
            model.run.spawned = model.FloorPopulation;
            return model;
        }
        static EnemyState Enemy(int id, float x, float y, float hp = 100000) =>
            new EnemyState { id = id, x = x, y = y, hp = hp, maxHp = hp, attackTimer = 100 };
        [CliCommand("ragna_hunting_checks", "Verify grounded depth movement, independent auto combat, targeting and saves", MainThreadRequired = true)]
        public static string Run()
        {
            checks = 0;
            var map = HuntingMapSettings.Current;
            var m = Ready(); var enemy = Enemy(99, 4, map.depthBounds.y); m.run.enemies.Add(enemy);
            string before = JsonUtility.ToJson(m.run); m.Tick(.5f);
            Check(before == JsonUtility.ToJson(m.run), "preparation freezes positions");
            m.BeginCombat(); float oldX = m.run.heroX, oldY = m.run.heroY; m.Tick(.5f);
            Check(m.run.heroX > oldX && m.run.heroY > oldY && m.run.heroMoving, "hero automatically approaches in X and depth");
            Check(enemy.hp == enemy.maxHp && !m.events.Exists(e => e.type == "attack"), "no sword attack outside reach");
            for (int i = 0; i < 120 && enemy.hp == enemy.maxHp; i++) m.Tick(.05f);
            Check(enemy.hp < enemy.maxHp && m.InHuntingRange(enemy), "attack begins only within horizontal and depth reach");
            enemy.y = m.run.heroY + map.depthTolerance + .1f;
            Check(!m.InHuntingRange(enemy), "wrong depth lane rejects attack");
            enemy.y = m.run.heroY; enemy.x = m.run.heroX + map.swordReach + .1f;
            Check(!m.InHuntingRange(enemy), "beyond sword tip rejects attack");
            enemy.x = m.run.heroX - map.swordReach * .8f; m.Tick(.05f);
            Check(m.run.facing == -1, "hero faces target on left");
            var grounded = Ready(); grounded.BeginCombat();
            grounded.run.heroX = 999; grounded.run.heroY = -999; grounded.Tick(.05f);
            Check(grounded.run.heroX == map.horizontalBounds.y && grounded.run.heroY == map.depthBounds.x, "ground bounds clamp hero");
            float ground = grounded.run.heroY; grounded.Tick(.5f);
            Check(grounded.run.heroY == ground, "idle hero has no jumping or gravity");
            var simultaneous = Ready();
            var a = Enemy(1, simultaneous.run.heroX + 1, simultaneous.run.heroY);
            var b = Enemy(2, simultaneous.run.heroX + 1.1f, simultaneous.run.heroY);
            a.attackTimer = b.attackTimer = .01f;
            simultaneous.run.enemies.Add(a); simultaneous.run.enemies.Add(b); simultaneous.BeginCombat();
            simultaneous.run.turnTimer = 100; simultaneous.Tick(.05f);
            Check(simultaneous.events.FindAll(e => e.type == "hurt").Count == 2, "two enemies attack independently without waiting for player");
            Check(simultaneous.run.comboIndex == 0, "enemy attacks do not advance or reset player sequence");
            var combo = Ready(); var front = Enemy(1, combo.run.heroX + 1, combo.run.heroY, 1);
            var rear = Enemy(2, combo.run.heroX + 1.2f, combo.run.heroY);
            combo.run.enemies.Add(front); combo.run.enemies.Add(rear); combo.SetPlanAction(0, "skill2"); combo.BeginCombat();
            combo.Tick(.4f);
            Check(front.hp == 0 && rear.hp == rear.maxHp, "multi-hit skill never spills to second enemy");
            for (int i = 0; i < 25 && rear.hp == rear.maxHp; i++) combo.Tick(.05f);
            Check(rear.hp < rear.maxHp, "following slot acquires next living target");
            var saved = SaveStore.Parse(JsonUtility.ToJson(new SaveEnvelope { meta = combo.meta, run = combo.run, hasRun = true }));
            Check(saved.version == 7 && saved.run.heroX == combo.run.heroX && saved.run.heroY == combo.run.heroY, "save preserves ground positions");
            var restored = new RunModel(saved.meta); Check(restored.Restore(saved.run), "auto hunting snapshot restores"); restored.paused = false;
            for (int i = 0; i < 20; i++) { combo.Tick(.05f); restored.Tick(.05f); }
            Check(JsonUtility.ToJson(combo.run) == JsonUtility.ToJson(restored.run), "restored movement attacks and RNG remain deterministic");
            var legacy = SaveStore.Parse(JsonUtility.ToJson(new SaveEnvelope { version = 5, meta = combo.meta, run = combo.run, hasRun = true }));
            Check(legacy.version == 7 && legacy.run.heroX == RunModel.HeroX, "old turn save migrates to auto hunting");
            var corrupt = JsonUtility.FromJson<RunSnapshot>(JsonUtility.ToJson(combo.run)); corrupt.heroY = float.NaN;
            Check(!new RunModel(combo.meta).Restore(corrupt), "non-finite ground position rejected");
            combo.PrepareCombat(); before = JsonUtility.ToJson(combo.run); combo.Tick(.5f);
            Check(before == JsonUtility.ToJson(combo.run), "editing pauses both sides and movement");
            var spawn = new RunModel(new MetaProgress()); spawn.Start(731); spawn.BeginCombat(); spawn.Tick(.05f);
            Check(spawn.run.enemies[0].y >= map.depthBounds.x && spawn.run.enemies[0].y <= map.depthBounds.y, "spawn uses walkable depth band");
            string result = "PASS: " + checks + " auto-hunting checks. Grounded X/Y pursuit, reach, independent enemy timers, single-target slots and v7 save continuity.";
            File.WriteAllText("../Docs/HUNTING_CHECKS.txt", result); return result;
        }
    }
}
