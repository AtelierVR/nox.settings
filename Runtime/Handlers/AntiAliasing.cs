using System;
using System.Collections.Generic;
using Nox.CCK.Settings;
using Nox.CCK.Utils;
using Nox.Settings.Clients;
using Nox.Settings.Runtime;
using Nox.UI;
using Nox.UI.modals;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Nox.Settings.Handlers {
	public sealed class AntiAliasing : DropdownHandler {
		public override string[] GetPath()
			=> new[] { "graphic", "anti_aliasing", "mode" };

		public override int GetOrder() => 2000;

		override protected GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/dropdown.prefab");

		override protected IModalBuilder GetModalBuilder(IMenu menu)
			=> Client.UiAPI.MakeModal(menu);

		private static string[] GetConfigPath()
			=> new[] { "settings", "graphic", "anti_aliasing" };

		private static Dictionary<string, string[]> GetAntiAliasingOptions() {
			var dict = new Dictionary<string, string[]>();

			foreach (var mode in Enum.GetValues(typeof(AntialiasingMode))) {
				var name = mode.ToString().ToSnakeCase();
				dict[name] = new[] { $"settings.entry.graphic.anti_aliasing.option.{name}" };
			}

			return dict;
		}

		public AntiAliasing() {
			SetLabel($"settings.entry.{string.Join(".", GetPath())}.label");
			SetOptions(GetAntiAliasingOptions());
			Value = Config.Load().Get(GetConfigPath(), Value);
			SetValue(Value.ToString().ToSnakeCase(), false);
		}

		override protected void OnValueChanged(string value) {
			foreach (AntialiasingMode mode in Enum.GetValues(typeof(AntialiasingMode))) {
				if (mode.ToString().ToSnakeCase() != value)
					continue;
				Value = mode;
				return;
			}
		}

		public static void OnInstantiate(GameObject arg0)
			=> UpdateCamera();

		private static void UpdateCamera()
			=> UpdateCamera(Value);

		private static void UpdateCamera(AntialiasingMode mode) {
			var cameras = Camera.allCameras;
			foreach (var camera in cameras) {
				var data = camera.GetUniversalAdditionalCameraData();
				if (data)
					data.antialiasing = Value;
			}
		}

		public static AntialiasingMode Value {
			get => (AntialiasingMode)Config.Load().Get(GetConfigPath(), (int)AntialiasingMode.None);
			set {
				var config = Config.Load();
				config.Set(GetConfigPath(), (int)value);
				config.Save();
				UpdateCamera(value);
			}
		}
	}
}