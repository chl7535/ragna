using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ragna
{
    public sealed class RagnaGame : MonoBehaviour
    {
        public TMP_FontAsset koreanFont;
        public Material spriteMaterial;
        public RunModel Model { get; private set; }
        GameUI ui;
        BattleView view;
        RunState? shownState;
        float saveTimer, messageTime, soundTimer, deathDelay;
        string message = "";
        AudioSource audioSource;
        AudioClip attackTone, skillTone, healTone, alertTone;
        bool inCamp;
        bool reviewPreview;
        void Start()
        {
            Application.targetFrameRate = 60; Screen.sleepTimeout = SleepTimeout.NeverSleep;
            CareerCatalog.Reload();
            var loaded = SaveStore.Load(); Model = new RunModel(loaded.meta);
            ui = new GameUI(koreanFont, Drink, Guard, PauseMenu, GrowthMenu);
            view = new BattleView(Camera.main, ui, spriteMaterial);
            audioSource = gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake = false; audioSource.volume = .18f;
            attackTone = Tone(330, .045f); skillTone = Tone(660, .15f); healTone = Tone(880, .16f); alertTone = Tone(130, .15f);
            if (loaded.run != null && Model.Restore(loaded.run))
            {
                inCamp = false; view.Update(Model, 0);
                if (Model.run.state == RunState.Fighting) PauseMenu();
                else { Model.paused = false; SyncState(); }
            }
            else Camp();
            if (SaveStore.LastError != null) Debug.LogWarning(SaveStore.LastError);
        }
        AudioClip Tone(float hz, float duration)
        {
            int count = Mathf.CeilToInt(22050 * duration); var samples = new float[count];
            for (int i = 0; i < count; i++) samples[i] = Mathf.Sin(i * hz * Mathf.PI * 2 / 22050) * Mathf.Pow(1f - (float)i / count, 2) * .3f;
            var clip = AudioClip.Create("OriginalTone" + hz, count, 1, 22050, false); clip.SetData(samples, 0); return clip;
        }
        void Update()
        {
            if (Model == null) return;
            float dt = Time.unscaledDeltaTime;
            messageTime = Mathf.Max(0, messageTime - dt); soundTimer = Mathf.Max(0, soundTimer - dt);
            if (Keyboard.current != null)
            {
                if (Keyboard.current.qKey.wasPressedThisFrame) Drink();
                if (Keyboard.current.wKey.wasPressedThisFrame) Guard();
                if (Keyboard.current.escapeKey.wasPressedThisFrame && !inCamp) PauseMenu();
            }
            if (Model.run != null)
            {
                Model.Tick(dt);
                if (deathDelay > 0) deathDelay = Mathf.Max(0, deathDelay - dt);
                view.Update(Model, Model.IsActive || (!Model.paused && Model.run.state == RunState.Dead) ? dt : 0);
                ProcessEvents(); SyncState();
                ui.Update(Model, message, messageTime);
                saveTimer += dt;
                if (saveTimer >= 5) { Save(); saveTimer = 0; }
            }
            else ui.SafeArea();
        }
        void ProcessEvents()
        {
            foreach (var ev in Model.events)
            {
                view.Event(ev, Model);
                if (ev.type == "death") deathDelay = .65f;
                if (ev.type == "level") Toast("LEVEL UP  ·  " + Model.run.level);
                if (ev.type == "boss") Toast(Model.run.floor == 30 ? "월식의 군주" : "보스 출현");
                if (ev.type == "promoted") Toast("승급 완료 · 새로운 힘");
                if (ev.type == "hurt" && Model.run.hp < Model.MaxHp * .3f && Model.meta.vibration && Application.isMobilePlatform) Handheld.Vibrate();
                if (Model.meta.sound && soundTimer <= 0)
                {
                    var clip = ev.type == "skill" ? skillTone : ev.type == "potion" || ev.type == "level" || ev.type == "promoted" ? healTone : ev.type == "hurt" ? alertTone : ev.type == "attack" ? attackTone : null;
                    if (clip != null) { audioSource.PlayOneShot(clip); soundTimer = .06f; }
                }
            }
            Model.events.Clear();
        }
        void Toast(string text) { message = text; messageTime = 2.2f; }
        public void BeginRun(uint seed = 0)
        {
            Model.Start(seed == 0 ? (uint)DateTime.UtcNow.Ticks : seed);
            inCamp = false; shownState = null; view.Reset(); view.SetVisible(true); ui.Close();
            Save(); SyncState();
            if (!Model.meta.tutorialSeen) Tutorial();
        }
        void Camp()
        {
            inCamp = true; shownState = null; view.SetVisible(false);
            ui.Camp(Model.meta, () => BeginRun(), Settings);
        }
        void Tutorial()
        {
            Model.paused = true;
            var p = ui.Open("생존의 첫걸음", "공격은 자동으로, 생존은 직접");
            ui.Text(p, "적 아래 붉은 원이 깜박이면 공격 직전입니다.\n\n물약: HP 회복 · 5초 대기\n회피 보호막: 1.6초 동안 피해 85% 감소\n막기에 성공하면 즉시 반격 스킬 발동\n보호막 재사용: 8초\n\n물약은 한정되어 있습니다.\n5층마다 보스를 이기면 3개를 보충합니다.\n\n강화 선택과 앱을 떠난 동안 전투 정지", .07f, .22f, .93f, .73f, 24);
            ui.Button(p, "알겠어요 · 전투 시작", .07f, .07f, .93f, .18f, () => { Model.meta.tutorialSeen = true; Model.paused = false; ui.Close(); Save(); }, 27);
        }
        void SyncState()
        {
            if (Model.run == null || inCamp || shownState == Model.run.state) return;
            if (Model.run.state == RunState.Dead && deathDelay > 0) return;
            shownState = Model.run.state;
            switch (Model.run.state)
            {
                case RunState.Fighting: ui.Close(); break;
                case RunState.Blessing:
                    ui.Blessings(Model, index => { if (Model.ChooseBlessing(index)) { Save(); SyncState(); } }); break;
                case RunState.Promotion:
                    ui.Promotion(Model, career => { if (Model.Promote(career)) { ProcessEvents(); Save(); SyncState(); } }); break;
                case RunState.Dead: Result(false); break;
                case RunState.Victory: Result(true); break;
            }
            Save();
        }
        void Result(bool won)
        {
            Model.paused = false;
            var r = Model.run;
            var p = ui.Open(won ? "월식을 넘어" : "이번 여정의 끝", won ? "30층 정복 · 당신의 성장이 길을 열었습니다" : "쓰러져도, 다음 여정은 계속됩니다");
            ui.Text(p, Model.JobName + "  LV." + r.level + "\n" + r.floor + "층 · 처치 " + r.kills + "\n스킬 LV." + r.skillLevel + " · 진화 " + (Model.SkillTier + 1) + "단계\n" + TimeSpan.FromSeconds(r.time).ToString(@"mm\:ss"), .06f, .42f, .94f, .72f, 30);
            ui.Text(p, won ? "다른 직업과 강화 조합으로 다시 도전하세요." : "적의 공격 직전에 보호막을 쓰고,\nHP가 낮아지기 전에 물약을 준비하세요.", .07f, .28f, .93f, .40f, 23);
            if (Model.CanRebirth) ui.Button(p, "환생 · " + JobData.Passives[(int)r.job] + " 계승", .07f, .17f, .93f, .26f, RebirthMenu, 25);
            ui.Button(p, "캠프로 돌아가기", .07f, .05f, .93f, .15f, EndRun, 28);
        }
        void EndRun() { Model.run = null; Model.paused = false; view.Reset(); Save(); Camp(); }
        void Drink()
        {
            if (Model != null && Model.DrinkPotion()) { ProcessEvents(); Save(); }
        }
        void Guard()
        {
            if (Model != null && Model.Guard()) { ProcessEvents(); Save(); }
        }
        void PauseMenu()
        {
            if (inCamp || Model.run == null) return;
            if (Model.run.state != RunState.Fighting) return;
            Model.paused = true; Save();
            var p = ui.Open("잠시 숨 고르기", "진행 상황이 기기에 저장됩니다");
            ui.Text(p, "LV." + Model.run.level + " · " + Model.run.floor + "층\n환생 " + Model.meta.rebirths + "회\n\n게임이 닫혀 있는 동안에는\n전투가 진행되지 않습니다.", .08f, .42f, .92f, .71f, 28);
            ui.Button(p, "계속하기", .07f, .30f, .93f, .41f, () => { Model.paused = false; ui.Close(); }, 28);
            ui.Button(p, "설정 · 계승 도감", .07f, .18f, .93f, .28f, Settings, 25);
            ui.Button(p, "이번 도전 포기", .07f, .06f, .93f, .16f, AbandonMenu, 25);
        }
        void AbandonMenu()
        {
            var p = ui.Open("캠프로 돌아갈까요?", "현재 도전의 레벨과 강화는 초기화됩니다");
            ui.Text(p, "이미 계승한 패시브와\n최고 기록은 유지됩니다.", .08f, .40f, .92f, .68f, 28);
            ui.Button(p, "계속 도전", .07f, .23f, .93f, .35f, PauseMenu, 28);
            ui.Button(p, "도전 포기 · 캠프로", .07f, .07f, .93f, .19f, EndRun, 27);
        }
        void GrowthMenu()
        {
            if (inCamp || Model.run == null || Model.run.state != RunState.Fighting) return;
            Model.paused = true;
            var r = Model.run; var p = ui.Open("성장의 기록", Model.JobName + " · " + r.promotion + "차 승급 · LV." + r.level);
            string text = "공격력 " + Model.Power.ToString("0") + "   ·   치명타 " + (Model.CritChance * 100).ToString("0") + "%\n" +
                "공격 간격 " + Model.AttackInterval.ToString("0.00") + "초\n" +
                JobData.Skills[(int)r.job] + " LV." + r.skillLevel + " · 진화 " + (Model.SkillTier + 1) + "/4\n" +
                "스킬 피해 배율 " + Model.SkillMultiplier.ToString("0.0") + "배\n\n";
            for (int i = 0; i < r.upgrades.Length; i++) if (r.upgrades[i] > 0) text += GameUI.BlessingName((Blessing)i) + " ×" + r.upgrades[i] + "\n";
            ui.Text(p, text, .08f, .25f, .92f, .74f, 23, null, TextAlignmentOptions.TopLeft);
            ui.Text(p, r.promotion < 3 ? "다음 승급: LV." + Model.NextPromotion + " · 스킬 진화: LV.4 / 8 / 12" : "3차 승급 완료 · 환생으로 패시브를 계승하세요", .06f, .17f, .94f, .25f, 20);
            if (Model.CanRebirth) ui.Button(p, "환생하기", .07f, .09f, .51f, .17f, RebirthMenu, 25);
            ui.Button(p, "전투로", Model.CanRebirth ? .54f : .07f, .055f, .93f, .14f, () => { Model.paused = false; ui.Close(); }, 25);
        }
        void RebirthMenu()
        {
            if (!Model.CanRebirth) return;
            Model.paused = true;
            Job job = Model.run.job;
            var p = ui.Open("새로운 시작", "현재 도전을 마치고 영구적인 힘을 얻습니다");
            ui.Text(p, JobData.Passives[(int)job] + "  " + (Model.meta.passives[(int)job] + 1) + "/5\n\n" + JobData.PassiveDescriptions[(int)job] +
                "\n\n모든 직업에서 계승 효과 적용\n환생당 기본 HP +2% · 공격력 +2.5%\n다음 도전은 LV.1부터 시작", .08f, .30f, .92f, .72f, 27);
            ui.Button(p, "패시브 계승 · 환생 확정", .07f, .16f, .93f, .28f, () => { if (Model.Rebirth()) { view.Reset(); Save(); Camp(); } }, 27);
            ui.Button(p, "돌아가기", .07f, .05f, .93f, .14f, () => { if (Model.run.state == RunState.Fighting) GrowthMenu(); else Result(Model.run.state == RunState.Victory); }, 25);
        }
        void Settings()
        {
            var p = ui.Open("여정의 준비", "소리 · 진동 · 영구 계승");
            ui.Button(p, "효과음  " + (Model.meta.sound ? "켜짐" : "꺼짐"), .07f, .65f, .49f, .75f, () => { Model.meta.sound = !Model.meta.sound; Save(); Settings(); }, 24);
            ui.Button(p, "진동  " + (Model.meta.vibration ? "켜짐" : "꺼짐"), .52f, .65f, .93f, .75f, () => { Model.meta.vibration = !Model.meta.vibration; Save(); Settings(); }, 24);
            string text = "";
            for (int i = 0; i < 7; i++) text += JobData.Names[i] + " · " + JobData.Passives[i] + "  " + Model.meta.passives[i] + "/5\n<size=18>" + JobData.PassiveDescriptions[i] + "</size>\n\n";
            ui.Text(p, text, .07f, .13f, .93f, .62f, 22, null, TextAlignmentOptions.TopLeft);
            ui.Button(p, "돌아가기", .07f, .025f, .93f, .11f, () => { if (inCamp) Camp(); else PauseMenu(); }, 27);
        }
        void Save()
        {
            if (reviewPreview) return;
            if (Model != null && !SaveStore.Save(Model)) Toast("저장 공간을 확인해 주세요");
        }
#if UNITY_EDITOR
        public void PreviewBackground(int floor)
        {
            PreviewGrowth(0, (int)Job.Adventurer);
            Model.run.floor = Mathf.Clamp(floor, 1, 30);
            view.Update(Model, 0); ui.Update(Model, "", 0);
        }
        // Review fixtures never overwrite the player's save and are excluded from builds.
        public void PreviewGrowth(int tier, int job, string careerId = "knight")
        {
            reviewPreview = true;
            Model = new RunModel(new MetaProgress { tutorialSeen = true });
            Model.Start(123);
            Model.run.job = (Job)Mathf.Clamp(job, 0, 7); // Isolated art fixture, not a new-run entry point.
            var r = Model.run;
            r.promotion = Mathf.Clamp(tier, 0, 3);
            if (job == 0 && tier >= 2) r.careerId = careerId;
            r.level = new[] { 1, 20, 60, 100 }[r.promotion];
            r.skillLevel = new[] { 1, 4, 8, 12 }[r.promotion];
            r.hp = Model.MaxHp; r.time = 20;
            r.enemies.Add(new EnemyState { id = 1, kind = 3, boss = true, hp = 3000, maxHp = 4000, x = 2.4f, y = -1.5f, attackTimer = 2 });
            r.nextId = 1; r.spawned = 1;
            Model.paused = true; inCamp = false; shownState = RunState.Fighting;
            view.Reset(); view.SetVisible(true); ui.Close(); view.Update(Model, 0);
            view.Event(new CombatEvent("skill"), Model);
            view.Update(Model, .30f);
            Toast("성장 미리보기 · " + (tier + 1) + "단계");
        }
        public string ReviewUIFlow()
        {
            int checks = 0;
            void Require(bool valid, string label)
            { if (!valid) throw new InvalidOperationException("UI check failed: " + label); checks++; }
            void Click(string prefix)
            {
                foreach (var button in ui.root.GetComponentsInChildren<UnityEngine.UI.Button>())
                {
                    var label = button.GetComponentInChildren<TMP_Text>();
                    if (label != null && label.text.StartsWith(prefix, StringComparison.Ordinal))
                    { Require(button.interactable, prefix + " interactable"); button.onClick.Invoke(); return; }
                }
                throw new InvalidOperationException("UI button not found: " + prefix);
            }
            reviewPreview = true;
            Model = new RunModel(new MetaProgress());
            Camp(); Click("모험가로 도전 시작");
            Require(Model.run.job == Job.Adventurer && Model.run.level == 1, "single adventurer start");
            Require(Model.paused && ui.modal != null, "tutorial pauses first run");
            Click("알겠어요"); Require(Model.IsActive && ui.modal == null, "tutorial resumes combat");
            Model.run.hp = Model.MaxHp * .2f; ui.Update(Model, "", 0);
            Click("물약"); Require(Model.run.hp > Model.MaxHp * .2f && Model.run.potions == 4, "potion button heals");
            Click("정지"); Require(Model.paused && ui.modal != null, "pause button");
            Click("계속하기"); Require(!Model.paused && ui.modal == null, "resume button");
            Model.run.level = 20; Model.Tick(.05f); SyncState();
            Require(Model.run.state == RunState.Promotion, "promotion modal");
            Click("검사"); Require(Model.run.promotion == 1 && Model.run.job == Job.Warrior && Model.run.careerId == "", "first class choice button");
            Model.run.level = 60; Model.Tick(.05f); SyncState();
            Click("기사"); Require(Model.run.promotion == 2 && Model.run.careerId == "knight", "level 60 career choice button");
            Model.run.state = RunState.Blessing;
            Model.run.offers = new[] { Blessing.Power, Blessing.Skill, Blessing.Supplies }; SyncState();
            Click("날카로운 의지"); Require(Model.run.floor == 2 && Model.run.upgrades[0] == 1, "blessing card");
            Model.run.level = 100; Model.run.promotion = 3; GrowthMenu();
            Click("환생하기"); Click("패시브 계승");
            Require(Model.run == null && Model.meta.rebirths == 1 && Model.meta.passives[0] == 1 && inCamp, "rebirth and camp");
            Model = new RunModel(new MetaProgress { tutorialSeen = true }); BeginRun(731);
            Model.run.hp = 0; Model.run.state = RunState.Dead; SyncState();
            Require(ui.modal != null, "death results modal"); Click("캠프로 돌아가기");
            Require(inCamp && Model.run == null, "death retry path");
            return "PASS: " + checks + " UI checks (tutorial, potion, pause, promotion, blessing, rebirth, death). Save untouched.";
        }
#endif
        void OnApplicationPause(bool pause)
        {
#if UNITY_EDITOR
            if (reviewPreview) return;
#endif
            if (!pause || Model == null) return;
            if (!inCamp && Model.run?.state == RunState.Fighting) PauseMenu();
            Save();
        }
        void OnApplicationFocus(bool focus)
        {
#if UNITY_EDITOR
            if (reviewPreview) return;
#endif
            if (!focus && Model != null) { if (!inCamp && Model.run?.state == RunState.Fighting) PauseMenu(); Save(); }
        }
        void OnApplicationQuit() { Save(); }
    }
}
