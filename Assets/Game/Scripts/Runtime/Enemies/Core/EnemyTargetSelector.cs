using System.Collections.Generic;
using UnityEngine;

namespace RealmShards.Enemies
{
    /// <summary>
    /// Picks a living player target and keeps it for a while (no per-frame retarget spam).
    /// </summary>
    public sealed class EnemyTargetSelector
    {
        private readonly float _retargetInterval;
        private readonly float _aggroRange;
        private Transform _self;
        private IPlayerMarker _current;
        private float _nextRetargetTime;

        public IPlayerMarker Current => _current;
        public Transform CurrentTransform => _current != null && _current.IsAlive ? _current.Transform : null;

        public EnemyTargetSelector(Transform self, float retargetInterval, float aggroRange)
        {
            _self = self;
            _retargetInterval = Mathf.Max(0.2f, retargetInterval);
            _aggroRange = aggroRange;
        }

        public void Tick(float time, IReadOnlyList<IPlayerMarker> players)
        {
            bool lost = _current == null || !_current.IsAlive || _current.Transform == null;
            bool due = time >= _nextRetargetTime;
            bool outOfRange = false;

            if (!lost && _current.Transform != null)
            {
                outOfRange = Vector2.Distance(_self.position, _current.Transform.position) > _aggroRange * 1.35f;
            }

            if (lost || due || outOfRange)
            {
                _current = PickBest(players);
                _nextRetargetTime = time + _retargetInterval;
            }
        }

        public void ForceClear()
        {
            _current = null;
            _nextRetargetTime = 0f;
        }

        private IPlayerMarker PickBest(IReadOnlyList<IPlayerMarker> players)
        {
            if (players == null || players.Count == 0)
                return null;

            IPlayerMarker best = null;
            float bestDist = float.MaxValue;
            Vector2 origin = _self.position;

            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p == null || !p.IsAlive || p.Transform == null)
                    continue;

                float d = Vector2.Distance(origin, p.Transform.position);
                if (d > _aggroRange)
                    continue;

                if (d < bestDist)
                {
                    bestDist = d;
                    best = p;
                }
            }

            return best;
        }
    }
}
