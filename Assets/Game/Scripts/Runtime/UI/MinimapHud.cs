using RealmShards.World;
using UnityEngine;
using UnityEngine.UI;

namespace RealmShards.UI
{
    /// <summary>
    /// Semi-transparent top-right minimap with fog-of-war (only explored cells).
    /// </summary>
    public sealed class MinimapHud : MonoBehaviour
    {
        private const int TexSize = 128;

        private RawImage _image;
        private Texture2D _tex;
        private Color32[] _pixels;
        private ExplorationFog _fog;
        private float _refresh;

        public static void EnsurePresent()
        {
            if (FindFirstObjectByType<MinimapHud>() != null)
                return;
            var go = new GameObject(nameof(MinimapHud));
            go.AddComponent<MinimapHud>();
        }

        private void Start()
        {
            BuildUi();
            _fog = FindFirstObjectByType<ExplorationFog>();
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("MinimapCanvas");
            canvasGo.transform.SetParent(transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280, 800);
            canvasGo.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("MinimapPanel");
            panel.transform.SetParent(canvasGo.transform, false);
            var panelRt = panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(1f, 1f);
            panelRt.anchorMax = new Vector2(1f, 1f);
            panelRt.pivot = new Vector2(1f, 1f);
            panelRt.anchoredPosition = new Vector2(-16f, -16f);
            panelRt.sizeDelta = new Vector2(170f, 170f);
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.45f);

            var mapGo = new GameObject("Map");
            mapGo.transform.SetParent(panel.transform, false);
            var mapRt = mapGo.AddComponent<RectTransform>();
            mapRt.anchorMin = Vector2.zero;
            mapRt.anchorMax = Vector2.one;
            mapRt.offsetMin = new Vector2(8f, 8f);
            mapRt.offsetMax = new Vector2(-8f, -8f);
            _image = mapGo.AddComponent<RawImage>();
            _image.color = new Color(1f, 1f, 1f, 0.82f);

            _tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _pixels = new Color32[TexSize * TexSize];
            _image.texture = _tex;
        }

        private void Update()
        {
            if (_fog == null)
                _fog = FindFirstObjectByType<ExplorationFog>();

            _refresh -= Time.unscaledDeltaTime;
            if (_refresh > 0f || _fog == null || _fog.Map == null || _tex == null)
                return;
            _refresh = 0.12f;
            Redraw();
        }

        private void Redraw()
        {
            var map = _fog.Map;
            for (int i = 0; i < _pixels.Length; i++)
                _pixels[i] = new Color32(0, 0, 0, 90);

            for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                int px = Mathf.FloorToInt((x / (float)map.Width) * TexSize);
                int py = Mathf.FloorToInt((y / (float)map.Height) * TexSize);
                if (px < 0 || py < 0 || px >= TexSize || py >= TexSize)
                    continue;

                if (!_fog.IsExplored(x, y))
                {
                    _pixels[py * TexSize + px] = new Color32(0, 0, 0, 140);
                    continue;
                }

                if (!map.IsWalkable(x, y))
                {
                    _pixels[py * TexSize + px] = new Color32(8, 8, 10, 160);
                    continue;
                }

                bool corridor = map.Get(x, y) == DungeonCell.Corridor;
                _pixels[py * TexSize + px] = corridor
                    ? new Color32(120, 110, 95, 200)
                    : new Color32(170, 150, 110, 210);
            }

            // Player markers.
            var players = Enemies.PlayerTargetRegistry.Collect();
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] == null || !players[i].IsAlive || players[i].Transform == null)
                    continue;
                var cell = map.WorldToCell(players[i].Transform.position);
                int px = Mathf.FloorToInt((cell.x / (float)map.Width) * TexSize);
                int py = Mathf.FloorToInt((cell.y / (float)map.Height) * TexSize);
                PaintDot(px, py, new Color32(220, 90, 255, 255), 2);
            }

            _tex.SetPixels32(_pixels);
            _tex.Apply(false);
        }

        private void PaintDot(int cx, int cy, Color32 color, int radius)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                if (x < 0 || y < 0 || x >= TexSize || y >= TexSize)
                    continue;
                _pixels[y * TexSize + x] = color;
            }
        }

        private void OnDestroy()
        {
            if (_tex != null)
                Destroy(_tex);
        }
    }
}
