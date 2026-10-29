using System;
using TMPro;
using UnityEngine;

namespace Ragna
{
    public sealed partial class GameUI
    {
        public void Preparation(RunModel model, int selected, int page, Action<int> select, Action<string> pick, Action start, Action<int> changePage)
        {
            var p = Open("전투 준비", "몬스터 자동 추적 · 공격 슬롯 순서대로 반복\n기본 1 + 첫 전직 " + (model.run.promotion >= 1 ? 1 : 0) + " + 전승 " + model.meta.rebirths + " = " + model.SlotCapacity + "칸");
            int capacity = model.SlotCapacity, startSlot = page * 8;
            for (int i = 0; i < 8 && startSlot + i < capacity; i++)
            {
                int index = startSlot + i, anchor = model.PlanAnchor(index);
                var action = CombatActionCatalog.Find(model.run.attackPlan[anchor]);
                string label = (index + 1) + "번\n" + (anchor == index ? (action?.basicAttack == true ? "평타" : action?.displayName ?? "평타") : (anchor + 1) + "번 스킬 점유");
                int col = i % 4, row = i / 4;
                float left = .055f + col * .225f, top = .73f - row * .112f;
                Button(p, label, left, top - .10f, left + .213f, top, () => select(index), 21,
                    index == selected ? new Color32(74, 96, 104, 255) : panel);
            }
            if (capacity > 8)
            {
                var prev = Button(p, "이전", .055f, .475f, .25f, .515f, () => changePage(-1), 20); prev.interactable = page > 0;
                Text(p, (page + 1) + " / " + ((capacity + 7) / 8), .28f, .475f, .72f, .515f, 19, muted);
                var next = Button(p, "다음", .75f, .475f, .945f, .515f, () => changePage(1), 20); next.interactable = startSlot + 8 < capacity;
            }
            Text(p, (selected + 1) + "번 칸에 넣을 공격 선택", .055f, .432f, .945f, .475f, 23, gold);
            var actions = CombatActionCatalog.All;
            // The action list itself scrolls when new data assets are added later.
            var scrollRoot = Box(p, "ActionList", new Vector2(.055f, .112f), new Vector2(.945f, .43f));
            var scroll = scrollRoot.gameObject.AddComponent<UnityEngine.UI.ScrollRect>(); scroll.horizontal = false;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            var viewport = Box(scrollRoot, "Viewport", Vector2.zero, Vector2.one);
            viewport.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(1, 1, 1, .01f);
            var mask = viewport.gameObject.AddComponent<UnityEngine.UI.Mask>(); mask.showMaskGraphic = false;
            var content = Box(viewport, "Content", new Vector2(0, 1), new Vector2(1, 1)); content.pivot = new Vector2(.5f, 1);
            var layout = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = 6; layout.childControlHeight = layout.childControlWidth = true;
            layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
            var fitter = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>(); fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content;
            foreach (var action in actions)
            {
                var current = action;
                string label = current.Label(model.run.job) + "  ·  " + current.slotCost + "칸";
                bool unlocked = model.run.skillLevel >= current.minSkillLevel;
                bool fits = selected + current.slotCost <= capacity;
                if (!unlocked) label += "  [스킬 LV." + current.minSkillLevel + " 필요]";
                else if (!fits) label += "  [공간 부족]";
                var button = Button(content, label, 0, 0, 1, 1, () => pick(current.id), 21);
                var size = button.gameObject.AddComponent<UnityEngine.UI.LayoutElement>(); size.preferredHeight = 63;
                button.interactable = unlocked && fits;
            }
            Button(p, "설정 완료 · 전투 시작", .055f, .025f, .945f, .095f, start, 28, new Color32(99, 76, 47, 255));
        }
    }
}
