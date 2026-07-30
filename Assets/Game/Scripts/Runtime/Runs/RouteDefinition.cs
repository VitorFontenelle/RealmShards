using UnityEngine;

namespace RealmShards.Runs
{
    /// <summary>
    /// Stub route definition for decade / city routing.
    /// TODO (world agent): room sequence, encounter tables, decade modifiers.
    /// </summary>
    [CreateAssetMenu(fileName = "RouteDefinition", menuName = "RealmShards/Runs/Route Definition")]
    public sealed class RouteDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string cityId;
        [SerializeField] private int recommendedDecade;

        public string Id => id;
        public string DisplayName => displayName;
        public string CityId => cityId;
        public int RecommendedDecade => recommendedDecade;
    }
}
