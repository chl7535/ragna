using UnityEngine;

namespace Ragna
{
    [CreateAssetMenu(menuName = "Ragna/Adventurer Warrior Combat Stats")]
    public sealed class CombatStats : ScriptableObject
    {
        [Header("기본 공격 피해 = 기본값 + 레벨 × 레벨당 증가")]
        [Min(0)] public float baseDamage = 18;
        [Min(0)] public float damagePerLevel = 3.5f;
        [Header("공격속도 · 초당 공격 횟수")]
        [Min(.1f)] public float attacksPerSecond = 1f / .95f;
        [Header("스킬 피해 배율 = 기본값 + 스킬 레벨 × 증가")]
        [Min(0)] public float skillBaseMultiplier = 1.7f;
        [Min(0)] public float skillMultiplierPerLevel = .2f;
        static readonly System.Collections.Generic.Dictionary<Job, CombatStats> profiles = new System.Collections.Generic.Dictionary<Job, CombatStats>();
        public static CombatStats For(Job job)
        {
            if (!System.Enum.IsDefined(typeof(Job), job)) return null;
            if (!profiles.TryGetValue(job, out var profile) || profile == null)
                profiles[job] = profile = Load(job.ToString());
            return profile;
        }
        static CombatStats Load(string name) => Resources.Load<CombatStats>("CombatStats/" + name) ?? CreateInstance<CombatStats>();
    }
}
