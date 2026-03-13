using System.Collections.Generic;
using Nox.CCK.Settings;
using Nox.CCK.Utils;
using Nox.UI;
using Nox.UI.modals;
using UnityEngine;
using Nox.Settings.Clients;
using Nox.Settings.Runtime;

namespace Nox.Settings.Handlers {
	public sealed class WindowSize : DropdownHandler {
		private const string Fullscreen = "fullscreen";
		private const string Maximized  = "maximized";
		private const string Windowed   = "windowed";

		public override string[] GetPath()
			=> new[] { "graphic", "display", "window_size" };

		public override int GetOrder() => 1;

		private static string[] GetConfigPath()
			=> new[] { "settings", "graphic", "window_mode" };
		
		private static Dictionary<string, string[]> GetAvailableSize()
			=> new() {
				[Fullscreen] = new[] { "settings.entry.graphic.window_size.option.fullscreen" },
				[Maximized]  = new[] { "settings.entry.graphic.window_size.option.maximized" },
				[Windowed]   = new[] { "settings.entry.graphic.window_size.option.windowed" }
			};

		override protected GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/dropdown.prefab");

		override protected IModalBuilder GetModalBuilder(IMenu menu)
			=> Client.UiAPI.MakeModal(menu);

		public WindowSize() {
			SetLabel($"settings.entry.{string.Join(".", GetPath())}.label");
			SetOptions(GetAvailableSize());
			Value = Value;
			SetValue(GetCurrentWindowMode(), false);
			#if UNITY_EDITOR
			// In editor, disable edition since changing window mode can cause focus issues.
			SetInteractable(false);
			#endif
		}

		private static string GetCurrentWindowMode() {
			if (Screen.fullScreen)
				return Fullscreen;

			// Check if window is maximized (approximate check)
			if (Screen.width == Display.main.systemWidth && Screen.height == Display.main.systemHeight)
				return Maximized;

			return Windowed;
		}

		private static string Value {
			get {
				var config = Config.Load();
				return config.Get(GetConfigPath(), Windowed);
			}
			set {
				#if !UNITY_EDITOR
				switch (value) {
					case Fullscreen:
						Screen.fullScreen = true;
						break;

					case Maximized:
						Screen.fullScreen = false;
						Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.MaximizedWindow);
						break;

					case Windowed:
						Screen.fullScreen = false;
						break;
				}

				var config = Config.Load();
				config.Set(GetConfigPath(), value);
				config.Save();
				#endif
			}
		}
		
		override protected void OnValueChanged(string value)
			=> Value = value;
	}
}