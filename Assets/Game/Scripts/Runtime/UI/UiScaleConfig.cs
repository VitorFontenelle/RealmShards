using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RealmShards.UI
{
    /// <summary>
    /// Deck-friendly canvas defaults: reference 1280x800 with match bias for handheld.
    /// </summary>
    public static class UiScaleConfig
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(1280f, 800f);
        public const float MatchWidthOrHeight = 0.5f;
        public const float SafeMarginNormalized = 0.02f;

        public static void Apply(CanvasScaler scaler)
        {
            if (scaler == null) return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = MatchWidthOrHeight;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        }

        public static void ApplySafeArea(RectTransform rt)
        {
            if (rt == null) return;
            Rect safe = Screen.safeArea;
            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
            min.x = Mathf.Max(min.x, SafeMarginNormalized);
            min.y = Mathf.Max(min.y, SafeMarginNormalized);
            max.x = Mathf.Min(max.x, 1f - SafeMarginNormalized);
            max.y = Mathf.Min(max.y, 1f - SafeMarginNormalized);
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
