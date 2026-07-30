#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RealmShards.EditorTools
{
    /// <summary>
    /// Processes a COPY of sample-tile into Generated/ with padded edges for cleaner seams.
    /// Never overwrites Assets/Tiles/** sources.
    /// </summary>
    public static class FloorSeamProcessor
    {
        private const string SourcePath = "Assets/Tiles/sample-tile.png";
        private const string OutputDir = "Assets/Game/Art/Tiles/Generated";
        private const string OutputPath = OutputDir + "/sample-tile_seamed.png";

        [MenuItem("RealmShards/Art/Process Floor Seam Tile")]
        public static void Process()
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(SourcePath);
            if (tex == null)
            {
                Debug.LogWarning($"[FloorSeam] Source missing: {SourcePath}");
                return;
            }

            EnsureFolder(OutputDir);

            // Read via file bytes so we don't require Read/Write on importer for the source permanently.
            string abs = Path.GetFullPath(SourcePath);
            if (!File.Exists(abs))
                abs = Path.Combine(Application.dataPath, "../" + SourcePath).Replace('\\', '/');
            // Prefer AssetDatabase path resolution
            abs = Path.GetFullPath(Path.Combine(Application.dataPath, "..", SourcePath));

            byte[] bytes = File.Exists(abs) ? File.ReadAllBytes(abs) : null;
            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (bytes == null || !src.LoadImage(bytes))
            {
                // Fallback: duplicate via RenderTexture blit if readable fails
                src = DuplicateReadable(tex);
                if (src == null)
                {
                    Debug.LogError("[FloorSeam] Could not read source tile.");
                    return;
                }
            }

            int pad = Mathf.Max(2, src.width / 16);
            var dst = new Texture2D(src.width + pad * 2, src.height + pad * 2, TextureFormat.RGBA32, false);
            dst.filterMode = FilterMode.Point;
            dst.wrapMode = TextureWrapMode.Repeat;

            // Fill with edge-extended samples (simple seam pad).
            for (int y = 0; y < dst.height; y++)
            {
                for (int x = 0; x < dst.width; x++)
                {
                    int sx = Mathf.Clamp(x - pad, 0, src.width - 1);
                    int sy = Mathf.Clamp(y - pad, 0, src.height - 1);
                    // Mirror edge for softer seam
                    if (x < pad) sx = pad - x;
                    if (x >= pad + src.width) sx = src.width - 1 - (x - (pad + src.width));
                    if (y < pad) sy = pad - y;
                    if (y >= pad + src.height) sy = src.height - 1 - (y - (pad + src.height));
                    sx = Mathf.Clamp(sx, 0, src.width - 1);
                    sy = Mathf.Clamp(sy, 0, src.height - 1);
                    dst.SetPixel(x, y, src.GetPixel(sx, sy));
                }
            }

            // Average center crossfade on padded border toward tile interior color.
            Color avg = AverageColor(src);
            for (int y = 0; y < dst.height; y++)
            {
                for (int x = 0; x < dst.width; x++)
                {
                    bool border = x < pad || y < pad || x >= dst.width - pad || y >= dst.height - pad;
                    if (!border) continue;
                    Color c = dst.GetPixel(x, y);
                    dst.SetPixel(x, y, Color.Lerp(c, avg, 0.25f));
                }
            }

            dst.Apply();
            byte[] png = dst.EncodeToPNG();
            string outAbs = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputPath));
            Directory.CreateDirectory(Path.GetDirectoryName(outAbs));
            File.WriteAllBytes(outAbs, png);
            Object.DestroyImmediate(src);
            Object.DestroyImmediate(dst);
            AssetDatabase.Refresh();

            ApplyImportSettings(OutputPath);
            Debug.Log($"[FloorSeam] Wrote {OutputPath} (source untouched). Use this under ArenaBuilder Generated override when ready.");
        }

        private static Texture2D DuplicateReadable(Texture2D source)
        {
            var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return tex;
        }

        private static Color AverageColor(Texture2D tex)
        {
            var px = tex.GetPixels32();
            float r = 0, g = 0, b = 0;
            for (int i = 0; i < px.Length; i++)
            {
                r += px[i].r; g += px[i].g; b += px[i].b;
            }
            float n = Mathf.Max(1, px.Length);
            return new Color(r / (255f * n), g / (255f * n), b / (255f * n), 1f);
        }

        private static void ApplyImportSettings(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string[] parts = path.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
#endif
