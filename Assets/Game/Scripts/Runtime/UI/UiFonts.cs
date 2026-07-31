using UnityEngine;

namespace RealmShards.UI
{
    /// <summary>
    /// Loads UI fonts from Resources/Fonts.
    /// </summary>
    public static class UiFonts
    {
        private static Font _menuBold;
        private static Font _menuRegular;
        private static Font _fallback;

        public static Font MenuBold => _menuBold ??= Load("Fonts/Cinzel-Bold");
        public static Font MenuRegular => _menuRegular ??= Load("Fonts/Cinzel-Regular");

        private static Font Load(string path)
        {
            var font = Resources.Load<Font>(path);
            return font != null ? font : Default();
        }

        private static Font Default()
        {
            if (_fallback != null) return _fallback;
            _fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_fallback == null)
                _fallback = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return _fallback;
        }
    }
}
