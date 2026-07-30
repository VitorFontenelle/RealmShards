#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RealmShards.EditorTools
{
    /// <summary>
    /// MVP sprite-sheet processor: never overwrites sources; outputs under Art/**/Processed|Generated.
    /// Limitations: threshold matte only (near-black → alpha); grid re-slice with padding; no true content-aware fill.
    /// </summary>
    public sealed class SpriteSheetProcessorWindow : EditorWindow
    {
        private DefaultAsset _sourceFolderOrFile;
        private string _sourcePath = "Assets/Characters";
        private string _outputRoot = "Assets/Game/Art/Characters/Processed";
        private int _columns = 8;
        private int _rows = 6;
        private int _padPixels = 2;
        private float _blackThreshold = 0.04f;
        private bool _makeTransparentNearBlack = true;

        [MenuItem("RealmShards/Art/Sprite Sheet Processor")]
        public static void Open() => GetWindow<SpriteSheetProcessorWindow>("Sprite Sheet Processor");

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "MVP pipeline\n" +
                "• Never overwrites Assets/Characters/** or Assets/Tiles/**\n" +
                "• Writes PNG + applies Point / no mips / no compression\n" +
                "• Near-black → transparent (threshold)\n" +
                "• Optional padded grid re-slice import (mage 8×6 etc.)\n" +
                "Limitations: no ML content-aware fill; padding is edge-clone only; complex sheets may need hand slice.",
                MessageType.Info);

            _sourcePath = EditorGUILayout.TextField("Source asset path", _sourcePath);
            _outputRoot = EditorGUILayout.TextField("Output root", _outputRoot);
            _columns = EditorGUILayout.IntField("Grid columns", _columns);
            _rows = EditorGUILayout.IntField("Grid rows", _rows);
            _padPixels = EditorGUILayout.IntField("Cell pad px", _padPixels);
            _makeTransparentNearBlack = EditorGUILayout.Toggle("Near-black → alpha", _makeTransparentNearBlack);
            _blackThreshold = EditorGUILayout.Slider("Black threshold", _blackThreshold, 0.01f, 0.2f);

            if (GUILayout.Button("Process selected Texture2D / path"))
            {
                ProcessPath(_sourcePath);
            }

            if (GUILayout.Button("Process mage 8×6 defaults"))
            {
                // Best-effort common paths — skip silently if missing.
                TryProcess("Assets/Characters/Mage/mage-spritesheet.png", 8, 6);
                TryProcess("Assets/Characters/mage-spritesheet.png", 8, 6);
            }

            if (GUILayout.Button("Process enemy sheets (auto grid guess)"))
            {
                TryProcess("Assets/Characters/Enemies/knight-spritesheet.png", 6, 4);
                TryProcess("Assets/Characters/Enemies/archer-spritesheet.png", 6, 4);
            }
        }

        private void TryProcess(string path, int cols, int rows)
        {
            _columns = cols;
            _rows = rows;
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null)
                ProcessPath(path);
            else
                Debug.Log($"[SpriteSheetProcessor] Skip missing {path}");
        }

        private void ProcessPath(string assetPath)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null)
            {
                Debug.LogWarning($"[SpriteSheetProcessor] Missing texture: {assetPath}");
                return;
            }

            string abs = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            if (!File.Exists(abs))
            {
                Debug.LogWarning($"[SpriteSheetProcessor] File not found on disk: {abs}");
                return;
            }

            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!src.LoadImage(File.ReadAllBytes(abs)))
            {
                Object.DestroyImmediate(src);
                Debug.LogError("[SpriteSheetProcessor] LoadImage failed.");
                return;
            }

            if (_makeTransparentNearBlack)
                ApplyBlackThreshold(src, _blackThreshold);

            Texture2D padded = ApplyCellPadding(src, _columns, _rows, _padPixels);
            Object.DestroyImmediate(src);

            EnsureFolder(_outputRoot);
            string file = Path.GetFileNameWithoutExtension(assetPath) + "_processed.png";
            string outRel = _outputRoot.TrimEnd('/') + "/" + file;
            string outAbs = Path.GetFullPath(Path.Combine(Application.dataPath, "..", outRel));
            Directory.CreateDirectory(Path.GetDirectoryName(outAbs));
            File.WriteAllBytes(outAbs, padded.EncodeToPNG());
            Object.DestroyImmediate(padded);
            AssetDatabase.Refresh();
            ApplyImport(outRel, _columns, _rows);
            Debug.Log($"[SpriteSheetProcessor] Wrote {outRel}");
        }

        private static void ApplyBlackThreshold(Texture2D tex, float threshold)
        {
            var px = tex.GetPixels();
            for (int i = 0; i < px.Length; i++)
            {
                float lum = px[i].r * 0.3f + px[i].g * 0.59f + px[i].b * 0.11f;
                if (lum <= threshold && px[i].a > 0.01f)
                    px[i].a = 0f;
            }
            tex.SetPixels(px);
            tex.Apply();
        }

        private static Texture2D ApplyCellPadding(Texture2D src, int cols, int rows, int pad)
        {
            cols = Mathf.Max(1, cols);
            rows = Mathf.Max(1, rows);
            pad = Mathf.Max(0, pad);
            int cellW = src.width / cols;
            int cellH = src.height / rows;
            if (cellW <= 0 || cellH <= 0)
                return src;

            int outW = cols * (cellW + pad * 2);
            int outH = rows * (cellH + pad * 2);
            var dst = new Texture2D(outW, outH, TextureFormat.RGBA32, false);
            dst.filterMode = FilterMode.Point;
            var clear = new Color(0, 0, 0, 0);
            var fill = new Color[outW * outH];
            for (int i = 0; i < fill.Length; i++) fill[i] = clear;
            dst.SetPixels(fill);

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int sx = col * cellW;
                    int sy = (rows - 1 - row) * cellH; // unity tex origin bottom-left; keep row order top-down visually
                    // Prefer top-down sheet convention: row 0 at top of image
                    sy = src.height - (row + 1) * cellH;
                    int dx = col * (cellW + pad * 2) + pad;
                    int dy = outH - (row + 1) * (cellH + pad * 2) + pad;

                    for (int y = 0; y < cellH; y++)
                    {
                        for (int x = 0; x < cellW; x++)
                        {
                            Color c = src.GetPixel(sx + x, sy + y);
                            dst.SetPixel(dx + x, dy + y, c);
                        }
                    }

                    // Edge clone into pad
                    for (int p = 0; p < pad; p++)
                    {
                        for (int y = 0; y < cellH; y++)
                        {
                            dst.SetPixel(dx - 1 - p, dy + y, src.GetPixel(sx, sy + y));
                            dst.SetPixel(dx + cellW + p, dy + y, src.GetPixel(sx + cellW - 1, sy + y));
                        }
                        for (int x = 0; x < cellW; x++)
                        {
                            dst.SetPixel(dx + x, dy - 1 - p, src.GetPixel(sx + x, sy));
                            dst.SetPixel(dx + x, dy + cellH + p, src.GetPixel(sx + x, sy + cellH - 1));
                        }
                    }
                }
            }

            dst.Apply();
            return dst;
        }

        private static void ApplyImport(string assetPath, int cols, int rows)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex != null)
            {
                int cellW = tex.width / Mathf.Max(1, cols);
                int cellH = tex.height / Mathf.Max(1, rows);
                var metas = new SpriteMetaData[cols * rows];
                int i = 0;
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        metas[i] = new SpriteMetaData
                        {
                            name = $"{Path.GetFileNameWithoutExtension(assetPath)}_{i}",
                            rect = new Rect(c * cellW, (rows - 1 - r) * cellH, cellW, cellH),
                            alignment = (int)SpriteAlignment.Center,
                            pivot = new Vector2(0.5f, 0.5f)
                        };
                        i++;
                    }
                }

                importer.spritesheet = metas;
            }

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
