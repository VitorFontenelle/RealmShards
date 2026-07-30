using UnityEngine;

namespace RealmShards
{
    public sealed class AbilityContext
    {
        public Transform CasterTransform;
        public Rigidbody2D CasterBody;
        public FactionMember Faction;
        public Health Health;
        public Vector2 Origin;
        public Vector2 AimDirection;
        public AbilityCaster Caster;
        public PlayerMotor Motor;
    }
}
