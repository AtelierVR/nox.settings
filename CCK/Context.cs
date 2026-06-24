using System.Collections.Generic;
using Nox.Settings;

namespace Nox.CCK.Settings {
	/// <summary>
	/// Default implementation of Nox.Settings.IContext with generic storage.
	/// </summary>
	public class Context : IContext {
		private readonly Dictionary<string, object> _store = new();

		/// <summary>
		/// Store a value by key. Returns this for chaining.
		/// </summary>
		public Context Set(string key, object value) {
			_store[key] = value;
			return this;
		}

		public T Get<T>(string key, T defaultValue = default) {
			if (_store.TryGetValue(key, out var value) && value is T typed)
				return typed;
			return defaultValue;
		}

		/// <summary>
		/// Check if a key exists.
		/// </summary>
		public bool Has(string key) 
            => _store.ContainsKey(key);

		/// <summary>
		/// Remove a key. Returns this for chaining.
		/// </summary>
		public Context Delete(string key) {
			_store.Remove(key);
			return this;
		}

		public IReadOnlyDictionary<string, object> All 
            => _store;
	}
}
