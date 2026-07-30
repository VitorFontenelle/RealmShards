using UnityEngine;

namespace RealmShards
{
    /// <summary>
    /// Rich damage payload used by player hitboxes/projectiles.
    /// </summary>
    public struct DamageInfo
    {
        public float Amount;
        public Vector2 Knockback;
        public Vector2 HitPoint;
        public FactionId SourceFaction;
        public int SourceTeamId;
        public GameObject Source;
        public bool IgnoreIFrames;

        public static DamageInfo Create(
            float amount,
            Vector2 knockback,
            Vector2 hitPoint,
            FactionMember sourceFaction,
            GameObject source,
            bool ignoreIFrames = false)
        {
            return new DamageInfo
            {
                Amount = amount,
                Knockback = knockback,
                HitPoint = hitPoint,
                SourceFaction = sourceFaction != null ? sourceFaction.Faction : FactionId.Neutral,
                SourceTeamId = sourceFaction != null ? sourceFaction.TeamId : 0,
                Source = source,
                IgnoreIFrames = ignoreIFrames
            };
        }
    }
}
