using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Dock.WinUI3
{
    /// <summary>
    /// File-backed dictionary for WinUIEx's <c>WindowManager.PersistenceStorage</c>.
    /// Unpackaged apps must supply this themselves: the built-in storage uses
    /// ApplicationData, which requires package identity. Values are the base64
    /// placement strings WinUIEx writes; every mutation persists the file, so a
    /// crash after a window close loses nothing.
    /// </summary>
    public sealed class FilePersistenceStorage : IDictionary<string, object>
    {
        private readonly string _path;
        private readonly Dictionary<string, object> _data = new();

        public FilePersistenceStorage(string path)
        {
            _path = path;

            try
            {
                if (File.Exists(path)
                    && JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) is { } loaded)
                {
                    foreach (var pair in loaded)
                    {
                        _data[pair.Key] = pair.Value;
                    }
                }
            }
            catch
            {
                // A corrupt store just means windows open at their defaults.
            }
        }

        private void Flush()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path));

                var strings = new Dictionary<string, string>();
                foreach (var pair in _data)
                {
                    strings[pair.Key] = pair.Value?.ToString() ?? string.Empty;
                }

                File.WriteAllText(_path, JsonSerializer.Serialize(
                    strings, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch
            {
                // Best-effort persistence; never let it take a window close down.
            }
        }

        public object this[string key]
        {
            get => _data[key];
            set
            {
                _data[key] = value;
                Flush();
            }
        }

        public ICollection<string> Keys => _data.Keys;
        public ICollection<object> Values => _data.Values;
        public int Count => _data.Count;
        public bool IsReadOnly => false;

        public void Add(string key, object value)
        {
            _data.Add(key, value);
            Flush();
        }

        public void Add(KeyValuePair<string, object> item) => Add(item.Key, item.Value);

        public void Clear()
        {
            _data.Clear();
            Flush();
        }

        public bool Contains(KeyValuePair<string, object> item) => _data.ContainsKey(item.Key);
        public bool ContainsKey(string key) => _data.ContainsKey(key);

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
            => ((IDictionary<string, object>)_data).CopyTo(array, arrayIndex);

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _data.GetEnumerator();

        public bool Remove(string key)
        {
            var removed = _data.Remove(key);
            if (removed)
            {
                Flush();
            }

            return removed;
        }

        public bool Remove(KeyValuePair<string, object> item) => Remove(item.Key);
        public bool TryGetValue(string key, out object value) => _data.TryGetValue(key, out value);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
