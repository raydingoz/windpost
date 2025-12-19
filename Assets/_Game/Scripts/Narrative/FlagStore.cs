using System;
using System.Collections.Generic;
using Ink.Runtime;
using UnityEngine;

namespace Windpost.Narrative
{
    public sealed class FlagStore : MonoBehaviour
    {
        [Serializable]
        private struct FlagEntry
        {
            public string Key;
            public int Value;
        }

        [Header("Initial flags (optional)")]
        [SerializeField] private List<FlagEntry> initialFlags = new List<FlagEntry>();

        private readonly Dictionary<string, int> _flags = new Dictionary<string, int>(StringComparer.Ordinal);

        public IReadOnlyDictionary<string, int> Flags => _flags;

        private void Awake()
        {
            ApplyInitialFlags();
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return defaultValue;
            }

            return _flags.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public void SetInt(string key, int value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            _flags[key] = value;
        }

        public void Clear()
        {
            _flags.Clear();
            ApplyInitialFlags();
        }

        public void ReplaceAll(IReadOnlyList<KeyValuePair<string, int>> flags)
        {
            _flags.Clear();

            if (flags == null)
            {
                return;
            }

            for (var i = 0; i < flags.Count; i++)
            {
                var pair = flags[i];
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    continue;
                }

                _flags[pair.Key] = pair.Value;
            }
        }

        public void BindToStory(Story story)
        {
            if (story == null)
            {
                Debug.LogError("[FlagStore] BindToStory called with null story.");
                return;
            }

            TryBindExternalFunctions(story);
        }

        private void ApplyInitialFlags()
        {
            if (initialFlags == null || initialFlags.Count == 0)
            {
                return;
            }

            for (var i = 0; i < initialFlags.Count; i++)
            {
                var entry = initialFlags[i];
                if (string.IsNullOrWhiteSpace(entry.Key))
                {
                    continue;
                }

                _flags[entry.Key] = entry.Value;
            }
        }

        private void TryBindExternalFunctions(Story story)
        {
            try
            {
                story.BindExternalFunction("GetFlag", (string key) => GetInt(key));
                story.BindExternalFunction("HasFlag", (string key) => GetInt(key) != 0);
                story.BindExternalFunction("SetFlag", (string key, int value) =>
                {
                    SetInt(key, value);
                    return value;
                });
                story.BindExternalFunction("IncFlag", (string key, int delta) =>
                {
                    var next = GetInt(key) + delta;
                    SetInt(key, next);
                    return next;
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FlagStore] External function bind failed (already bound?): {ex.Message}");
            }
        }
    }
}

