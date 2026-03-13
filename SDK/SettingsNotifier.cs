using System;

namespace Nox.Settings {
	/// <summary>
	/// Shared notification bridge so CCK base-handler classes (which cannot reference
	/// Nox.Settings.Runtime.Main directly) can still broadcast value-change events.
	/// Main subscribes here and propagates to all registered handlers.
	/// </summary>
	public static class SettingsNotifier {
		public static event Action<IHandler> OnHandlerUpdated;

		public static void NotifyUpdated(IHandler handler)
			=> OnHandlerUpdated?.Invoke(handler);
	}
}
