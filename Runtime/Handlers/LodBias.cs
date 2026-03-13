using Nox.CCK.Settings;
using Nox.CCK.Utils;
using Nox.Settings.Runtime;
using UnityEngine;

namespace Nox.Settings.Handlers {
	public sealed class LodBias : RangeHandler {
		public override string[] GetPath()
			=> new[] { "performances", "lod_bias" };

		public override int GetOrder() => 1;

		private static string[] GetConfigPath()
			=> new[] { "settings", "graphic", "lod_bias" };

		protected override GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/range.prefab");

		public LodBias() {
			SetRange(0.25f, 4f);
			SetStep(0.01f);
			SetValue(Value);
			SetLabelKey($"settings.entry.{string.Join(".", GetPath())}.label");
			SetValueKey("settings.range.value.float");
		}

		protected override void OnValueChanged(float value) {
			Value = value;
		}

		private static float Value {
			get => Config.Load().Get(GetConfigPath(), QualitySettings.lodBias);
			set {
				QualitySettings.lodBias = value;
				var config = Config.Load();
				config.Set(GetConfigPath(), value);
				config.Save();
			}
		}
	}
}
