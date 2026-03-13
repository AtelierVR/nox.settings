using Nox.CCK.Settings;
using Nox.CCK.Utils;
using Nox.Settings.Runtime;
using UnityEngine;

namespace Nox.Settings.Handlers {
	public sealed class PixelLightCount : RangeHandler {
		public override string[] GetPath()
			=> new[] { "graphic", "quality", "pixel_light_count" };

		public override int GetOrder() => 1002;

		private static string[] GetConfigPath()
			=> new[] { "settings", "graphic", "pixel_light_count" };

		protected override GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/range.prefab");

		public PixelLightCount() {
			SetRange(0f, 8f);
			SetStep(1f);
			SetValue(Value);
			SetLabelKey($"settings.entry.{string.Join(".", GetPath())}.label");
			SetValueKey("settings.range.value.float");
		}

		protected override void OnValueChanged(float value) {
			Value = Mathf.RoundToInt(value);
		}

		private static int Value {
			get => Config.Load().Get(GetConfigPath(), QualitySettings.pixelLightCount);
			set {
				QualitySettings.pixelLightCount = value;
				var config = Config.Load();
				config.Set(GetConfigPath(), value);
				config.Save();
			}
		}
	}
}
