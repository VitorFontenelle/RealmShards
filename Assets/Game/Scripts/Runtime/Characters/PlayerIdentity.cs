using UnityEngine;

namespace RealmShards
{
    public sealed class PlayerIdentity : MonoBehaviour
    {
        private static readonly Color[] DefaultColors =
        {
            new Color(0.72f, 0.45f, 0.95f),
            new Color(0.35f, 0.75f, 0.95f),
            new Color(0.95f, 0.55f, 0.35f),
            new Color(0.45f, 0.9f, 0.55f)
        };

        [SerializeField] private int playerIndex;
        [SerializeField] private Color playerColor = new Color(0.72f, 0.45f, 0.95f);
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer ringRenderer;
        [SerializeField] private TextMesh indicatorLabel;
        [SerializeField] private bool useMaterialPropertyBlock = true;

        private MaterialPropertyBlock _mpb;

        public int PlayerIndex => playerIndex;
        public Color PlayerColor => playerColor;

        private void Awake()
        {
            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponentInChildren<SpriteRenderer>();
            }

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
                if (useMaterialPropertyBlock)
                {
                    _mpb ??= new MaterialPropertyBlock();
                    bodyRenderer.GetPropertyBlock(_mpb);
                    _mpb.SetColor("_Color", playerColor);
                    // URP/Lit and Sprites/Default use different color props; also set tint fallback.
                    _mpb.SetColor("_BaseColor", playerColor);
                    bodyRenderer.SetPropertyBlock(_mpb);
                }

                // Always tint as reliable fallback for default sprite material.
                var c = playerColor;
                c.a = bodyRenderer.color.a;
                bodyRenderer.color = c;
            }

            if (ringRenderer != null)
            {
                var c = playerColor;
                c.a = 0.7f;
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
