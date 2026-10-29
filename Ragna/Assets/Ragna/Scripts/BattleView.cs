using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Ragna
{
    public sealed partial class BattleView
    {
        sealed class EnemyView
        {
            public SpriteRenderer body, shadow, warning, health;
            public Transform root;
            public Sprite original;
            public float deathTime, attackPulse;
            public bool dying;
            public GameObject hpUI;
            public UnityEngine.UI.Image hpFill;
            public TMP_Text hpValue;
        }
        sealed class Effect
        {
            public SpriteRenderer sprite;
            public TMP_Text text;
            public float life, total, delay;
            public Vector3 velocity;
            public float spin, growth;
            public Color color;
        }
        readonly Dictionary<int, EnemyView> enemies = new Dictionary<int, EnemyView>();
        readonly List<Effect> effects = new List<Effect>();
        readonly Stack<Effect> spritePool = new Stack<Effect>();
        readonly Stack<Effect> textPool = new Stack<Effect>();
        readonly Transform root;
        readonly Camera camera;
        readonly GameUI ui;
        readonly PanoramaView background;
        Vector3 HeroPosition = new Vector3(RunModel.HeroX, -1.5f, 0);
        readonly List<int> remove = new List<int>();
        readonly List<Rect> healthBounds = new List<Rect>();
        readonly Material spriteMaterial;
        GameObject healthPrefab, damagePrefab;
        SpriteRenderer hero, heroShadow, aura, shield;
        Sprite heroOriginal;
        string heroKey;
        int region = -1;
        float attackPulse, skillPulse, hurtPulse, deathTime, visualTime;
        public BattleView(Camera camera, GameUI ui, Material material)
        {
            this.camera = camera; this.ui = ui;
            spriteMaterial = material != null ? material : throw new System.InvalidOperationException("Run Ragna scene setup to assign the runtime sprite material.");
            root = new GameObject("BattleWorld").transform;
            healthPrefab = Resources.Load<GameObject>("CombatUI/EnemyHealth");
            damagePrefab = Resources.Load<GameObject>("CombatUI/DamageNumber");
            background = new PanoramaView(root, camera, spriteMaterial);
            var map = HuntingMapSettings.Current;
            foreach (float edge in new[] { map.depthBounds.x, map.depthBounds.y })
            {
                var boundary = Sprite("WalkableDepthBoundary", PixelArt.Shape("square"),
                    new Vector3((map.horizontalBounds.x + map.horizontalBounds.y) * .5f, edge, 0), -1);
                boundary.transform.localScale = new Vector3((map.horizontalBounds.y - map.horizontalBounds.x) * .5f, .012f, 1);
                boundary.color = new Color(.9f, .8f, .5f, .3f);
            }
            heroShadow = Sprite("HeroShadow", PixelArt.Shape("shadow"), new Vector3(RunModel.HeroX, -3f, 0), -2);
            heroShadow.color = new Color(0, 0, 0, .4f); heroShadow.transform.localScale = Vector3.one * .5f;
            aura = Sprite("GrowthAura", PixelArt.Shape("ring"), new Vector3(RunModel.HeroX, -2.8f, 0), -1);
            aura.transform.localScale = new Vector3(.7f, .23f, 1);
            hero = Sprite("Hero", PixelArt.Hero(Job.Warrior, 0), HeroPosition, 8);
            hero.transform.localScale = Vector3.one * 1.2f;
            shield = Sprite("GuardShield", PixelArt.Shape("ring"), HeroPosition, 12);
            shield.transform.localScale = Vector3.one * .8f;
        }
        SpriteRenderer Sprite(string name, Sprite sprite, Vector3 position, int order)
        {
            var go = new GameObject(name); go.transform.SetParent(root); go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = order;
            if (spriteMaterial != null) sr.sharedMaterial = spriteMaterial;
            return sr;
        }
        public void Reset()
        {
            foreach (var v in enemies.Values) { Object.Destroy(v.root.gameObject); if (v.hpUI != null) Object.Destroy(v.hpUI); }
            enemies.Clear();
            foreach (var e in effects) Release(e);
            effects.Clear(); heroKey = null; attackPulse = skillPulse = hurtPulse = deathTime = visualTime = 0;
        }
        public void SetVisible(bool visible)
        {
            root.gameObject.SetActive(visible);
            foreach (var v in enemies.Values) if (v.hpUI != null) v.hpUI.SetActive(visible && !v.dying);
        }
        public void Update(RunModel model, float dt)
        {
            if (model.run == null) return;
            var r = model.run; Color color = model.Accent;
            HeroPosition = new Vector3(r.heroX, r.heroY + 1.5f, 0);
            int ascension = Mathf.Min(5, model.meta.rebirths);
            var career = model.Career;
            string key = r.job + ":" + r.promotion + ":" + ascension + ":" + r.careerId;
            if (key != heroKey)
            {
                heroOriginal = career != null && career.promotionSprites != null && career.promotionSprites.Length > r.promotion && career.promotionSprites[r.promotion] != null
                    ? career.promotionSprites[r.promotion] : PixelArt.Hero(r.job, r.promotion, ascension, career != null ? career.appearance : 0, color);
                heroKey = key;
            }
            int nextRegion = (r.floor - 1) / 10;
            region = nextRegion; background.Update(r);
            visualTime += dt;
            attackPulse = Mathf.Max(0, attackPulse - dt * 3.4f); skillPulse = Mathf.Max(0, skillPulse - dt * 2);
            hurtPulse = Mathf.Max(0, hurtPulse - dt * 5);
            if (r.state == RunState.Dead) deathTime = Mathf.Min(.6f, deathTime + dt);
            bool walking = r.state == RunState.Fighting && r.heroMoving;
            PixelMotion motion = r.state == RunState.Dead ? PixelMotion.Death : hurtPulse > 0 ? PixelMotion.Hurt : r.guardTime > 0 ? PixelMotion.Guard :
                skillPulse > 0 ? PixelMotion.Cast : attackPulse > 0 ? PixelMotion.Attack : walking ? PixelMotion.Run : PixelMotion.Idle;
            int frame = motion == PixelMotion.Death ? Mathf.Min(3, (int)(deathTime * 7)) : motion == PixelMotion.Attack ? Mathf.Min(3, (int)((1 - attackPulse) * 4)) :
                motion == PixelMotion.Cast ? Mathf.Min(3, (int)((1 - skillPulse) * 4)) : (int)(visualTime * (walking ? 10 : 4)) % 4;
            hero.sprite = ReferenceHero.TryFrame(r.job, motion, frame, out var referenceFrame)
                ? referenceFrame : PixelArt.Animate(heroOriginal, motion, frame);
            float bob = 0; // Grounded movement only; no jump or vertical bob.

            hero.transform.position = HeroPosition + new Vector3((attackPulse * .35f - hurtPulse * .15f) * r.facing, bob, 0);
            hero.transform.rotation = Quaternion.identity;
            hero.flipX = r.facing < 0;
            int heroDepth = 100 + Mathf.RoundToInt(-r.heroY * 100);
            hero.sortingOrder = heroDepth;
            heroShadow.transform.position = new Vector3(r.heroX, r.heroY, 0);
            aura.transform.position = new Vector3(r.heroX, r.heroY + .1f, 0);
            shield.transform.position = HeroPosition; shield.sortingOrder = heroDepth + 1;
            hero.color = r.state == RunState.Dead ? new Color(.65f, .65f, .65f) : hurtPulse > .5f ? new Color(1, .6f, .65f) : Color.white;
            aura.enabled = r.promotion > 0 || model.meta.rebirths > 0;
            aura.color = new Color(color.r, color.g, color.b, .35f + .15f * Mathf.Sin(r.time * 2));
            aura.transform.localScale = new Vector3(.65f + .14f * r.promotion, .22f + .04f * r.promotion, 1);
            shield.enabled = r.guardTime > 0; shield.color = new Color(.6f, .88f, 1, .55f);
            remove.Clear();
            foreach (var entry in enemies)
            {
                var v = entry.Value;
                if (r.enemies.Exists(e => e.id == entry.Key && e.hp > 0)) continue;
                v.dying = true; v.deathTime += dt;
                v.health.enabled = v.warning.enabled = false;
                if (v.hpUI != null) v.hpUI.SetActive(false);
                v.body.sprite = PixelArt.Animate(v.original, PixelMotion.Death, Mathf.Min(3, (int)(v.deathTime * 9)), false);
                v.body.color = new Color(1, .75f, .8f, Mathf.Clamp01(1 - v.deathTime / .5f));
                if (v.deathTime >= .5f) remove.Add(entry.Key);
            }
            foreach (int id in remove) { Object.Destroy(enemies[id].root.gameObject); if (enemies[id].hpUI != null) Object.Destroy(enemies[id].hpUI); enemies.Remove(id); }
            healthBounds.Clear();
            foreach (var e in r.enemies)
            {
                if (e.hp <= 0) continue;
                if (!enemies.TryGetValue(e.id, out var v))
                {
                    v = new EnemyView(); v.root = new GameObject("Enemy" + e.id).transform; v.root.SetParent(root);
                    v.original = PixelArt.Enemy(e.kind);
                    v.body = Sprite("Body", v.original, Vector3.zero, 5);
                    v.body.transform.SetParent(v.root, false);
                    v.shadow = Sprite("Shadow", PixelArt.Shape("shadow"), Vector3.zero, -2); v.shadow.transform.SetParent(v.root, false);
                    v.shadow.transform.localPosition = new Vector3(0, -.65f, 0); v.shadow.transform.localScale = Vector3.one * (e.boss ? .65f : .4f); v.shadow.color = new Color(0, 0, 0, .4f);
                    v.warning = Sprite("AttackWarning", PixelArt.Shape("ring"), Vector3.zero, 1); v.warning.transform.SetParent(v.root, false);
                    v.warning.transform.localPosition = new Vector3(0, -.55f, 0);
                    v.health = Sprite("HP", PixelArt.Shape("square"), Vector3.zero, 11); v.health.transform.SetParent(v.root, false);
                    v.health.color = new Color32(205, 88, 105, 255); enemies[e.id] = v;
                    if (healthPrefab != null)
                    {
                        v.hpUI = Object.Instantiate(healthPrefab, ui.root);
                        v.hpUI.name = "EnemyHP_" + e.id;
                        v.hpFill = v.hpUI.transform.Find("Background/Fill").GetComponent<UnityEngine.UI.Image>();
                        v.hpValue = v.hpUI.GetComponentInChildren<TMP_Text>();
                    }
                }
                v.root.position = new Vector3(e.x, e.y + .65f, 0);
                v.body.sortingOrder = 100 + Mathf.RoundToInt(-e.y * 100);
                v.health.sortingOrder = v.body.sortingOrder + 1; v.body.flipX = e.x < r.heroX;
                v.attackPulse = Mathf.Max(0, v.attackPulse - dt * 4);
                var enemyMotion = e.flash > 0 ? PixelMotion.Hurt : v.attackPulse > 0 ? PixelMotion.Attack : !model.InHuntingRange(e) ? PixelMotion.Run : PixelMotion.Idle;
                v.body.sprite = PixelArt.Animate(v.original, enemyMotion, (int)(visualTime * 8 + e.id) % 4, e.kind != 0 && e.kind != 2);
                v.body.transform.localPosition = new Vector3(-v.attackPulse * .3f, 0, 0);
                v.body.color = e.flash > 0 ? Color.white * 1.5f : Color.white;
                if (e.boss) v.body.color *= Color.Lerp(Color.white, new Color(1, .65f, .75f), region * .2f);
                v.health.transform.localPosition = new Vector3(0, e.boss ? 1.8f : 1.1f, 0);
                v.health.transform.localScale = new Vector3((e.boss ? .8f : .4f) * e.hp / e.maxHp, .018f, 1);
                if (v.hpUI != null)
                {
                    v.health.enabled = false;
                    v.hpValue.text = Mathf.CeilToInt(e.hp) + " / " + Mathf.CeilToInt(e.maxHp);
                    var fillRect = v.hpFill.rectTransform;
                    fillRect.anchorMax = new Vector2(Mathf.Clamp01(e.hp / e.maxHp), 1);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(ui.root,
                        camera.WorldToScreenPoint(v.root.position + Vector3.up * (e.boss ? 2 : 1.25f)), null, out var hpPoint);
                    var hpRect = (RectTransform)v.hpUI.transform;
                    hpRect.anchorMin = hpRect.anchorMax = new Vector2(.5f, .5f);
                    var bounds = new Rect(hpPoint - hpRect.sizeDelta * .5f, hpRect.sizeDelta);
                    for (int attempt = 0; attempt < 8 && healthBounds.Exists(other => other.Overlaps(bounds)); attempt++)
                    { hpPoint.y += hpRect.sizeDelta.y + 4; bounds.position = hpPoint - hpRect.sizeDelta * .5f; }
                    healthBounds.Add(bounds); hpRect.anchoredPosition = hpPoint;
                    if (ui.modal != null) ui.modal.SetAsLastSibling();
                }
                bool warning = model.InHuntingRange(e) && e.attackTimer <= 1;
                v.warning.enabled = warning; v.warning.color = new Color(1, .22f, .28f, .6f + .3f * Mathf.Sin(r.time * 17));
                v.warning.transform.localScale = new Vector3(e.boss ? 1 : .6f, e.boss ? .35f : .2f, 1);
            }
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                var effect = effects[i]; float effectDt = dt;
                if (effect.delay > 0)
                {
                    float waited = Mathf.Min(effectDt, effect.delay); effect.delay -= waited; effectDt -= waited;
                    effect.sprite.gameObject.SetActive(effect.delay <= 0);
                    if (effect.delay > 0) continue;
                }
                effect.life -= effectDt;
                if (effect.life <= 0) { Release(effect); effects.RemoveAt(i); continue; }
                float alpha = Mathf.Clamp01(effect.life / Mathf.Min(.3f, effect.total));
                if (effect.sprite != null)
                {
                    var tr = effect.sprite.transform; tr.position += effect.velocity * effectDt;
                    tr.Rotate(0, 0, effect.spin * effectDt); tr.localScale += Vector3.one * effect.growth * effectDt;
                    effect.sprite.color = new Color(effect.color.r, effect.color.g, effect.color.b, effect.color.a * alpha);
                }
                else
                {
                    effect.text.rectTransform.anchoredPosition += new Vector2(effect.velocity.x, effect.velocity.y) * effectDt;
                    effect.text.color = new Color(effect.color.r, effect.color.g, effect.color.b, alpha);
                }
            }
        }
        static bool CanWalk(EnemyState e) => e.x > (e.boss ? -1.35f : RunModel.MeleeX) + .01f;
        void Release(Effect e)
        { if (e.sprite != null) { e.sprite.gameObject.SetActive(false); spritePool.Push(e); } else { e.text.gameObject.SetActive(false); textPool.Push(e); } }
        void Fx(string shape, Vector3 pos, Color color, float scale, float life, Vector3 velocity = default, float spin = 0, float growth = 0,
            float rotation = 0, Vector2? stretch = null, float delay = 0)
        {
            if (effects.Count >= 100) return;
            var e = spritePool.Count > 0 ? spritePool.Pop() : new Effect { sprite = Sprite("Effect", PixelArt.Shape(shape), pos, 1000) };
            e.sprite.gameObject.SetActive(true); e.sprite.sprite = PixelArt.Shape(shape); e.sprite.transform.position = pos;
            var size = stretch ?? Vector2.one;
            e.sprite.transform.localScale = new Vector3(scale * size.x, scale * size.y, 1); e.sprite.transform.rotation = Quaternion.Euler(0, 0, rotation);
            e.color = color; e.sprite.color = color; e.life = e.total = life; e.velocity = velocity; e.spin = spin; e.growth = growth; e.delay = delay;
            e.sprite.gameObject.SetActive(delay <= 0);
            effects.Add(e);
        }
        void Number(string value, Vector3 world, Color color, int size = 30)
        {
            if (effects.Count >= 100) return;
            var e = textPool.Count > 0 ? textPool.Pop() : new Effect { text = damagePrefab != null ? Object.Instantiate(damagePrefab, ui.root).GetComponent<TMP_Text>() : ui.Text(ui.root, "", 0, 0, 0, 0, size) };
            e.text.gameObject.SetActive(true); e.text.transform.SetAsLastSibling();
            // Keep damage text below a modal's raycast layer and headings.
            if (ui.modal != null) ui.modal.SetAsLastSibling();
            e.text.text = value; e.text.fontSize = damagePrefab != null ? damagePrefab.GetComponent<TMP_Text>().fontSize * size / 30f : size; e.text.color = color; e.color = color;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(ui.root, camera.WorldToScreenPoint(world), null, out var local);
            var rect = e.text.rectTransform; rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(230, 65);
            rect.anchoredPosition = local + new Vector2((effects.Count % 3 - 1) * 27, (effects.Count % 4) * 14);
            for (int attempt = 0; attempt < 8; attempt++)
            {
                var textBounds = new Rect(rect.anchoredPosition - rect.sizeDelta * .5f, rect.sizeDelta);
                if (!healthBounds.Exists(bounds => bounds.Overlaps(textBounds))) break;
                rect.anchoredPosition += Vector2.up * (rect.sizeDelta.y + 4);
            }
            e.life = e.total = .8f; e.velocity = new Vector3(0, 70, 0); effects.Add(e);
        }
        public void Event(CombatEvent ev, RunModel model)
        {
            Color color = model.Accent;
            Vector3 pos = enemies.TryGetValue(ev.enemy, out var target) ? target.root.position : new Vector3(0, -1, 0);
            if (ev.type == "attack" || ev.type == "double" || ev.type == "skillhit" || ev.type == "kill") pos = ev.position;
            if (ev.type == "attack" || ev.type == "double" || ev.type == "skillhit")
            {
                Number(Mathf.CeilToInt(ev.amount).ToString() + (ev.critical ? "!" : ""), pos + Vector3.up * 2.5f, ev.critical ? new Color(1, .83f, .4f) : Color.white, ev.critical ? 36 : 28);
                if (ev.type != "skillhit")
                {
                    attackPulse = 1;
                    Vector3 from = HeroPosition + new Vector3(.6f * model.run.facing, .1f, 0), delta = pos - from;
                    BasicAttack(model.run.job, from, delta, color);
                }
                if (ev.type == "skillhit") SkillImpact(model.run.job, pos, color, model.SkillTier);
                else Fx("spark", pos, color, .14f, .16f, default, 300, .4f);
            }
            else if (ev.type == "skill")
            {
                skillPulse = 1;
                int tier = ev.visualTier >= 0 ? ev.visualTier : model.SkillTier;
                Number(JobData.Skills[(int)model.run.job], HeroPosition + Vector3.up * 2, color, 25);
                RenderSkill(model, color, tier);
            }
            else if (ev.type == "hurt")
            {
                hurtPulse = 1;
                if (target != null) target.attackPulse = 1;
                Vector3 delta = HeroPosition - pos;
                Fx("slash", pos, new Color(1, .35f, .4f), .2f, .16f, delta / .16f, -500);
                Number("−" + Mathf.CeilToInt(ev.amount), HeroPosition + Vector3.up * 1.5f, new Color(1, .4f, .45f), 33);
            }
            else if (ev.type == "blocked") Number("막기 · 강화!", HeroPosition + Vector3.up * 2, color, 27);
            else if (ev.type == "potion") { Number("+" + Mathf.CeilToInt(ev.amount), HeroPosition + Vector3.up * 1.7f, new Color(.5f, 1, .7f)); Fx("ring", HeroPosition, new Color(.4f, 1, .7f), .3f, .6f, default, 0, 1.5f); }
            else if (ev.type == "kill") for (int i = 0; i < 4; i++) Fx("spark", pos, new Color(.8f, .7f, .5f), .05f, .5f, new Vector3((i - 1.5f) * 1.5f, 2, 0));
            else if (ev.type == "promoted" || ev.type == "level") Fx("ring", HeroPosition, color, .2f, 1, default, 0, 2);
        }
    }
}
