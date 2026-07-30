using System.Collections.Generic;
using RealmShards.Enemies;
using RealmShards.Rooms;
using UnityEngine;

namespace RealmShards.CameraSystem
{
    /// <summary>
    /// Orthographic camera that tracks all living players (or spawn center). No Cinemachine.
    /// Soft catch-up when a player is extremely separated; locate pulse support.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class SharedOrthoCamera : MonoBehaviour
    {
        [SerializeField] private float smoothTime = 0.18f;
        [SerializeField] private float minOrthoSize = 5f;
        [SerializeField] private float maxOrthoSize = 12f;
        [SerializeField] private float padding = 2.5f;
        [SerializeField] private float fixedZ = -10f;
        [SerializeField] private RoomBounds clampBounds;
        [SerializeField] private Transform fallbackTarget;
        [SerializeField] private bool autoFindPlayers = true;
        [Header("Catch-up")]
        [SerializeField] private bool enableCatchUp = true;
        [SerializeField] private float catchUpSeparation = 14f;
        [SerializeField] private float catchUpSoftTeleportDistance = 22f;
        [SerializeField] private float catchUpPullSpeed = 18f;
        [Header("Locate")]
        [SerializeField] private float locateFocusBlend = 0.65f;

        private Camera _cam;
        private Vector3 _velocity;
        private float _zoomVelocity;
        private readonly List<Transform> _targets = new List<Transform>(8);
        private Transform _locateFocus;
        private float _locateUntil;
        private float _locateOrthoBoost = 1f;

        public void Configure(RoomBounds bounds, Transform fallback, float minZoom, float maxZoom)
        {
            clampBounds = bounds;
            fallbackTarget = fallback;
            minOrthoSize = minZoom;
            maxOrthoSize = maxZoom;
        }

        public void ConfigureCatchUp(bool enabled, float separation, float softTeleport, float pullSpeed)
        {
            enableCatchUp = enabled;
            catchUpSeparation = separation;
            catchUpSoftTeleportDistance = softTeleport;
            catchUpPullSpeed = pullSpeed;
        }

        public void PulseLocate(Transform focus, float duration, float orthoBoost)
        {
            _locateFocus = focus;
            _locateUntil = Time.unscaledTime + duration;
            _locateOrthoBoost = Mathf.Max(1f, orthoBoost);
        }

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _cam.orthographic = true;
            if (_cam.orthographicSize < minOrthoSize)
                _cam.orthographicSize = minOrthoSize;
        }

        private void LateUpdate()
        {
            RefreshTargets();
            ApplyCatchUp();

            Vector3 focus;
            float desiredSize = minOrthoSize;

            if (_targets.Count == 0)
            {
                focus = fallbackTarget != null ? fallbackTarget.position
                    : (clampBounds != null ? clampBounds.Center : Vector3.zero);
            }
            else if (_targets.Count == 1)
            {
                focus = _targets[0].position;
                desiredSize = minOrthoSize;
            }
            else
            {
                Bounds b = new Bounds(_targets[0].position, Vector3.zero);
                for (int i = 1; i < _targets.Count; i++)
                    b.Encapsulate(_targets[i].position);

                focus = b.center;
                float sizeX = b.size.x * 0.5f + padding;
                float sizeY = b.size.y * 0.5f + padding;
                float aspect = _cam.aspect > 0.01f ? _cam.aspect : 1.777f;
                desiredSize = Mathf.Max(sizeY, sizeX / aspect);
            }

            if (_locateFocus != null && Time.unscaledTime <= _locateUntil)
            {
                focus = Vector3.Lerp(focus, _locateFocus.position, locateFocusBlend);
                desiredSize *= _locateOrthoBoost;
            }

            desiredSize = Mathf.Clamp(desiredSize, minOrthoSize, maxOrthoSize * 1.35f);

            if (clampBounds != null)
            {
                float halfH = desiredSize;
                float halfW = desiredSize * _cam.aspect;
                var wb = clampBounds.WorldBounds;
                focus.x = Mathf.Clamp(focus.x, wb.min.x + halfW, wb.max.x - halfW);
                focus.y = Mathf.Clamp(focus.y, wb.min.y + halfH, wb.max.y - halfH);
            }

            Vector3 targetPos = new Vector3(focus.x, focus.y, fixedZ);
            float camDist = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.y),
                new Vector2(targetPos.x, targetPos.y));

            if (enableCatchUp && camDist > catchUpSoftTeleportDistance)
            {
                transform.position = targetPos;
                _velocity = Vector3.zero;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, smoothTime);
            }

            _cam.orthographicSize = Mathf.SmoothDamp(_cam.orthographicSize, desiredSize, ref _zoomVelocity, smoothTime);
        }

        private void ApplyCatchUp()
        {
            if (!enableCatchUp || _targets.Count < 2)
                return;

            // Soft-pull outliers toward the group centroid so co-op framing recovers.
            Vector3 centroid = Vector3.zero;
            for (int i = 0; i < _targets.Count; i++)
                centroid += _targets[i].position;
            centroid /= _targets.Count;

            for (int i = 0; i < _targets.Count; i++)
            {
                var t = _targets[i];
                float dist = Vector3.Distance(t.position, centroid);
                if (dist < catchUpSeparation)
                    continue;

                var rb = t.GetComponent<Rigidbody2D>();
                Vector3 pull = Vector3.MoveTowards(t.position, centroid,
                    catchUpPullSpeed * Time.deltaTime * (dist / catchUpSeparation));
                if (dist > catchUpSoftTeleportDistance)
                    pull = Vector3.Lerp(t.position, centroid, 0.35f);

                if (rb != null)
                    rb.MovePosition(pull);
                else
                    t.position = pull;
            }
        }

        private void RefreshTargets()
        {
            _targets.Clear();
            if (!autoFindPlayers)
            {
                if (fallbackTarget != null)
                    _targets.Add(fallbackTarget);
                return;
            }

            var players = PlayerTargetRegistry.Collect();
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] != null && players[i].IsAlive && players[i].Transform != null)
                    _targets.Add(players[i].Transform);
            }
        }
    }
}
