using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Controllers;
using Nox.Settings.Commands;
using Nox.Terminal;
using UnityEngine.Events;

namespace Nox.Settings.Runtime {
	public class Main : IMainModInitializer, ISettingAPI {
		static readonly internal List<IHandler> Handlers = new();
		public static            Main           Instance;
		public                   IMainModCoreAPI CoreAPI;
		private                  LanguagePack   _lang;

		public static IControllerAPI ControllerAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("controllers")
				.GetInstance<IControllerAPI>();

		static readonly internal UnityEvent<IHandler> OnHandlerAdded   = new();
		static readonly internal UnityEvent<IHandler> OnHandlerRemoved = new();
		static readonly internal UnityEvent<IHandler> OnHandlerUpdated = new();

		private void InvokeHandlerAdded(IHandler handler) {
			OnHandlerAdded.Invoke(handler);
			CoreAPI.EventAPI.Emit("setting_handler_added", handler);
		}

		private void InvokeHandlerRemoved(IHandler handler) {
			OnHandlerRemoved.Invoke(handler);
			CoreAPI.EventAPI.Emit("setting_handler_removed", handler);
		}

		public IHandler Add(IHandler handler) {
			if (handler == null) {
				CoreAPI.LoggerAPI.LogError("Cannot register a null setting handler");
				return null;
			}

			if (string.IsNullOrWhiteSpace(handler.Path.FirstOrDefault())) {
				CoreAPI.LoggerAPI.LogError("Cannot register a setting handler with an empty id");
				return null;
			}

			if (Has(handler.Path)) {
				CoreAPI.LoggerAPI.LogError($"Setting handler with id {handler.Path} already exists");
				return null;
			}

			Handlers.Add(handler);
			InvokeHandlerAdded(handler);
			return handler;
		}

		public void Remove(string[] path) {
			var handler = Get(path);

			if (handler == null) {
				CoreAPI.LoggerAPI.LogError($"Setting handler with id {path} does not exist");
				return;
			}

			if (handler is IDisposable disposable)
				disposable.Dispose();

			Handlers.Remove(handler);
			InvokeHandlerRemoved(handler);
		}

		public IHandler Get(string[] path)
			=> Handlers.FirstOrDefault(h => h.Path.SequenceEqual(path));

		public bool Has(string[] path)
			=> Handlers.Exists(b => b.Path.SequenceEqual(path));

		internal static ITerminalAPI TerminalAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("terminal")
				.GetInstance<ITerminalAPI>();

		private IHandler[]        _handlers               = Array.Empty<IHandler>();
		private EventSubscription _controllerChangedSub;
		private uint              _settingsCommandId;

		private static void OnControllerChangedEvent(EventData _)
			=> PropagateHandlerUpdated(null);

		/// <summary>A value changed: the settings UI rereads everything to stay in sync.</summary>
		private static void OnConfigChanged(string _)
			=> PropagateHandlerUpdated(null);

		/// <summary>The config was reloaded from disk: every handler applies its stored value.</summary>
		private static void OnConfigReloaded() {
			foreach (var handler in Handlers.ToArray()) {
				var path  = new[] { "settings" }.Concat(handler.Path).ToArray();
				var token = Config.Load().Get(path);

				if (token == null)
					continue;

				try {
					handler.Apply(token.ToObject<object>());
				} catch (Exception exception) {
					Instance?.CoreAPI?.LoggerAPI?.LogError($"Unable to apply the setting {string.Join(".", handler.Path)}: {exception.Message}");
				}
			}

			PropagateHandlerUpdated(null);
		}

		private static void PropagateHandlerUpdated(IHandler changedHandler) {
			foreach (var h in Handlers)
				h.OnUpdated(changedHandler);
			OnHandlerUpdated.Invoke(changedHandler);
		}

		public void OnInitializeMain(IMainModCoreAPI api) {
			Instance = this;
			CoreAPI  = api;
			SettingsNotifier.OnHandlerUpdated += PropagateHandlerUpdated;
			Config.OnChanged.AddListener(OnConfigChanged);
			Config.OnReloaded.AddListener(OnConfigReloaded);
			_controllerChangedSub = CoreAPI.EventAPI.Subscribe("controller_changed", OnControllerChangedEvent);
			_lang    = CoreAPI.AssetAPI.GetAsset<LanguagePack>("lang.asset");
			LanguageManager.AddPack(_lang);
			_handlers = DefaultSettings.GetHandlers();
			foreach (var handler in _handlers)
				Add(handler);
			DefaultSettings.Listen();

			// Register terminal command
			_settingsCommandId = TerminalAPI.Register(new SettingsCommand());
		}

		public void OnDisposeMain() {
			DefaultSettings.Unlisten();
			Config.OnChanged.RemoveListener(OnConfigChanged);
			Config.OnReloaded.RemoveListener(OnConfigReloaded);
			if (_controllerChangedSub != null)
				CoreAPI.EventAPI.Unsubscribe(_controllerChangedSub);
			_controllerChangedSub = null;
			SettingsNotifier.OnHandlerUpdated -= PropagateHandlerUpdated;
			foreach (var handler in Handlers.ToArray())
				Remove(handler.Path);
			Handlers.Clear();
			_handlers = Array.Empty<IHandler>();
			// Unregister terminal command
			TerminalAPI.Unregister(_settingsCommandId);
			_settingsCommandId = 0;

			LanguageManager.RemovePack(_lang);
			_lang    = null;
			Instance = null;
			CoreAPI  = null;
		}
	}
}