using UnityEngine;

namespace Ragna
{
    public sealed partial class BattleView
    {
        void BasicAttack(Job job, Vector3 from, Vector3 delta, Color color)
        {
            string shape = "slash";
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            Fx(shape, from, color, job == Job.Warrior ? .25f : .16f, .22f, delta / .22f,
                job == Job.Warrior ? -360 : 0, rotation: angle);
        }

        void RenderSkill(RunModel model, Color color, int tier)
        {
            Vector3 hand = HeroPosition + new Vector3(.65f * model.run.facing, .15f, 0);
            var nearest = model.Nearest();
            Vector3 target = nearest != null ? new Vector3(nearest.x, nearest.y + .65f, 0) : new Vector3(2.4f, -1.5f, 0);
            int count = tier + 1;
            float size = .3f + tier * .1f;
            // Each stage adds geometry as well as scale. Colours still follow the career.
            switch (model.run.job)
            {
                case Job.Adventurer:
                    Fx("slash", hand, color, .3f + tier * .08f, .55f, Vector3.right * (8 * model.run.facing), -100);
                    break;
                case Job.Warrior:
                    for (int i = 0; i < count; i++)
                    {
                        float angle = (i - tier * .5f) * 13;
                        Vector3 direction = Quaternion.Euler(0, 0, angle) * (Vector3.right * model.run.facing);
                        Fx("slash", hand, color, .46f + tier * .16f, .7f, direction * 9, -120, .18f, angle, delay: i * .06f);
                        if (tier >= 2) Fx("slash", hand, Color.white, .32f + tier * .1f, .55f, direction * 10, -120, rotation: angle, delay: i * .06f);
                    }
                    if (tier == 3) Fx(model.run.careerId == "berserker" ? "slash" : "rune", HeroPosition, color, .65f, .5f, spin: 120, growth: .5f);
                    break;
            }
        }

        void SkillImpact(Job job, Vector3 pos, Color color, int tier)
        {
            float size = .17f + tier * .045f;
            string shape = "spark";
            Fx(shape, pos, color, size, .2f + tier * .025f, spin: 120, growth: .5f);
            if (tier >= 2) Fx("spark", pos, Color.white, size * .5f, .12f);
        }
    }
}
