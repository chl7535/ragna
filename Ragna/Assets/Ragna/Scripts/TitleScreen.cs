using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ragna
{
    public sealed class TitleScreen : MonoBehaviour
    {
        public Button gameStart, settings, credits, quit;
        public RectTransform safeArea;
        Rect previousSafeArea;
        void LateUpdate()
        {
            if (safeArea == null || Screen.safeArea == previousSafeArea) return;
            previousSafeArea = Screen.safeArea;
            safeArea.anchorMin = new Vector2(previousSafeArea.xMin / Screen.width, previousSafeArea.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(previousSafeArea.xMax / Screen.width, previousSafeArea.yMax / Screen.height);
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        }
        public void Bind(Action start)
        {
            var scaler = GetComponent<CanvasScaler>();
            scaler.referenceResolution = LandscapeDisplaySettings.Current.referenceResolution;
            scaler.matchWidthOrHeight = 1;
            gameStart.onClick.AddListener(() => start());
            // The other buttons are intentionally visual placeholders with no actions.
        }
    }
}
