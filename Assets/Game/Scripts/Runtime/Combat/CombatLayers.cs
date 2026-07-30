using UnityEngine;
using RealmShards.Core;

namespace RealmShards
{
    public static class CombatLayers
    {
        public const string Player = GameLayers.PlayerName;
        public const string PlayerHitbox = GameLayers.PlayerHitboxName;
        public const string Enemy = GameLayers.EnemyName;
        public const string EnemyHitbox = GameLayers.EnemyHitboxName;
        public const string Projectile = GameLayers.PlayerProjectileName;
        public const string ProjectileCompat = GameLayers.ProjectileName;
        public const string Pickup = GameLayers.PickupName;

        public static int Resolve(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            return layer >= 0 ? layer : 0;
        }

        public static void TrySetLayer(GameObject go, string layerName)
        {
            if (go == null)
            {
                return;
            }

            int layer = Resolve(layerName);
            if (layer == 0 && layerName != "Default")
            {
                // Fallback aliases used across agents.
                if (layerName == GameLayers.PlayerProjectileName)
                {
                    layer = Resolve(GameLayers.ProjectileName);
                }
            }

            go.layer = layer >= 0 ? layer : 0;
        }
    }
}
