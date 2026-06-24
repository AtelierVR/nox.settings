using System.Collections.Generic;

namespace Nox.Settings {
	/// <summary>
	/// Context for settings handlers, with generic storage.
	/// </summary>
	public interface IContext {
        /// <summary>
        /// Get a stored value by key and type.
        /// </summary>
		public T Get<T>(string key, T defaultValue = default);

        /// <summary>
        /// Verify if a key exists in the context.
        /// </summary>
        public bool Has(string key);

        /// <summary>
        /// Get all stored values in the context.
        /// </summary>
		public IReadOnlyDictionary<string, object> All { get; }
	}
}
