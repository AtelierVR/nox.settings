using System.Collections.Generic;
using Nox.CCK.Language;
using Nox.CCK.Settings;
using Nox.CCK.Utils;
using Nox.Settings.Clients;
using Nox.Settings.Runtime;
using Nox.UI;
using Nox.UI.modals;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Nox.Settings.Handlers {
	public sealed class MSAA : DropdownHandler {
		public override string[] GetPath()
			=> new[] { "graphic", "anti_aliasing", "msaa" };

		public override int GetOrder() => 2001;

		override protected GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/dropdown.prefab");

		override protected IModalBuilder GetModalBuilder(IMenu menu)
			=> Client.UiAPI.MakeModal(menu);

		private static string[] GetConfigPath()
			=> new[] { "settings", "graphic", "msaa" };

		public override bool IsActive()
			=> AntiAliasing.Value == AntialiasingMode.SubpixelMorphologicalAntiAliasing;

		private static Dictionary<string, string[]> GetAntiAliasingOptions()
			=> new() {
				[0.ToString()] = new[] { "settings.entry.graphic.msaa.option.off" },
				[1.ToString()] = new[] { "settings.entry.graphic.msaa.option.x2" },
				[2.ToString()] = new[] { "settings.entry.graphic.msaa.option.x4" },
				[3.ToString()] = new[] { "settings.entry.graphic.msaa.option.x8" }
			};

		public MSAA() {
			SetLabel($"settings.entry.{string.Join(".", GetPath())}.label");
			SetOptions(GetAntiAliasingOptions());
			Value = Config.Load().Get(GetConfigPath(), Value);
			SetValue(Value.ToString(), false);
		}

		override protected void OnValueChanged(string value) {
			if (!int.TryParse(value, out var msaaLevel))
				return;
			Value = msaaLevel;
		}

		private static int Value {
			get => QualitySettings.antiAliasing;
			set {
				QualitySettings.antiAliasing = value;
				var config = Config.Load();
				config.Set(GetConfigPath(), value);
				config.Save();
			}
		}
	}
}