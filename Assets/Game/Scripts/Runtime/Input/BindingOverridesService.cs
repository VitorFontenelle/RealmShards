using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RealmShards.Input
{
    /// <summary>
    /// Persists Input System binding overrides under persistentDataPath.
    /// </summary>
    public sealed class BindingOverridesService
    {
        public const string FileName = "input_bindings.json";

        private readonly InputActionAsset _actions;
        private string _defaultsJson;

        public BindingOverridesService(InputActionAsset actions)
        {
            _actions = actions ?? throw new ArgumentNullException(nameof(actions));
            CaptureDefaults();
        }

        public string OverridesPath => Path.Combine(Application.persistentDataPath, FileName);

        public void CaptureDefaults()
        {
            if (_actions != null)
                _defaultsJson = _actions.SaveBindingOverridesAsJson();
        }

        public void Load()
        {
            if (_actions == null)
                return;

            var path = OverridesPath;
            if (!File.Exists(path))
                return;

            try
            {
                var json = File.ReadAllText(path);
                if (!string.IsNullOrWhiteSpace(json))
                    _actions.LoadBindingOverridesFromJson(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Input] Failed to load binding overrides: {ex.Message}");
            }
        }

        public void Save()
        {
            if (_actions == null)
                return;

            try
            {
                var dir = Path.GetDirectoryName(OverridesPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(OverridesPath, _actions.SaveBindingOverridesAsJson());
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Input] Failed to save binding overrides: {ex.Message}");
            }
        }

        public void ResetToDefaults()
        {
            if (_actions == null)
                return;

            _actions.RemoveAllBindingOverrides();
            if (!string.IsNullOrEmpty(_defaultsJson))
                _actions.LoadBindingOverridesFromJson(_defaultsJson);

            // Clear file so next launch uses asset defaults.
            try
            {
                if (File.Exists(OverridesPath))
                    File.Delete(OverridesPath);
            }
            catch
            {
                // ignored
            }
        }

        /// <summary>
        /// Apply a new binding; if another action already uses the control, steal it.
        /// </summary>
        public bool ApplyBindingWithConflictReplace(
            InputAction action,
            int bindingIndex,
            InputBinding binding,
            out string replacedActionName)
        {
            replacedActionName = null;
            if (action == null || _actions == null)
                return false;

            string path = binding.effectivePath;
            if (string.IsNullOrEmpty(path))
                path = binding.path;

            foreach (var map in _actions.actionMaps)
            {
                foreach (var other in map.actions)
                {
                    for (int i = 0; i < other.bindings.Count; i++)
                    {
                        if (other == action && i == bindingIndex)
                            continue;
                        var b = other.bindings[i];
                        if (b.isComposite || b.isPartOfComposite)
                            continue;
                        string otherPath = string.IsNullOrEmpty(b.overridePath) ? b.path : b.overridePath;
                        if (string.Equals(otherPath, path, StringComparison.OrdinalIgnoreCase))
                        {
                            other.ApplyBindingOverride(i, string.Empty);
                            replacedActionName = other.name;
                        }
                    }
                }
            }

            action.ApplyBindingOverride(bindingIndex, binding);
            Save();
            return true;
        }
    }
}
