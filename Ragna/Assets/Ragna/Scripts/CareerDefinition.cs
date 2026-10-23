using System;
using System.Linq;
using UnityEngine;

namespace Ragna
{
    [CreateAssetMenu(menuName = "Ragna/Career", fileName = "NewCareer")]
    public sealed class CareerDefinition : ScriptableObject
    {
        [Tooltip("Stable save identifier. Do not rename after shipping.")]
        public string id;
        public Job baseJob = Job.Warrior;
        public int unlockLevel = 60;
        [Range(2, 3)] public int promotionStage = 2;
        [Tooltip("Optional parent career ID. Blank allows any previous career.")]
        public string requiredCareerId;
        public string displayName;
        [TextArea] public string description;
        [Min(.1f)] public float damageMultiplier = 1;
        [Min(.1f)] public float healthMultiplier = 1;
        [Min(.1f)] public float attackSpeedMultiplier = 1;
        [Range(.1f, 2)] public float damageTakenMultiplier = 1;
        [Range(0, 2)] public float guardDurationBonus;
        public Color tint = Color.white;
        [Tooltip("Generated placeholder: 0=standard, 1=shield, 2=greatsword")]
        public int appearance;
        [Tooltip("Optional original sprites per promotion tier (0 to 3), replacing generated art.")]
        public Sprite[] promotionSprites;
    }

    public static class CareerCatalog
    {
        static CareerDefinition[] all;
        public static CareerDefinition[] All => all ??= Load();
        static CareerDefinition[] Load()
        {
            var entries = Resources.LoadAll<CareerDefinition>("Careers");
            var ids = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in entries)
                if (string.IsNullOrWhiteSpace(entry.id) || !ids.Add(entry.id))
                    throw new InvalidOperationException("Career IDs must be nonempty and unique: " + entry.name);
            return entries.OrderBy(e => e.id, StringComparer.Ordinal).ToArray();
        }
        public static void Reload() { all = Load(); }
        public static CareerDefinition Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var career in All) if (career.id == id) return career;
            return null;
        }
        public static CareerDefinition[] Choices(Job job, int level, int promotionStage = 2, string parentId = null) =>
            All.Where(c => c.baseJob == job && c.unlockLevel <= level && c.promotionStage == promotionStage &&
                (string.IsNullOrEmpty(c.requiredCareerId) || c.requiredCareerId == parentId)).ToArray();
    }
}
