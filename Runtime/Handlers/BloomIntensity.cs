using Nox.CCK.Settings;
using Nox.CCK.Utils;
using UnityEngine;
using Nox.Settings.Runtime;

namespace Nox.Settings.Handlers {
	public sealed class BloomIntensity : RangeHandler {
		public override string[] GetPath()
			=> new[] { "graphic", "quality", "bloom_intensity" };

		public override int GetOrder() => 1003;

		public BloomIntensity() {
			SetRange(0f, 1f);
			SetStep(0.001f);
			SetValue(Value);
			SetLabelKey($"settings.entry.{string.Join(".", GetPath())}.label");
			SetValueKey("settings.range.value.percent");
		}

		public static float Value {
			get
				=> Config.Load()
					.Get(
						new[] {
							"settings", "accessibility", "bloom_intensity"
						}, 0f
					);
			set {
				var config = Config.Load();
				config.Set(
					new[] {
						"settings", "accessibility", "bloom_intensity"
					}, value
				);
				config.Save();
			}
		}

		override protected GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/range.prefab");

		override protected void OnValueChanged(float value) {
			Value = value;
		}
	}
}