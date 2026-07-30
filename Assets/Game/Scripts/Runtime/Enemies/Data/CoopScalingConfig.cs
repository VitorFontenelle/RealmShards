using UnityEngine;

namespace RealmShards.Enemies
{
    [CreateAssetMenu(menuName = "RealmShards/Enemies/Coop Scaling Config", fileName = "CoopScalingConfig")]
    public sealed class CoopScalingConfig : ScriptableObject
    {
        [SerializeField] private AnimationCurve healthMultiplierByPlayerCount = new AnimationCurve(
            new Keyframe(1f, 1f),
            new Keyframe(2f, 1.45f),
            new Keyframe(3f, 1.9f),
            new Keyframe(4f, 2.35f));

        [SerializeField] private AnimationCurve spawnCountMultiplierByPlayerCount = new AnimationCurve(
            new Keyframe(1f, 1f),
            new Keyframe(2f, 1.25f),
            new Keyframe(3f, 1.5f),
            new Keyframe(4f, 1.75f));

        [SerializeField] private AnimationCurve damageMultiplierByPlayerCount = new AnimationCurve(
            new Keyframe(1f, 1f),
            new Keyframe(2f, 1.1f),
            new Keyframe(3f, 1.2f),
            new Keyframe(4f, 1.3f));

        public float GetHealthMultiplier(int playerCount)
        {
            return Mathf.Max(0.1f, healthMultiplierByPlayerCount.Evaluate(Mathf.Clamp(playerCount, 1, 4)));
        }

        public float GetSpawnCountMultiplier(int playerCount)
        {
            return Mathf.Max(0.1f, spawnCountMultiplierByPlayerCount.Evaluate(Mathf.Clamp(playerCount, 1, 4)));
        }

        public float GetDamageMultiplier(int playerCount)
        {
            return Mathf.Max(0.1f, damageMultiplierByPlayerCount.Evaluate(Mathf.Clamp(playerCount, 1, 4)));
        }

        public int ScaleSpawnCount(int baseCount, int playerCount)
        {
            return Mathf.Max(1, Mathf.RoundToInt(baseCount * GetSpawnCountMultiplier(playerCount)));
        }
    }
}
