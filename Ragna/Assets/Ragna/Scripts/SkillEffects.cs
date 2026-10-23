using UnityEngine;

namespace Ragna
{
    public sealed partial class BattleView
    {
        void BasicAttack(Job job, Vector3 from, Vector3 delta, Color color)
        {
            string shape = job == Job.Archer ? "arrow" : job == Job.Gunner ? "bullet" : job == Job.Rogue ? "dagger" :
                job == Job.Fighter ? "fist" : job == Job.Cleric ? "cross" : job == Job.Mage ? "rune" : "slash";
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            Fx(shape, from, color, job == Job.Warrior ? .25f : .16f, .22f, delta / .22f,
                job == Job.Warrior ? -360 : 0, rotation: angle);
            if (job == Job.Gunner) Fx("spark", from, Color.white, .16f, .08f);
        }

        void RenderSkill(RunModel model, Color color, int tier)
        {
            Vector3 hand = HeroPosition + new Vector3(.65f, .15f, 0);
            var nearest = model.Nearest();
            Vector3 target = nearest != null ? new Vector3(nearest.x, nearest.y, 0) : new Vector3(2.4f, -1.5f, 0);
            int count = tier + 1;
            float size = .3f + tier * .1f;
            // Each stage adds geometry as well as scale. Colours still follow the career.
            switch (model.run.job)
            {
                case Job.Adventurer:
                    Fx("slash", hand, color, .3f + tier * .08f, .55f, Vector3.right * 8, -100);
                    break;
                case Job.Warrior:
                    for (int i = 0; i < count; i++)
                    {
                        float angle = (i - tier * .5f) * 13;
                        Vector3 direction = Quaternion.Euler(0, 0, angle) * Vector3.right;
                        Fx("slash", hand, color, .46f + tier * .16f, .7f, direction * 9, -120, .18f, angle, delay: i * .06f);
                        if (tier >= 2) Fx("slash", hand, Color.white, .32f + tier * .1f, .55f, direction * 10, -120, rotation: angle, delay: i * .06f);
                    }
                    if (tier == 3) Fx(model.run.careerId == "berserker" ? "slash" : "rune", HeroPosition, color, .65f, .5f, spin: 120, growth: .5f);
                    break;
                case Job.Archer:
                    for (int i = -count; i <= count; i++)
                    {
                        float angle = i * (3 + tier);
                        Vector3 direction = Quaternion.Euler(0, 0, angle) * Vector3.right;
                        Fx("arrow", hand, color, .24f + tier * .035f, .65f, direction * (12 + tier), rotation: angle, delay: Mathf.Abs(i) * .035f);
                    }
                    if (tier >= 2) Fx("rune", hand, color, .4f + tier * .08f, .5f, spin: 100, stretch: new Vector2(.3f, 1));
                    break;
                case Job.Mage:
                    Fx("rune", hand, color, .35f + tier * .1f, .8f, spin: -90);
                    for (int i = 0; i < count; i++)
                    {
                        Vector3 impact = target + Vector3.right * ((i - tier * .5f) * .75f);
                        Vector3 start = impact + new Vector3(-1.8f, 4, 0);
                        Fx("meteor", start, color, size, .48f, (impact - start) / .48f, rotation: 0, delay: i * .09f);
                        Fx("ring", impact, color, size, .35f, growth: 1.6f + tier * .5f, stretch: new Vector2(1, .35f), delay: .46f + i * .09f);
                        Fx("spark", impact, Color.white, size * .65f, .2f, delay: .46f + i * .09f);
                    }
                    break;
                case Job.Gunner:
                    for (int burst = 0; burst < 2 + tier; burst++) for (int lane = 0; lane < count; lane++)
                    {
                        float angle = (lane - tier * .5f) * 6;
                        Vector3 direction = Quaternion.Euler(0, 0, angle) * Vector3.right;
                        Fx("bullet", hand + Vector3.up * (lane % 2 == 0 ? .08f : -.08f), color, .17f + tier * .035f, .5f,
                            direction * 18, rotation: angle, delay: burst * .09f);
                    }
                    Fx("spark", hand, new Color(1, .9f, .6f), .3f + tier * .08f, .18f, growth: .5f);
                    if (tier == 3) Fx("bullet", hand, Color.white, .3f, .38f, Vector3.right * 22, stretch: new Vector2(2, 1), delay: .3f);
                    break;
                case Job.Fighter:
                    for (int i = 0; i < 3 + tier * 2; i++)
                    {
                        Vector3 offset = new Vector3((i % 3) * .22f, (i % 2 == 0 ? .35f : -.35f), 0);
                        Fx("fist", hand + offset, color, size, .32f, Vector3.right * 13, delay: i * .045f);
                        Fx("ring", target + offset, color, size * .5f, .2f, growth: 1.2f, delay: .25f + i * .045f);
                    }
                    if (tier >= 2) Fx("ring", new Vector3(target.x, -2.8f, 0), color, .5f, .5f, growth: 2, stretch: new Vector2(1, .25f), delay: .35f);
                    break;
                case Job.Cleric:
                    for (int i = 0; i < count; i++)
                    {
                        Vector3 strike = target + Vector3.right * ((i - tier * .5f) * .9f);
                        Fx("square", strike + Vector3.up, new Color(color.r, color.g, color.b, .45f), .3f + tier * .07f, .5f,
                            stretch: new Vector2(.25f, 3.4f), delay: i * .08f);
                        Fx("cross", strike + Vector3.up * 1.7f, Color.white, size, .55f, delay: i * .08f);
                        Fx("rune", strike + Vector3.down * .9f, color, .4f + tier * .1f, .7f, spin: 45, stretch: new Vector2(1, .3f), delay: i * .08f);
                    }
                    Fx("cross", HeroPosition + Vector3.up, color, .2f, .65f, Vector3.up * 1.4f);
                    break;
                case Job.Rogue:
                    for (int i = 0; i < 2 + tier * 2; i++)
                    {
                        Vector3 offset = Vector3.up * (i % 2 == 0 ? .4f : -.4f);
                        Fx("dagger", hand + offset, color, .26f + tier * .04f, .4f, Vector3.right * 16, delay: i * .05f);
                        if (tier >= 1) Fx("slash", target + offset, color, size, .3f, spin: i % 2 == 0 ? 220 : -220, delay: .3f + i * .05f);
                    }
                    if (tier >= 2) for (int i = 0; i < 3; i++)
                        Fx("dagger", HeroPosition + Vector3.right * (i * .7f), new Color(color.r, color.g, color.b, .3f), .5f, .5f, Vector3.right * 10, delay: i * .05f);
                    break;
            }
        }

        void SkillImpact(Job job, Vector3 pos, Color color, int tier)
        {
            float size = .17f + tier * .045f;
            string shape = job == Job.Cleric ? "cross" : job == Job.Mage ? "rune" : job == Job.Fighter ? "ring" : job == Job.Rogue ? "slash" : "spark";
            Fx(shape, pos, color, size, .2f + tier * .025f, spin: 120, growth: .5f);
            if (tier >= 2) Fx("spark", pos, Color.white, size * .5f, .12f);
        }
    }
}
