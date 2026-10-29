using System.IO;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

namespace Ragna.Editor
{
    public static class CombatSetup
    {
        [CliCommand("ragna_preparation_ui_checks", "Exercise actual preparation slot buttons without changing saves", MainThreadRequired = true)]
        public static string CheckUI()
        {
            if (!EditorApplication.isPlaying) throw new System.InvalidOperationException("Enter Play mode first.");
            var result = UnityEngine.Object.FindFirstObjectByType<RagnaGame>().ReviewTurnUI();
            File.WriteAllText("../Docs/PREPARATION_UI_CHECKS.txt", result); return result;
        }
        [CliCommand("ragna_combat_setup", "Create missing combat action data without changing authored values", MainThreadRequired = true)]
        public static string Run()
        {
            if (AssetDatabase.LoadAssetAtPath<HuntingMapSettings>("Assets/Ragna/Resources/HuntingMap.asset") == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<HuntingMapSettings>(), "Assets/Ragna/Resources/HuntingMap.asset");
            Directory.CreateDirectory("Assets/Ragna/Resources/CombatActions");
            Create("basic", "평타", true, 1, 1, 0, 1, 0);
            Create("skill1", "기본 스킬", false, 1, 1, 0, .9f, 0);
            Create("skill2", "강화 스킬", false, 2, 4, 1, 1.5f, 1);
            Create("skill3", "상급 스킬", false, 3, 8, 2, 2.1f, 2);
            Create("skill6", "궁극 스킬", false, 6, 12, 3, 4.8f, 4);
            AssetDatabase.SaveAssets(); CombatActionCatalog.Reload();
            return "Combat actions ready: basic 1 cell; skills 1/2/3/6 cells. Existing data preserved.";
        }
        static void Create(string id, string label, bool basic, int cost, int level, int tier, float damage, int hits)
        {
            string path = "Assets/Ragna/Resources/CombatActions/" + id + ".asset";
            if (AssetDatabase.LoadAssetAtPath<CombatActionDefinition>(path) != null) return;
            var action = ScriptableObject.CreateInstance<CombatActionDefinition>();
            action.id = id; action.displayName = label; action.basicAttack = basic; action.slotCost = cost;
            action.minSkillLevel = level; action.effectTier = tier; action.damageMultiplier = damage; action.bonusHits = hits;
            AssetDatabase.CreateAsset(action, path);
        }
    }
}
