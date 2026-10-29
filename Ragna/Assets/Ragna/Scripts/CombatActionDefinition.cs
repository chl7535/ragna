using System;
using System.Linq;
using UnityEngine;

namespace Ragna
{
    [CreateAssetMenu(menuName = "Ragna/Combat Action", fileName = "NewCombatAction")]
    public sealed class CombatActionDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public bool basicAttack;
        [Min(1)] public int slotCost = 1;
        [Min(1)] public int minSkillLevel = 1;
        [Range(0, 3)] public int effectTier;
        [Min(.1f)] public float damageMultiplier = 1;
        [Min(0)] public int bonusHits;
        public string Label(Job job) => basicAttack ? "평타" : JobData.Skills[(int)job] + " · " + displayName;
    }

    public static class CombatActionCatalog
    {
        static CombatActionDefinition[] all;
        public static CombatActionDefinition[] All => all ??= Load();
        public static void Reload() => all = Load();
        static CombatActionDefinition[] Load()
        {
            var items = Resources.LoadAll<CombatActionDefinition>("CombatActions");
            var ids = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (var item in items)
                if (string.IsNullOrWhiteSpace(item.id) || !ids.Add(item.id) || item.slotCost < 1 || item.slotCost > 10002 ||
                    item.minSkillLevel < 1 || !float.IsFinite(item.damageMultiplier) || item.damageMultiplier <= 0)
                    throw new InvalidOperationException("Invalid combat action data: " + item.name);
            return items.OrderBy(item => item.slotCost).ThenBy(item => item.id, StringComparer.Ordinal).ToArray();
        }
        public static CombatActionDefinition Find(string id)
        {
            foreach (var item in All) if (item.id == id) return item;
            return null;
        }
    }
}
