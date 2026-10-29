using System.IO;
using TMPro;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ragna.Editor
{
    public static class WarriorAuthoring
    {
        [CliCommand("ragna_route_checks", "Verify warrior-only roster and removed-job save migration", MainThreadRequired = true)]
        public static string CheckRoute()
        {
            int checks = 0;
            if (System.Enum.GetValues(typeof(Job)).Length != 2 || JobData.AvailableJobs.Length != 1 || JobData.AvailableJobs[0] != Job.Warrior)
                throw new System.Exception("Only Adventurer and Warrior may remain"); checks++;
            foreach (int retiredId in new[] { 1, 2, 3, 4, 5, 6 })
            {
                var gate = new RunModel(new MetaProgress()); gate.Start(); gate.run.level = 20; gate.run.state = RunState.Promotion;
                if (gate.ChooseJob((Job)retiredId)) throw new System.Exception("Retired job can be selected"); checks++;
                foreach (int level in new[] { 10, 20, 60, 100 })
                {
                    var old = new RunModel(new MetaProgress { rebirths = 2 }); old.Start();
                    old.run.job = (Job)retiredId; old.run.level = level;
                    old.run.promotion = level < 20 ? 0 : level < 60 ? 1 : level < 100 ? 2 : 3;
                    old.ResizePlan(); old.meta.passives = new[] { 2, 3, 4, 5, 1, 2, 3 };
                    var converted = SaveStore.Parse(JsonUtility.ToJson(new SaveEnvelope { version = 6, meta = old.meta, run = old.run, hasRun = true }));
                    if (converted.version != 7 || converted.run.job != (level < 20 ? Job.Adventurer : Job.Warrior) || converted.run.level != level ||
                        converted.meta.rebirths != 2 || converted.meta.passives.Length != 1 || converted.meta.passives[0] != 2 ||
                        (level >= 60 && converted.run.careerId != "knight")) throw new System.Exception("Retired save migration failed"); checks++;
                }
            }
            var fresh = new RunModel(new MetaProgress()); fresh.Start();
            fresh.run.job = (Job)2;
            try { SaveStore.Parse(JsonUtility.ToJson(new SaveEnvelope { meta = fresh.meta, run = fresh.run, hasRun = true })); throw new System.Exception("Current invalid job was accepted"); }
            catch (InvalidDataException) { checks++; }
            string result = "PASS: " + checks + " warrior-only route checks. Retired job IDs rejected; v1-v6 migration retains progress and only Warrior passive.";
            File.WriteAllText("../Docs/ROUTE_CHECKS.txt", result); return result;
        }
        [CliCommand("ragna_warrior_stats_checks", "Verify editable damage and speed for Adventurer/Warrior only", MainThreadRequired = true)]
        public static string CheckStats()
        {
            int count = 0;
            foreach (Job job in new[] { Job.Adventurer, Job.Warrior })
            {
                var model = new RunModel(new MetaProgress()); model.Start();
                model.run.job = job;
                var stats = CombatStats.For(job);
                float damage = stats.baseDamage, perLevel = stats.damagePerLevel, speed = stats.attacksPerSecond;
                float skillBase = stats.skillBaseMultiplier;
                try
                {
                    float power = model.Power, interval = model.AttackInterval, skill = model.SkillMultiplier;
                    stats.baseDamage += 10;
                    if (Mathf.Abs(model.Power - power - 10) > .001f) throw new System.Exception("Damage Inspector value not used"); count++;
                    stats.damagePerLevel += 3;
                    if (Mathf.Abs(model.Power - power - 13) > .001f) throw new System.Exception("Damage growth not used"); count++;
                    stats.attacksPerSecond *= 2;
                    if (Mathf.Abs(model.AttackInterval - Mathf.Max(.22f, interval / 2)) > .001f) throw new System.Exception("Speed Inspector value not used"); count++;
                    stats.skillBaseMultiplier += 1;
                    if (Mathf.Abs(model.SkillMultiplier - skill - 1) > .001f) throw new System.Exception("Skill Inspector value not used"); count++;
                }
                finally { stats.baseDamage = damage; stats.damagePerLevel = perLevel; stats.attacksPerSecond = speed; stats.skillBaseMultiplier = skillBase; }
            }
            if (CombatStats.For(Job.Adventurer) == CombatStats.For(Job.Warrior)) throw new System.Exception("Profiles must be independent"); count++;
            var prefab = Resources.Load<GameObject>("CombatUI/EnemyHealth");
            if (prefab == null || prefab.transform.Find("Background/Fill") == null || prefab.GetComponentInChildren<TMP_Text>() == null) throw new System.Exception("HP prefab missing"); count++;
            if (Resources.Load<GameObject>("CombatUI/DamageNumber")?.GetComponent<TMP_Text>() == null) throw new System.Exception("Damage prefab missing"); count++;
            string result = "PASS: " + count + " Adventurer/Warrior stats and combat feedback asset checks. Authored values restored.";
            File.WriteAllText("../Docs/WARRIOR_STATS_CHECKS.txt", result); return result;
        }
        [CliCommand("ragna_warrior_authoring", "Create editable Adventurer/Warrior stats and combat feedback prefabs", MainThreadRequired = true)]
        public static string Setup()
        {
            Directory.CreateDirectory("Assets/Ragna/Resources/CombatStats");
            foreach (string name in new[] { "Adventurer", "Warrior" })
            {
                string path = "Assets/Ragna/Resources/CombatStats/" + name + ".asset";
                if (AssetDatabase.LoadAssetAtPath<CombatStats>(path) == null)
                    AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<CombatStats>(), path);
            }
            Directory.CreateDirectory("Assets/Ragna/Resources/CombatUI");
            ReadoutMaterial();
            string barPath = "Assets/Ragna/Resources/CombatUI/EnemyHealth.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(barPath) == null)
            {
                var go = new GameObject("EnemyHealth", typeof(RectTransform));
                go.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 42);
                var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
                bg.transform.SetParent(go.transform, false);
                Rect(bg.GetComponent<RectTransform>(), new Vector2(0,.08f), new Vector2(1,.3f));
                bg.GetComponent<Image>().color = new Color(.06f,.08f,.12f,.95f); bg.GetComponent<Image>().raycastTarget = false;
                var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fill.transform.SetParent(bg.transform, false); Rect(fill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
                fill.GetComponent<Image>().color = new Color(.85f,.24f,.34f); fill.GetComponent<Image>().raycastTarget = false;
                var text = Label("Value", go.transform); Rect(text.rectTransform, new Vector2(-.2f,.32f), new Vector2(1.2f,1));
                text.fontSize = 19; text.text = "100 / 100";
                PrefabUtility.SaveAsPrefabAsset(go, barPath); Object.DestroyImmediate(go);
            }
            string damagePath = "Assets/Ragna/Resources/CombatUI/DamageNumber.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(damagePath) == null)
            {
                var text = Label("DamageNumber", null); text.rectTransform.sizeDelta = new Vector2(230,65);
                text.fontSize = 32; text.text = "100";
                PrefabUtility.SaveAsPrefabAsset(text.gameObject, damagePath); Object.DestroyImmediate(text.gameObject);
            }
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            return "Adventurer/Warrior stats and EnemyHealth/DamageNumber prefabs ready. Existing edits preserved.";
        }
        static TMP_Text Label(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            var t = go.GetComponent<TMP_Text>(); t.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Ragna/Fonts/NanumGothic SDF.asset");
            t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false; t.color = Color.white;
            t.fontSharedMaterial = ReadoutMaterial();
            return t;
        }
        static Material ReadoutMaterial()
        {
            const string path = "Assets/Ragna/Resources/CombatUI/CombatText.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Ragna/Fonts/NanumGothic SDF.asset");
            material = new Material(font.material) { name = "CombatText" };
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, .15f);
            material.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
            AssetDatabase.CreateAsset(material, path); return material;
        }
        static void Rect(RectTransform rect, Vector2 min, Vector2 max)
        { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    }
}
