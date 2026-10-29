using UnityEngine;

namespace Ragna
{
    [CreateAssetMenu(menuName = "Ragna/Landscape Display Settings")]
    public sealed class LandscapeDisplaySettings : ScriptableObject
    {
        [Header("가로 화면 기준 · 세로 지원 없음")]
        public Vector2 referenceResolution = new Vector2(1920,1080);
        [Range(0,1)] public float battleBottom = .17f;
        [Range(0,1)] public float battleTop = .90f;
        [Min(.1f)] public float cameraHalfHeight = 5.625f;
        static LandscapeDisplaySettings current;
        public static LandscapeDisplaySettings Current => current != null ? current : current = Resources.Load<LandscapeDisplaySettings>("UI/LandscapeDisplay") ?? CreateInstance<LandscapeDisplaySettings>();
    }
}
