using UnityEngine;

namespace RealmShards
{
    public sealed class PlayerIdentity : MonoBehaviour
    {
        /// <summary>P1 purple, P2 green, P3 yellow, P4 red — robe recolor targets.</summary>
        private static readonly Color[] DefaultColors =
        {
            new Color(0.72f, 0.45f, 0.95f), // purple
            new Color(0.35f, 0.82f, 0.42f), // green
            new Color(0.95f, 0.82f, 0.28f), // yellow
            new Color(0.92f, 0.28f, 0.28f)  // red
        };

        [SerializeField] private int playerIndex;
        [SerializeField] private Color playerColor = new Color(0.72f, 0.45f, 0.95f);
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer ringRenderer;
        [SerializeField] private TextMesh indicatorLabel;
        [SerializeField] private bool useMaterialPropertyBlock = true;
        [SerializeField] private float recolorStrength = 0.85f;
        [SerializeField] private float purpleTolerance = 0.42f;

        private MaterialPropertyBlock _mpb;

        public int PlayerIndex => playerIndex;
        public Color PlayerColor => playerColor;

        private void Awake()
        {
            if (bodyRenderer == null)
                bodyRenderer = GetComponentInChildren<SpriteRenderer>();
            ApplyVisuals();
        }

        public void Setup(int index, Color? overrideColor = null)
        {
            playerIndex = index;
            playerColor = overrideColor ?? DefaultColors[Mathf.Abs(index) % DefaultColors.Length];
            ApplyVisuals();
        }

        public void ApplyVisuals()
        {
            if (bodyRenderer != null)
            {
                bodyRenderer.sortingLayerName = Core.SortingLayers.Characters;
                if (bodyRenderer.sortingOrder < 8)
                    bodyRenderer.sortingOrder = 10;

                if (useMaterialPropertyBlock)
                {
                    _mpb ??= new MaterialPropertyBlock();
                    bodyRenderer.GetPropertyBlock(_mpb);
                    // SpriteTintRecolor: only purple-ish robe pixels remap; gold/dark stay.
                    _mpb.SetColor("_Color", playerColor);
                    _mpb.SetColor("_BaseColor", Color.white);
                    _mpb.SetFloat("_RecolorStrength", recolorStrength);
                    _mpb.SetFloat("_PurpleTolerance", purpleTolerance);
                    _mpb.SetColor("_PurpleCenter", new Color(0.45f, 0.25f, 0.7f, 1f));
                    bodyRenderer.SetPropertyBlock(_mpb);
                }

                // Keep vertex color near-white so shader luminance * tint owns robe hue.
                var c = bodyRenderer.color;
                bodyRenderer.color = new Color(1f, 1f, 1f, c.a);
            }

            if (ringRenderer != null)
            {
                ringRenderer.sortingLayerName = Core.SortingLayers.Characters;
                var c = playerColor;
                c.a = 0.75f;
                ringRenderer.color = c;
                ringRenderer.enabled = true;
            }

            if (indicatorLabel != null)
            {
                indicatorLabel.text = (playerIndex + 1).ToString();
                indicatorLabel.color = playerColor;
            }
        }
    }
}
