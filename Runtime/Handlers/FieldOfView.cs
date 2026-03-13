using Nox.CCK.Settings;
using Nox.CCK.Utils;
using Nox.Settings.Runtime;
using UnityEngine;

namespace Nox.Settings.Handlers {
	public sealed class FieldOfView : RangeHandler {
		public override string[] GetPath()
			=> new[] { "desktop", "fov" };

		private static string[] GetConfigPath()
			=> new[] { "settings", "desktop", "fov" };

		protected override GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/range.prefab");

		public override bool IsActive() {
			try {
				return Main.ControllerAPI?.Current?.GetId() == "desktop";
			} catch {
				return false;
			}
		}

		public override void OnUpdated(IHandler handler)
			=> ApplyFov();

		public FieldOfView() {
			SetRange(30f, 120f);
			SetStep(1f);
			SetValue(Value);
			SetLabelKey($"settings.entry.{string.Join(".", GetPath())}.label");
			SetValueKey("settings.range.value.float");
		}

		protected override void OnValueChanged(float value) {
			Value = value;
			ApplyFov();
		}

		private static void ApplyFov() {
			try {
				var camera = Main.ControllerAPI?.Current?.GetCamera();
				if (camera) camera.fieldOfView = Value;
			} catch { }
		}

		private static float Value {
			get => Config.Load().Get(GetConfigPath(), 60f);
			set {
				var config = Config.Load();
				config.Set(GetConfigPath(), value);
				config.Save();
			}
		}
	}
}
