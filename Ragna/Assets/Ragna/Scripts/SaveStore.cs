using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Ragna
{
    [Serializable] public sealed class SaveEnvelope
    {
        public int version = 4;
        public bool hasRun;
        public MetaProgress meta;
        public RunSnapshot run;
    }
    public static class SaveStore
    {
        public static string PathName => Path.Combine(Application.persistentDataPath, "ragna-save.json");
        public static string LastError { get; private set; }
        public static SaveEnvelope Load()
        {
            LastError = null;
            foreach (string path in new[] { PathName, PathName + ".bak" })
            {
                if (!File.Exists(path)) continue;
                try
                {
                    return Parse(File.ReadAllText(path, Encoding.UTF8));
                }
                catch (Exception e) { LastError = "저장 파일 복원 실패: " + e.GetType().Name; }
            }
            return new SaveEnvelope { meta = new MetaProgress() };
        }
        public static SaveEnvelope Parse(string json)
        {
            var save = JsonUtility.FromJson<SaveEnvelope>(json);
            if (save == null || save.version < 1 || save.version > 4 || save.meta == null)
                throw new InvalidDataException("지원하지 않는 저장 형식");
            save.meta.Sanitize();
            // Unity's value serializer can materialize a null class as an empty object.
            // An explicit discriminator prevents that object from becoming a ghost run.
            if (save.version == 1) save.hasRun = save.run != null && save.run.state != RunState.Camp;
            if (!save.hasRun) save.run = null;
            else
            {
                if (save.version < 3 && save.run != null && save.run.enemies != null)
                    foreach (var enemy in save.run.enemies)
                    {
                        if (enemy == null) throw new InvalidDataException("진행 데이터 오류");
                        // Preserve distance, approach time and lane jitter from the vertical layout.
                        float oldX = enemy.x;
                        enemy.x = enemy.y - .65f;
                        enemy.y = enemy.boss ? -1.5f : -2f + oldX / 30f;
                    }
                if (save.version < 4 && save.run != null && save.run.promotion == 0)
                {
                    if (save.run.level < 20) { save.run.job = Job.Adventurer; save.run.careerId = ""; }
                    else
                    {
                        save.run.promotion = 1; // Preserve an existing level-20+ class choice.
                        if (save.run.state == RunState.Promotion) save.run.state = RunState.Fighting;
                    }
                }
                var check = new RunModel(save.meta);
                if (!check.Restore(save.run)) throw new InvalidDataException("진행 데이터 오류");
            }
            save.version = 4; return save;
        }
        public static bool Save(RunModel model)
        {
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                string json = JsonUtility.ToJson(new SaveEnvelope { meta = model.meta, run = model.run, hasRun = model.run != null }, true);
                string temp = PathName + ".tmp";
                File.WriteAllText(temp, json, Encoding.UTF8);
                if (File.Exists(PathName)) File.Replace(temp, PathName, PathName + ".bak");
                else File.Move(temp, PathName);
                LastError = null; return true;
            }
            catch (Exception e)
            { LastError = "저장 실패: " + e.GetType().Name; Debug.LogWarning(LastError); return false; }
        }
    }
}
