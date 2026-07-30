namespace RealmShards
{
    /// <summary>
    /// Shared damage contract for players, enemies, and props.
    /// </summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        FactionId Faction { get; }
        int TeamId { get; }
        bool TryApplyDamage(in DamageInfo damage);
    }
}
