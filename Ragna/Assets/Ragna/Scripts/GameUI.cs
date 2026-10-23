using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Ragna
{
    public sealed class GameUI
    {
        public readonly RectTransform root;
        public RectTransform modal;
        public TMP_FontAsset font;
        readonly Color ink = new Color32(14, 21, 34, 248);
        readonly Color panel = new Color32(25, 35, 51, 255);
        readonly Color white = new Color32(237, 237, 223, 255);
        readonly Color muted = new Color32(158, 175, 184, 255);
        readonly Color gold = new Color32(232, 192, 124, 255);
        TMP_Text floor, title, hp, level, skill, info, potionText, guardText, banner, hint;
        UnityEngine.UI.Image hpFill, xpFill, floorFill;
        UnityEngine.UI.Button potion, guard;
        Rect safe;
        public GameUI(TMP_FontAsset font, Action drink, Action defend, Action pause, Action growth)
        {
            this.font = font;
            var canvas = new GameObject("GameCanvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280); scaler.matchWidthOrHeight = .5f;
            root = Box(canvas.transform, "SafeArea", Vector2.zero, Vector2.one);
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            SafeArea();
            var header = Panel(root, "Header", 0, .795f, 1, 1, ink);
            Text(header, "R A G N A", .055f, .72f, .5f, .94f, 29, gold, TextAlignmentOptions.MidlineLeft);
            Button(header, "정지", .83f, .74f, .95f, .94f, pause, 21);
            title = Text(header, "별빛의 폐허", .055f, .48f, .65f, .73f, 27, white, TextAlignmentOptions.MidlineLeft);
            floor = Text(header, "01 / 30", .68f, .48f, .95f, .73f, 25, gold, TextAlignmentOptions.MidlineRight);
            floorFill = Bar(header, .055f, .37f, .945f, .41f, new Color32(139, 164, 185, 255));
            hpFill = Bar(header, .055f, .13f, .945f, .30f, new Color32(205, 88, 105, 255));
            hp = Text(header, "HP", .07f, .13f, .93f, .30f, 23, white);
            var bottom = Panel(root, "ControlPanel", 0, 0, 1, .285f, ink);
            level = Text(bottom, "검사 · LV.1", .055f, .78f, .70f, .96f, 26, white, TextAlignmentOptions.MidlineLeft);
            Button(bottom, "성장", .78f, .80f, .945f, .97f, growth, 23);
            xpFill = Bar(bottom, .055f, .71f, .945f, .735f, gold);
            skill = Text(bottom, "월광 참격", .055f, .51f, .95f, .69f, 23, gold, TextAlignmentOptions.MidlineLeft);
            potion = Button(bottom, "", .055f, .14f, .49f, .48f, drink, 26, new Color32(109, 47, 62, 255));
            potionText = potion.GetComponentInChildren<TMP_Text>();
            guard = Button(bottom, "", .515f, .14f, .945f, .48f, defend, 26, new Color32(42, 72, 99, 255));
            guardText = guard.GetComponentInChildren<TMP_Text>();
            info = Text(bottom, "공격과 스킬은 자동 · 생존은 당신의 선택", .04f, .015f, .96f, .13f, 19, muted);
            banner = Text(root, "", .06f, .72f, .94f, .79f, 33, gold);
            hint = Text(root, "", .08f, .29f, .92f, .35f, 21, gold);
        }
        public void SafeArea()
        {
            if (safe == Screen.safeArea) return;
            safe = Screen.safeArea;
            root.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            root.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            root.offsetMin = root.offsetMax = Vector2.zero;
        }
        public RectTransform Box(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform)); var r = go.GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero; return r;
        }
        public RectTransform Panel(Transform parent, string name, float x0, float y0, float x1, float y1, Color color)
        {
            var rect = Box(parent, name, new Vector2(x0, y0), new Vector2(x1, y1));
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = false;
            return rect;
        }
        public TMP_Text Text(Transform parent, string value, float x0, float y0, float x1, float y1, int size = 26, Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var r = Box(parent, "Label", new Vector2(x0, y0), new Vector2(x1, y1));
            var t = r.gameObject.AddComponent<TextMeshProUGUI>(); t.font = font; t.text = value; t.fontSize = size;
            t.color = color ?? white; t.alignment = align; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Ellipsis; return t;
        }
        public UnityEngine.UI.Button Button(Transform parent, string value, float x0, float y0, float x1, float y1, Action click, int size = 26, Color? color = null)
        {
            var r = Panel(parent, "Button", x0, y0, x1, y1, color ?? panel);
            var image = r.GetComponent<UnityEngine.UI.Image>(); image.raycastTarget = true;
            var b = r.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic = image;
            b.onClick.AddListener(() => click());
            var colors = b.colors; colors.highlightedColor = new Color(.85f, .9f, 1); colors.pressedColor = new Color(.65f, .75f, .85f); colors.disabledColor = new Color(.45f, .45f, .45f); b.colors = colors;
            Text(r, value, .04f, .04f, .96f, .96f, size); return b;
        }
        UnityEngine.UI.Image Bar(Transform parent, float x0, float y0, float x1, float y1, Color color)
        {
            var bg = Panel(parent, "Bar", x0, y0, x1, y1, new Color32(44, 48, 59, 255));
            var fill = Panel(bg, "Fill", 0, 0, 1, 1, color).GetComponent<UnityEngine.UI.Image>(); return fill;
        }
        void Fill(UnityEngine.UI.Image image, float value)
        { var r = image.rectTransform; r.anchorMax = new Vector2(Mathf.Clamp01(value), 1); }
        public void Update(RunModel model, string message, float messageTime)
        {
            SafeArea(); if (model.run == null) return;
            var r = model.run;
            title.text = model.Region + (r.floor % 5 == 0 ? " · 보스" : "");
            floor.text = r.floor.ToString("00") + " / 30";
            Fill(floorFill, (float)r.floorKills / model.FloorPopulation); Fill(hpFill, r.hp / model.MaxHp);
            hp.text = "HP  " + Mathf.CeilToInt(r.hp) + " / " + Mathf.CeilToInt(model.MaxHp);
            hpFill.color = r.hp < model.MaxHp * .3f ? new Color(1, .25f, .33f) : new Color32(205, 88, 105, 255);
            Fill(xpFill, r.xp / model.XpRequired);
            level.text = model.JobName + (r.promotion > 0 ? " " + r.promotion + "차" : "") + " · LV." + r.level;
            skill.text = JobData.Skills[(int)r.job] + "  LV." + r.skillLevel + "  ·  진화 " + (model.SkillTier + 1) + "/4";
            potionText.text = r.potionCd > 0 ? "물약  " + r.potionCd.ToString("0.0") + "초" : "물약  ×" + r.potions + "  [Q]";
            guardText.text = r.guardTime > 0 ? "보호막 유지 중" : r.guardCd > 0 ? "회피  " + r.guardCd.ToString("0.0") + "초" : "회피 보호막  [W]";
            potion.interactable = model.IsActive && r.potions > 0 && r.potionCd <= 0 && r.hp < model.MaxHp;
            guard.interactable = model.IsActive && r.guardCd <= 0;
            banner.text = messageTime > 0 ? message : "";
            hint.text = model.IsActive && r.hp < model.MaxHp * .3f ? "위험! 물약 또는 보호막을 사용하세요" : "";
        }
        public RectTransform Open(string heading, string subtitle = "")
        {
            Close();
            var shade = Panel(root, "ModalShade", 0, 0, 1, 1, new Color(0.025f, .04f, .075f, .94f));
            shade.GetComponent<UnityEngine.UI.Image>().raycastTarget = true; modal = shade;
            var content = Panel(shade, "ModalContent", .06f, .075f, .94f, .925f, ink);
            Panel(content, "TopRule", .04f, .975f, .96f, .979f, gold);
            Text(content, heading, .06f, .86f, .94f, .96f, 38, gold);
            Text(content, subtitle, .07f, .75f, .93f, .86f, 23, muted);
            return content;
        }
        public void Close() { if (modal != null) { modal.gameObject.SetActive(false); UnityEngine.Object.Destroy(modal.gameObject); } modal = null; }
        public void Camp(MetaProgress meta, Action start, Action settings)
        {
            var p = Open("R A G N A", "모든 여정은 모험가에서 시작됩니다");
            Text(p, "최고 " + meta.bestFloor + "층  ·  환생 " + meta.rebirths + "회  ·  정복 " + meta.victories + "회", .05f, .68f, .95f, .75f, 22, gold);
            Text(p, "모험가", .06f, .53f, .94f, .66f, 42, JobData.Colors[(int)Job.Adventurer]);
            Text(p, "LV.1 · 용기의 일격\n\n20레벨에 첫 직업을 선택합니다\n검사 · 궁수 · 법사 · 거너\n격투가 · 성직자 · 도적\n\n60·100레벨에 다음 전직과 승급\n환생 후에도 모험가로 다시 시작", .07f, .25f, .93f, .53f, 25);
            Button(p, "모험가로 도전 시작", .06f, .09f, .94f, .20f, start, 29, new Color32(99, 76, 47, 255));
            Button(p, "설정 · 계승 도감", .06f, .015f, .94f, .075f, settings, 21);
        }
        public void Blessings(RunModel model, Action<int> pick)
        {
            var p = Open("운명의 선택", model.run.floor + "층 정복 · 하나의 축복을 선택하세요\n선택하는 동안 전투는 멈춥니다");
            for (int i = 0; i < model.run.offers.Length; i++)
            {
                int index = i; var blessing = model.run.offers[i];
                string text = BlessingName(blessing) + "  +" + (model.run.upgrades[(int)blessing] + 1) + "\n<size=22>" + BlessingDescription(blessing) + "</size>";
                Button(p, text, .06f, .51f - i * .22f, .94f, .70f - i * .22f, () => pick(index), 28, i == 1 ? new Color32(41, 54, 68, 255) : panel);
            }
        }
        public static string BlessingName(Blessing b) => new[] { "날카로운 의지", "거인의 심장", "질풍", "스킬 각성", "붉은 갈증", "약점 간파", "여행자의 보급", "수호의 문장", "메아리" }[(int)b];
        public static string BlessingDescription(Blessing b) => new[] { "공격력 +16%", "최대 HP +16% · 증가분 즉시 회복", "공격 속도 +12%", "스킬 레벨 +1 · 진화 단계에 영향", "가한 피해의 1.2% 회복", "치명타 확률 +7%", "물약 +2 · 최대 HP의 20% 회복", "받는 피해 6% 감소 (최대 55%)", "자동 스킬의 타격 횟수 +1" }[(int)b];
        public void Promotion(RunModel model, Action<string> promote)
        {
            if (model.run.job == Job.Adventurer)
            {
                var selection = Open("첫 전직", "LV.20 · 모험가의 다음 길을 선택하세요\n선택하는 동안 전투는 멈춥니다");
                for (int i = 0; i < 7; i++)
                {
                    Job job = (Job)i; float top = .73f - i * .088f;
                    Button(selection, JobData.Names[i] + "  <size=18>" + JobData.Descriptions[i] + "</size>", .055f, top - .078f, .945f, top,
                        () => promote("job:" + job), 27, Color.Lerp(JobData.Colors[i], ink, .74f));
                }
                Text(selection, "HP 완전 회복 · 직업 스킬 획득 · 물약 +2", .05f, .035f, .95f, .10f, 21, gold);
                return;
            }
            var p = Open("한계를 넘어", "LV." + model.run.level + " · 새로운 힘이 깨어납니다");
            var choices = CareerCatalog.Choices(model.run.job, model.run.level, model.run.promotion + 1, model.run.careerId);
            if (choices.Length > 0)
            {
                Text(p, "전직할 길을 선택하세요\n100레벨에 선택한 길을 강화합니다", .06f, .62f, .94f, .75f, 24);
                float height = Mathf.Min(.22f, .52f / choices.Length);
                for (int i = 0; i < choices.Length; i++)
                {
                    var choice = choices[i]; float top = .59f - i * height;
                    Button(p, choice.displayName + "\n<size=21>" + choice.description + "</size>", .07f, top - height + .015f, .93f, top,
                        () => promote(choice.id), 31, Color.Lerp(choice.tint, ink, .7f));
                }
                Text(p, "전직 시 HP 완전 회복 · 스킬 +2 · 물약 +2", .06f, .055f, .94f, .13f, 22, gold);
                return;
            }
            Text(p, model.JobName + "  " + (model.run.promotion + 1) + "차 승급", .05f, .58f, .95f, .73f, 38, model.Accent);
            Text(p, "장비 외형과 오라 진화\n공격력 증가 · 스킬 레벨 +2\nHP 완전 회복 · 물약 +2" +
                (model.meta.rebirths > 0 ? "\n환생의 힘 +" + model.meta.rebirths : ""), .08f, .28f, .92f, .56f, 28);
            Text(p, "세부 전직 분기는 추후 함께 설계합니다", .05f, .20f, .95f, .27f, 20, muted);
            Button(p, "승급하기", .08f, .07f, .92f, .18f, () => promote(null), 30, new Color32(99, 76, 47, 255));
        }
    }
}
