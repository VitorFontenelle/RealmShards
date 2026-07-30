using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RealmShards.Input
{
    /// <summary>
    /// Tracks which InputDevices are claimed by local players so two players cannot share one pad/KBM pair.
    /// </summary>
    public sealed class DeviceClaimService
    {
        private readonly Dictionary<int, int> _deviceIdToPlayer = new Dictionary<int, int>();
        private readonly Dictionary<int, List<int>> _playerToDevices = new Dictionary<int, List<int>>();

        public bool IsDeviceClaimed(InputDevice device)
        {
            return device != null && _deviceIdToPlayer.ContainsKey(device.deviceId);
        }

        public bool TryGetPlayerForDevice(InputDevice device, out int playerIndex)
        {
            playerIndex = -1;
            if (device == null)
                return false;
            return _deviceIdToPlayer.TryGetValue(device.deviceId, out playerIndex);
        }

        public bool TryClaim(int playerIndex, params InputDevice[] devices)
        {
            if (devices == null || devices.Length == 0)
                return false;

            foreach (var d in devices)
            {
                if (d == null) continue;
                if (_deviceIdToPlayer.TryGetValue(d.deviceId, out int owner) && owner != playerIndex)
                    return false;
            }

            ReleasePlayer(playerIndex);
            var list = new List<int>();
            foreach (var d in devices)
            {
                if (d == null) continue;
                _deviceIdToPlayer[d.deviceId] = playerIndex;
                list.Add(d.deviceId);
            }

            _playerToDevices[playerIndex] = list;
            return list.Count > 0;
        }

        public void ReleasePlayer(int playerIndex)
        {
            if (!_playerToDevices.TryGetValue(playerIndex, out var list))
                return;

            foreach (var id in list)
                _deviceIdToPlayer.Remove(id);
            _playerToDevices.Remove(playerIndex);
        }

        public void ReleaseDevice(InputDevice device)
        {
            if (device == null) return;
            if (!_deviceIdToPlayer.TryGetValue(device.deviceId, out int player))
                return;
            ReleasePlayer(player);
        }

        public void Clear()
        {
            _deviceIdToPlayer.Clear();
            _playerToDevices.Clear();
        }

        public IReadOnlyCollection<int> ClaimedPlayerIndices => _playerToDevices.Keys;
    }

    /// <summary>
    /// Hub / lobby join state for mixed keyboard+mouse and gamepads.
    /// </summary>
    public sealed class LocalCoopLobby
    {
        public const int MaxPlayers = 4;

        public sealed class Slot
        {
            public int PlayerIndex;
            public bool Joined;
            public string SchemeName;
            public string DeviceLabel;
            public InputDevice PrimaryDevice;
            public InputDevice SecondaryDevice;
        }

        private readonly Slot[] _slots = new Slot[MaxPlayers];
        private readonly DeviceClaimService _claims = new DeviceClaimService();

        public DeviceClaimService Claims => _claims;
        public int JoinedCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < MaxPlayers; i++)
                    if (_slots[i] != null && _slots[i].Joined) n++;
                return n;
            }
        }

        public LocalCoopLobby()
        {
            for (int i = 0; i < MaxPlayers; i++)
                _slots[i] = new Slot { PlayerIndex = i };
        }

        public Slot GetSlot(int index) => _slots[Mathf.Clamp(index, 0, MaxPlayers - 1)];

        public bool TryJoin(InputDevice device, string scheme, out int playerIndex, out string fail)
        {
            fail = null;
            playerIndex = -1;
            if (device == null)
            {
                fail = "No device.";
                return false;
            }

            if (_claims.IsDeviceClaimed(device))
            {
                fail = "Device already in use.";
                return false;
            }

            // Keyboard claims mouse as secondary for KBM scheme.
            InputDevice secondary = null;
            if (device is Keyboard && Mouse.current != null)
            {
                if (_claims.IsDeviceClaimed(Mouse.current))
                {
                    fail = "Mouse already claimed.";
                    return false;
                }
                secondary = Mouse.current;
            }

            if (device is Mouse)
            {
                fail = "Join with Space (keyboard) or A (gamepad), not mouse alone.";
                return false;
            }

            int free = -1;
            for (int i = 0; i < MaxPlayers; i++)
            {
                if (!_slots[i].Joined)
                {
                    free = i;
                    break;
                }
            }

            if (free < 0)
            {
                fail = "Lobby full.";
                return false;
            }

            if (secondary != null)
            {
                if (!_claims.TryClaim(free, device, secondary))
                {
                    fail = "Could not claim keyboard+mouse.";
                    return false;
                }
            }
            else if (!_claims.TryClaim(free, device))
            {
                fail = "Could not claim device.";
                return false;
            }

            _slots[free].Joined = true;
            _slots[free].SchemeName = scheme;
            _slots[free].PrimaryDevice = device;
            _slots[free].SecondaryDevice = secondary;
            _slots[free].DeviceLabel = Describe(device, scheme);
            playerIndex = free;
            return true;
        }

        public void Leave(int playerIndex)
        {
            playerIndex = Mathf.Clamp(playerIndex, 0, MaxPlayers - 1);
            _claims.ReleasePlayer(playerIndex);
            _slots[playerIndex].Joined = false;
            _slots[playerIndex].SchemeName = null;
            _slots[playerIndex].DeviceLabel = null;
            _slots[playerIndex].PrimaryDevice = null;
            _slots[playerIndex].SecondaryDevice = null;
        }

        public void ResetAll()
        {
            _claims.Clear();
            for (int i = 0; i < MaxPlayers; i++)
                Leave(i);
        }

        public void HandleDeviceLost(InputDevice device)
        {
            if (device == null) return;
            if (_claims.TryGetPlayerForDevice(device, out int player))
                Leave(player);
        }

        private static string Describe(InputDevice device, string scheme)
        {
            if (device is Keyboard)
                return "Keyboard + Mouse";
            if (device is Gamepad gp)
                return string.IsNullOrEmpty(gp.displayName) ? "Gamepad" : gp.displayName;
            return device.displayName;
        }
    }
}
