namespace RealmShards.Core
{
    /// <summary>
    /// Physics layer indices (TagManager user layers start at 6).
    /// </summary>
    public static class GameLayers
    {
        public const int Player = 6;
        public const int Enemy = 7;
        public const int PlayerProjectile = 8;
        public const int EnemyProjectile = 9;
        public const int PlayerHitbox = 10;
        public const int EnemyHitbox = 11;
        public const int Pickup = 12;
        public const int Environment = 13;
        public const int Trigger = 14;
        public const int RoomBoundary = 15;
        /// <summary>Compat alias used by combat agent (<c>CombatLayers.Projectile</c>).</summary>
        public const int Projectile = 16;

        public const string PlayerName = "Player";
        public const string EnemyName = "Enemy";
        public const string PlayerProjectileName = "PlayerProjectile";
        public const string EnemyProjectileName = "EnemyProjectile";
        public const string PlayerHitboxName = "PlayerHitbox";
        public const string EnemyHitboxName = "EnemyHitbox";
        public const string PickupName = "Pickup";
        public const string EnvironmentName = "Environment";
        public const string TriggerName = "Trigger";
        public const string RoomBoundaryName = "RoomBoundary";
        public const string ProjectileName = "Projectile";
    }
}
