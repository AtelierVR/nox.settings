using Nox.CCK.Settings;
using Nox.CCK.Utils;
using UnityEngine;
using Nox.Settings.Runtime;

namespace Nox.Settings.Handlers {
	public sealed class Brightness : RangeHandler {
		public override string[] Path
			=> new[] { "accessibility", "visual", "brightness" };

		public override int Order => 10000;

		override protected GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/range.prefab");

		public Brightness() {
			SetRange(0.2f, 1f);
			SetStep(0.001f);
			SetValue(Value);
			SetLabelKey($"settings.entry.{string.Join(".", Path)}.label");
			SetValueKey("settings.range.value.percent");
		}

		public static new float Value {
			get
				=> Config.Load()
					.Get(
						new[] {
							"settings", "accessibility", "brightness"
						}, 1f
					);
			set {
				var config = Config.Load();
				config.Set(
					new[] {
						"settings", "accessibility", "brightness"
					}, value
				);
				config.Save();
			}
		}

		override protected void OnValueChanged(float value) {
			Value = value;
		}
	}
}