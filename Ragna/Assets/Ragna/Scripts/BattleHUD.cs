using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ragna
{
    public sealed class BattleHUD : MonoBehaviour
    {
        public TMP_Text floor, title, hp, level, skill, info, potionText, guardText, banner, hint;
        public Image hpFill, xpFill, floorFill;
        public Button potion, guard, pause, growth, prepare;
    }
}
