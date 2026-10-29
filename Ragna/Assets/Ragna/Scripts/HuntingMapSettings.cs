using UnityEngine;

namespace Ragna
{
    [CreateAssetMenu(menuName = "Ragna/Hunting Map Settings")]
    public sealed class HuntingMapSettings : ScriptableObject
    {
        [Header("바닥 이동 범위 · 점프 없음")]
        public Vector2 horizontalBounds = new Vector2(-4.8f, 4.8f);
        public Vector2 depthBounds = new Vector2(-3.6f, -1.8f);
        [Header("자동 추적 이동 속도")]
        [Min(.1f)] public float heroSpeed = 2.8f;
        [Min(.1f)] public float enemySpeed = 1.1f;
        [Header("검사 공격 범위 · 좌우 거리 / 위아래 폭")]
        [Min(.1f)] public float swordReach = 1.5f;
        [Min(.05f)] public float depthTolerance = .28f;
        [Header("적 공격 주기 · 서로 독립적으로 공격")]
        [Min(.2f)] public float enemyAttackInterval = 1.6f;
        [Range(0, .5f), Tooltip("보스 공격에 더해지는 최대 HP 비율 피해")]
        public float bossHpDamage = .24f;
        [Min(.1f), Tooltip("새 몬스터의 첫 공격까지 대기 시간")]
        public float initialAttackDelay = .6f;
        [Min(1), Tooltip("자동사냥에서 보스가 공격 예고를 보여줄 수 있도록 체력을 조정")]
        public float bossHealthMultiplier = 4;
        public Vector2 Clamp(Vector2 value) => new Vector2(Mathf.Clamp(value.x, horizontalBounds.x, horizontalBounds.y), Mathf.Clamp(value.y, depthBounds.x, depthBounds.y));
        static HuntingMapSettings current;
        public static HuntingMapSettings Current => current != null ? current : current = Resources.Load<HuntingMapSettings>("HuntingMap") ?? CreateInstance<HuntingMapSettings>();
    }
}
