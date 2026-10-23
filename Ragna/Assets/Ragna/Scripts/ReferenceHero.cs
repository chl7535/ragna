using UnityEngine;

namespace Ragna
{
    // Authored animation frames replace procedural deformation for the reference character.
    public static class ReferenceHero
    {
        static Sprite[,] frames;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => frames = null;
        public static bool TryFrame(Job job, PixelMotion motion, int frame, out Sprite sprite)
        {
            sprite = null;
            if (job != Job.Warrior && job != Job.Adventurer) return false;
            if (frames == null)
            {
                frames = new Sprite[7, 4];
                foreach (var candidate in Resources.LoadAll<Sprite>("Heroes/AdventurerSheet"))
                    for (int row = 0; row < 7; row++) for (int col = 0; col < 4; col++)
                        if (candidate.name == ((PixelMotion)row) + "_" + col) frames[row, col] = candidate;
            }
            sprite = frames[(int)motion, Mathf.Clamp(frame, 0, 3)];
            return sprite != null;
        }
    }
}
