using System.Collections.Generic;
using UnityEngine;

namespace RealmShards.Enemies
{
    /// <summary>
    /// Loads sliced sprites from a spritesheet path (Editor AssetDatabase, or Resources fallback).
    /// </summary>
    public static class EnemySpriteLoader
    {
        private static readonly Dictionary<string, Sprite[]> Cache = new Dictionary<string, Sprite[]>();

        public static Sprite[] LoadAll(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return System.Array.Empty<Sprite>();

            if (Cache.TryGetValue(assetPath, out var cached))
                return cached;

            Sprite[] sprites = null;

#if UNITY_EDITOR
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
            var list = new List<Sprite>();
            if (assets != null)
            {
                for (int i = 0; i < assets.Length; i++)
                {
                    if (assets[i] is Sprite s)
                        list.Add(s);
                }
            }

            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            sprites = list.ToArray();
#else
            // Runtime builds: expect a Resources copy named without extension under Resources/Enemies/
            string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            sprites = Resources.LoadAll<Sprite>("Enemies/" + file);
#endif

            if (sprites == null)
                sprites = System.Array.Empty<Sprite>();

            Cache[assetPath] = sprites;
            return sprites;
        }

        public static Sprite[] Slice(Sprite[] source, int start, int count)
        {
            if (source == null || source.Length == 0 || count <= 0)
                return System.Array.Empty<Sprite>();

            start = Mathf.Clamp(start, 0, source.Length - 1);
            int end = Mathf.Min(source.Length, start + count);
            int n = end - start;
            if (n <= 0)
                return System.Array.Empty<Sprite>();

            var result = new Sprite[n];
            for (int i = 0; i < n; i++)
                result[i] = source[start + i];
            return result;
        }

        public static Sprite CreatePlaceholder(Color color, int size = 32)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool border = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                pixels[y * size + x] = border ? Color.black : color;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        }
    }
}
