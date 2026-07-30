using UnityEngine;

namespace RealmShards.Core
{
    /// <summary>
    /// Build-safe content references loaded via Resources (AssetDatabase is Editor-only).
    /// Populated by RealmShards setup menus into Resources/GameContent.asset.
    /// </summary>
    [CreateAssetMenu(menuName = "RealmShards/Runtime Content Catalog", fileName = "GameContent")]
    public sealed class RuntimeContentCatalog : ScriptableObject
    {
        public const string ResourcesName = "GameContent";

        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Sprite floorTile;
        [SerializeField] private Sprite arrowSprite;
        [SerializeField] private Sprite[] knightSprites;
        [SerializeField] private Sprite[] archerSprites;
        [SerializeField] private Sprite[] mageRunSprites;
        [SerializeField] private Sprite[] mageCastSprites;
        [SerializeField] private Sprite mageIdleSprite;

        public GameObject PlayerPrefab => playerPrefab;
        public Sprite FloorTile => floorTile;
        public Sprite ArrowSprite => arrowSprite;
        public Sprite[] KnightSprites => knightSprites;
        public Sprite[] ArcherSprites => archerSprites;
        public Sprite[] MageRunSprites => mageRunSprites;
        public Sprite[] MageCastSprites => mageCastSprites;
        public Sprite MageIdleSprite => mageIdleSprite;

        private static RuntimeContentCatalog _cached;

        public static RuntimeContentCatalog Get()
        {
            if (_cached != null)
                return _cached;
            _cached = Resources.Load<RuntimeContentCatalog>(ResourcesName);
            return _cached;
        }

        public Sprite[] GetSheetSprites(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;

            string name = System.IO.Path.GetFileName(assetPath).ToLowerInvariant();
            if (name.Contains("knight"))
                return knightSprites;
            if (name.Contains("archer"))
                return archerSprites;
            if (name.Contains("running"))
                return mageRunSprites;
            if (name.Contains("attacking"))
                return mageCastSprites;
            if (name.Contains("standing"))
                return mageIdleSprite != null ? new[] { mageIdleSprite } : null;
            if (name.Contains("sample-tile") || name.Contains("floor"))
                return floorTile != null ? new[] { floorTile } : null;
            return null;
        }

#if UNITY_EDITOR
        public void EditorAssign(
            GameObject player,
            Sprite floor,
            Sprite arrow,
            Sprite[] knight,
            Sprite[] archer,
            Sprite[] mageRun,
            Sprite[] mageCast,
            Sprite mageIdle)
        {
            playerPrefab = player;
            floorTile = floor;
            arrowSprite = arrow;
            knightSprites = knight ?? System.Array.Empty<Sprite>();
            archerSprites = archer ?? System.Array.Empty<Sprite>();
            mageRunSprites = mageRun ?? System.Array.Empty<Sprite>();
            mageCastSprites = mageCast ?? System.Array.Empty<Sprite>();
            mageIdleSprite = mageIdle;
        }
#endif
    }
}
