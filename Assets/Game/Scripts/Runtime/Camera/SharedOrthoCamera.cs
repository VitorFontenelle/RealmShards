using System.Collections.Generic;
using RealmShards.Enemies;
using RealmShards.Rooms;
using UnityEngine;

namespace RealmShards.CameraSystem
{
    /// <summary>
    /// Orthographic camera that tracks all living players (or spawn center). No Cinemachine.
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

        private Camera _cam;
        private Vector3 _velocity;
        private float _zoomVelocity;
        private readonly List<Transform> _targets = new List<Transform>(8);

        public void Configure(RoomBounds bounds, Transform fallback, float minZoom, float maxZoom)
        {
            clampBounds = bounds;
            fallbackTarget = fallback;
            minOrthoSize = minZoom;
            maxOrthoSize = maxZoom;
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

            desiredSize = Mathf.Clamp(desiredSize, minOrthoSize, maxOrthoSize);

            if (clampBounds != null)
            {
                float halfH = desiredSize;
                float halfW = desiredSize * _cam.aspect;
                var wb = clampBounds.WorldBounds;
                focus.x = Mathf.Clamp(focus.x, wb.min.x + halfW, wb.max.x - halfW);
                focus.y = Mathf.Clamp(focus.y, wb.min.y + halfH, wb.max.y - halfH);
            }

            Vector3 targetPos = new Vector3(focus.x, focus.y, fixedZ);
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, smoothTime);
            _cam.orthographicSize = Mathf.SmoothDamp(_cam.orthographicSize, desiredSize, ref _zoomVelocity, smoothTime);
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
