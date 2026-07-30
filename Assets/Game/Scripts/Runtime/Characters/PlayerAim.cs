using UnityEngine;

namespace RealmShards
{
    public sealed class PlayerAim : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private bool preferAimOverMove = true;

        private Vector2 _aimInput;
        private Vector2 _moveInput;
        private Vector2 _lastAim = Vector2.down;
        private bool _usingMouse;
        private bool _hasExplicitAim;

        public Vector2 AimDirection => _lastAim;
        public FacingDirection8 Facing => FacingUtility.FromVector(_lastAim);

        private void Awake()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }
        }

        public void SetCamera(Camera cam)
        {
            worldCamera = cam;
        }

        public void SetMoveInput(Vector2 move)
        {
            _moveInput = move;
            Recalculate();
        }

        public void SetAimInput(Vector2 aim, bool isMouseDevice)
        {
            _aimInput = aim;
            _usingMouse = isMouseDevice;
            _hasExplicitAim = aim.sqrMagnitude > 0.01f || isMouseDevice;
            Recalculate();
        }

        public void SetMouseScreenPosition(Vector2 screenPosition)
        {
            _usingMouse = true;
            _hasExplicitAim = true;

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (worldCamera == null)
            {
                return;
            }

            Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));
            world.z = 0f;
            Vector2 dir = (Vector2)(world - transform.position);
            if (dir.sqrMagnitude > 0.0001f)
            {
                _aimInput = dir.normalized;
                _lastAim = _aimInput;
            }
        }

        private void Recalculate()
        {
            if (_usingMouse && _hasExplicitAim && _aimInput.sqrMagnitude > 0.0001f)
            {
                _lastAim = _aimInput.normalized;
                return;
            }

            if (preferAimOverMove && _aimInput.sqrMagnitude > 0.25f)
            {
                _lastAim = _aimInput.normalized;
                return;
            }

            if (_moveInput.sqrMagnitude > 0.01f)
            {
                _lastAim = _moveInput.normalized;
            }
        }
    }
}
