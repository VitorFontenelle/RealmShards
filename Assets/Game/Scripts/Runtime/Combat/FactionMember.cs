using UnityEngine;

namespace RealmShards
{
    public sealed class FactionMember : MonoBehaviour
    {
        [SerializeField] private FactionId faction = FactionId.Player;
        [SerializeField] private int teamId;
        [SerializeField] private bool friendlyFire;

        public FactionId Faction => faction;
        public int TeamId => teamId;
        public bool FriendlyFire => friendlyFire;

        public void Configure(FactionId newFaction, int newTeamId, bool allowFriendlyFire = false)
        {
            faction = newFaction;
            teamId = newTeamId;
            friendlyFire = allowFriendlyFire;
        }

        public bool CanHarm(FactionMember other)
        {
            if (other == null)
            {
                return true;
            }

            if (friendlyFire || other.friendlyFire)
            {
                return true;
            }

            if (faction == FactionId.Neutral || other.faction == FactionId.Neutral)
            {
                return true;
            }

            if (faction == other.faction && teamId == other.teamId)
            {
                return false;
            }

            return faction != other.faction || teamId != other.teamId;
        }
    }
}
