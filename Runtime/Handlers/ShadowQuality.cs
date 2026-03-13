using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Settings;
using Nox.CCK.Utils;
using Nox.Settings.Clients;
using Nox.Settings.Runtime;
using Nox.UI;
using Nox.UI.modals;
using UnityEngine;

namespace Nox.Settings.Handlers {
	public sealed class ShadowQuality : DropdownHandler {
		public override string[] GetPath()
			=> new[] { "graphic", "quality", "shadow_quality" };

		public override int GetOrder() => 1001;

		private static string[] GetConfigPath()
			=> new[] { "settings", "graphic", "shadow_quality" };

		protected override GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/dropdown.prefab");

		protected override IModalBuilder GetModalBuilder(IMenu menu)
			=> Client.UiAPI.MakeModal(menu);

		private static Dictionary<string, string[]> BuildOptions() {
			var dict = new Dictionary<string, string[]>();
			foreach (ShadowResolution res in Enum.GetValues(typeof(ShadowResolution))) {
				var key = res.ToString().ToSnakeCase();
				dict[key] = new[] { $"settings.entry.graphic.shadow_quality.option.{key}" };
			}
			return dict;
		}

		public ShadowQuality() {
			SetLabel($"settings.entry.{string.Join(".", GetPath())}.label");
			SetOptions(BuildOptions());
			var saved = Config.Load().Get(GetConfigPath(), (int)Value);
			Value = (ShadowResolution)saved;
			SetValue(Value.ToString().ToSnakeCase(), false);
		}

		protected override void OnValueChanged(string value) {
			foreach (ShadowResolution res in Enum.GetValues(typeof(ShadowResolution))) {
				if (res.ToString().ToSnakeCase() != value) continue;
				Value = res;
				return;
			}
		}

		private static ShadowResolution Value {
			get => QualitySettings.shadowResolution;
			set {
				QualitySettings.shadowResolution = value;
				var config = Config.Load();
				config.Set(GetConfigPath(), (int)value);
				config.Save();
			}
		}
	}
}
