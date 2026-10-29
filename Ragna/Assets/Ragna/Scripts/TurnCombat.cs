using System;
using UnityEngine;

namespace Ragna
{
    public enum TurnSide { Player, Enemy }

    public sealed partial class RunModel
    {
        public int SlotCapacity => 1 + meta.rebirths + (run != null && run.promotion >= 1 ? 1 : 0);
        public string HuntingLabel => "자동사냥 · 몬스터 추적";
        public bool CanPrepare => run != null && (run.state == RunState.Preparation || run.state == RunState.Fighting);

        public void ResizePlan()
        {
            if (run == null) return;
            var old = run.attackPlan ?? Array.Empty<string>();
            var next = new string[SlotCapacity];
            for (int i = 0; i < next.Length; i++) next[i] = "basic";
            for (int i = 0; i < Mathf.Min(old.Length, next.Length); i++) next[i] = old[i];
            run.attackPlan = NormalizePlan(next);
        }
        string[] NormalizePlan(string[] source)
        {
            var result = new string[SlotCapacity];
            for (int i = 0; i < result.Length; i++) result[i] = "basic";
            for (int i = 0; i < Mathf.Min(source.Length, result.Length);)
            {
                var action = CombatActionCatalog.Find(source[i]);
                if (action == null || action.minSkillLevel > run.skillLevel || i + action.slotCost > result.Length) { i++; continue; }
                result[i] = action.id;
                for (int k = 1; k < action.slotCost; k++) result[i + k] = "";
                i += action.slotCost;
            }
            return result;
        }
        void ImportPreferredPlan()
        { if (meta.preferredPlan.Length > 0) run.attackPlan = NormalizePlan(meta.preferredPlan); }
        public int PlanAnchor(int index)
        {
            if (index < 0 || index >= run.attackPlan.Length) return -1;
            while (index > 0 && string.IsNullOrEmpty(run.attackPlan[index])) index--;
            return index;
        }
        public bool SetPlanAction(int slot, string actionId)
        {
            if (run == null || run.state != RunState.Preparation || slot < 0 || slot >= SlotCapacity) return false;
            var action = CombatActionCatalog.Find(actionId);
            if (action == null || action.minSkillLevel > run.skillLevel || slot + action.slotCost > SlotCapacity) return false;
            int end = slot + action.slotCost;
            // Replacing a covered cell removes its complete old skill, not only its label.
            for (int i = slot; i < end; i++)
            {
                int anchor = PlanAnchor(i); var previous = CombatActionCatalog.Find(run.attackPlan[anchor]);
                int length = previous != null ? previous.slotCost : 1;
                for (int k = anchor; k < Mathf.Min(SlotCapacity, anchor + length); k++) run.attackPlan[k] = "basic";
            }
            run.attackPlan[slot] = action.id;
            for (int i = slot + 1; i < end; i++) run.attackPlan[i] = "";
            return true;
        }
        public bool PrepareCombat()
        {
            if (!CanPrepare) return false;
            run.state = RunState.Preparation; paused = false; ResizePlan(); return true;
        }
        public bool BeginCombat()
        {
            if (run == null || run.state != RunState.Preparation) return false;
            ResizePlan(); meta.preferredPlan = (string[])run.attackPlan.Clone();
            run.state = RunState.Fighting; paused = false; return true;
        }
        bool ValidateHuntingState()
        {
            if (!Enum.IsDefined(typeof(TurnSide), run.turnSide) || !Finite(run.turnTimer) || run.turnTimer < 0 ||
                !Finite(run.heroX) || !Finite(run.heroY) || (run.facing != 1 && run.facing != -1) ||
                run.attackPlan == null || run.attackPlan.Length != SlotCapacity || run.activePlan == null || run.activePlan.Length > SlotCapacity ||
                run.comboIndex < 0 || run.comboIndex > run.activePlan.Length) return false;
            bool Valid(string[] plan)
            {
                for (int i = 0; i < plan.Length;)
                {
                    var action = CombatActionCatalog.Find(plan[i]);
                    if (action == null || action.minSkillLevel > run.skillLevel || i + action.slotCost > plan.Length) return false;
                    for (int k = 1; k < action.slotCost; k++) if (!string.IsNullOrEmpty(plan[i + k])) return false;
                    i += action.slotCost;
                }
                return true;
            }
            return Valid(run.attackPlan) && Valid(run.activePlan);
        }
        void HuntingStep(float dt)
        {
            run.time += dt;
            run.leechPool = Mathf.Min(MaxHp * .03f, run.leechPool + MaxHp * .02f * dt);
            run.potionCd = Mathf.Max(0, run.potionCd - dt);
            run.guardCd = Mathf.Max(0, run.guardCd - dt);
            run.guardTime = Mathf.Max(0, run.guardTime - dt);
            run.spawnTimer -= dt;
            if (run.spawned < FloorPopulation && run.spawnTimer <= 0)
            { SpawnEnemy(); run.spawnTimer = run.floor % 5 == 0 ? 4 : 1.7f; }
            var map = HuntingMapSettings.Current;
            var heroPoint = map.Clamp(new Vector2(run.heroX, run.heroY));
            var target = Nearest();
            run.heroMoving = false;
            if (target != null)
            {
                run.facing = target.x >= run.heroX ? 1 : -1;
                if (!InHuntingRange(target))
                {
                    var destination = map.Clamp(new Vector2(target.x - run.facing * map.swordReach * .85f, target.y));
                    var next = Vector2.MoveTowards(heroPoint, destination, map.heroSpeed * dt);
                    run.heroMoving = (next - heroPoint).sqrMagnitude > .000001f;
                    heroPoint = next;
                }
            }
            run.heroX = heroPoint.x; run.heroY = heroPoint.y;
            foreach (var enemy in run.enemies)
            {
                if (enemy.hp <= 0) continue;
                enemy.flash = Mathf.Max(0, enemy.flash - dt);
                if (!InHuntingRange(enemy))
                {
                    float direction = enemy.x >= run.heroX ? 1 : -1;
                    var destination = map.Clamp(new Vector2(run.heroX + direction * map.swordReach * .85f, run.heroY));
                    var next = Vector2.MoveTowards(map.Clamp(new Vector2(enemy.x, enemy.y)), destination, map.enemySpeed * dt);
                    enemy.x = next.x; enemy.y = next.y;
                }
                enemy.attackTimer = Mathf.Max(0, enemy.attackTimer - dt);
            }
            run.turnTimer = Mathf.Max(0, run.turnTimer - dt);
            target = Nearest();
            if (run.turnTimer <= 0 && target != null && InHuntingRange(target)) ExecutePlayerAction();
            foreach (var enemy in run.enemies)
            {
                if (enemy.hp <= 0 || enemy.attackTimer > 0 || !InHuntingRange(enemy)) continue;
                run.enemyActorId = enemy.id; ExecuteEnemyAction();
                enemy.attackTimer = map.enemyAttackInterval;
                if (run.state == RunState.Dead) break;
            }
            run.enemies.RemoveAll(e => e.hp <= 0);
            if (run.state == RunState.Dead) return;
            if (NextPromotion > 0 && run.level >= NextPromotion)
            { run.state = RunState.Promotion; events.Add(new CombatEvent("promotionReady")); return; }
            if (run.floorKills >= FloorPopulation) ClearFloor();
        }
        void ExecutePlayerAction()
        {
            if (run.activePlan == null || run.activePlan.Length == 0)
            {
                run.activePlan = (string[])run.attackPlan.Clone(); run.comboIndex = 0;
                events.Add(new CombatEvent("comboStart"));
            }
            while (run.comboIndex < run.activePlan.Length && string.IsNullOrEmpty(run.activePlan[run.comboIndex])) run.comboIndex++;
            if (run.comboIndex >= run.activePlan.Length) { EndAttackSequence(); return; }
            var action = CombatActionCatalog.Find(run.activePlan[run.comboIndex]);
            run.comboIndex += action != null ? action.slotCost : 1;
            events.Add(new CombatEvent("planAction", amount: run.comboIndex));
            if (action == null || action.basicAttack)
            {
                var target = Nearest();
                Hit(target, Power * (run.counterBoost ? 1.25f : 1), "attack");
                if (target.hp > 0 && Random01() < .06f * P(Job.Warrior)) Hit(target, Power, "double");
            }
            else ExecuteSkill(action);
            run.counterBoost = false;
            if (run.comboIndex >= run.activePlan.Length) EndAttackSequence();
            else run.turnTimer = AttackInterval;
        }
        void EndAttackSequence()
        {
            run.activePlan = Array.Empty<string>(); run.comboIndex = 0;
            run.turnTimer = AttackInterval;
        }
        float DistanceSquared(EnemyState enemy) => (new Vector2(enemy.x - run.heroX, enemy.y - run.heroY)).sqrMagnitude;
        public bool InHuntingRange(EnemyState enemy) => enemy != null && enemy.hp > 0 &&
            Mathf.Abs(enemy.x - run.heroX) <= HuntingMapSettings.Current.swordReach &&
            Mathf.Abs(enemy.y - run.heroY) <= HuntingMapSettings.Current.depthTolerance;
        void ExecuteSkill(CombatActionDefinition action)
        {
            // A slot owns one target, including every hit of a multi-hit skill.
            // Only the next slot may acquire another enemy after this one dies.
            var target = Nearest();
            if (target == null) return;
            int tier = Mathf.Clamp(SkillTier + action.effectTier, 0, 3);
            int baseHits = 1;
            int hits = baseHits + tier + U(Blessing.Multicast) + action.bonusHits;
            events.Add(new CombatEvent("skill", amount: action.slotCost, visualTier: tier));
            float damage = Power * SkillMultiplier * action.damageMultiplier * (run.counterBoost ? 1.25f : 1) / (1 + tier * .45f) / Mathf.Sqrt(baseHits);
            for (int i = 0; i < hits && target.hp > 0; i++)
                Hit(target, damage, "skillhit");
        }
        void ExecuteEnemyAction()
        {
            var actor = run.enemies.Find(e => e.id == run.enemyActorId && e.hp > 0) ?? Nearest();
            if (!InHuntingRange(actor)) return;
            float amount = (9 + run.floor * 3.5f + run.floor * run.floor * .055f) * (actor.boss ? 1.2f : 1);
            if (actor.boss) amount += MaxHp * HuntingMapSettings.Current.bossHpDamage;
            if (actor.boss && run.floor == 30) amount *= 1.25f;
            if (run.guardTime > 0)
            {
                amount *= .15f; run.counterBoost = true;
                events.Add(new CombatEvent("blocked", actor.id));
            }
            amount *= Mathf.Max(.45f, 1 - .06f * U(Blessing.Barrier));
            amount *= Career != null ? Career.damageTakenMultiplier : 1;
            run.hp = Mathf.Max(0, run.hp - amount);
            events.Add(new CombatEvent("hurt", actor.id, amount));
            if (run.hp <= 0)
            { run.state = RunState.Dead; meta.bestFloor = Mathf.Max(meta.bestFloor, run.floor - 1); events.Add(new CombatEvent("death")); return; }

            run.enemyActorId = 0;
        }
    }
}
