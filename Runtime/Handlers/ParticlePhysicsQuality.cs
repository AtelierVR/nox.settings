using Nox.CCK.Settings;
using Nox.CCK.Utils;
using Nox.Settings.Runtime;
using UnityEngine;

namespace Nox.Settings.Handlers {
	public sealed class ParticlePhysicsQuality : RangeHandler {
		public override string[] Path
			=> new[] { "performances", "physics_quality" };

		public override int Order => 20002;

		private static string[] GetConfigPath()
			=> new[] { "settings", "graphic", "particle_physics_quality" };

		protected override GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/range.prefab");

		public ParticlePhysicsQuality() {
			SetRange(4f, 4096f);
			SetStep(4f);
			SetValue(Value);
			SetLabelKey($"settings.entry.{string.Join(".", Path)}.label");
			SetValueKey("settings.range.value.float");
		}

		protected override void OnValueChanged(float value) {
			Value = Mathf.RoundToInt(value);
		}

		private static new int Value {
			get => Config.Load().Get(GetConfigPath(), QualitySettings.particleRaycastBudget);
			set {
				QualitySettings.particleRaycastBudget = value;
				var config = Config.Load();
				config.Set(GetConfigPath(), value);
				config.Save();
			}
		}
	}
}
