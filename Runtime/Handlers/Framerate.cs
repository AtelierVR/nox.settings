using Nox.CCK.Settings;
using Nox.CCK.Utils;
using Nox.Settings.Runtime;
using Nox.UI;
using UnityEngine;

namespace Nox.Settings.Handlers {
	public class Framerate : RangeHandler {
		public class FramerateBehavior : MonoBehaviour {
			public Framerate Handler;
			public const float Delay = 0.5f;
			private float _timer = Delay;

			private void Update() {
				_timer += Time.deltaTime;
				if (_timer < Delay)
					return;
				_timer = 0f;
				var v = Mathf.RoundToInt(1f / Time.deltaTime);
				Handler.SetTypeKey("settings.range.value.framerate", v.ToString());
			}

			private void OnDestroy() {
				Handler = null;
				Destroy(this);
			}
		}

		public const int VSync = 10;
		public const int Uncapped = MaxFramerate;
		public const int MaxFramerate = 240;

		private FramerateBehavior Behavior;

		public override string[] GetPath()
			=> new[] { "performances", "framerate" };

		public static string[] GetConfigPath()
			=> new[] { "settings", "performances", "framerate" };

		public override GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/range.prefab");

		public override GameObject GetContent(RectTransform transform, IMenu menu) {
			var generated = base.GetContent(transform, menu);
			Behavior         = generated.AddComponent<FramerateBehavior>();
			Behavior.Handler = this;
			return generated;
		}

		public Framerate() {
			SetRange(VSync, MaxFramerate);
			SetStep(1f);
			SetValue(Value);
			SetLabelKey($"settings.entry.{string.Join(".", GetPath())}.label");
		}

		public static float Value {
			get => Config.Load().Get(GetConfigPath(), VSync);
			set {
				var v = Mathf.RoundToInt(value);

				var config = Config.Load();
				config.Set(GetConfigPath(), v);
				config.Save();

				switch (v) {
					case VSync:
						QualitySettings.vSyncCount  = 1;
						Application.targetFrameRate = -1;
						break;
					case Uncapped:
						QualitySettings.vSyncCount  = 0;
						Application.targetFrameRate = -1;
						break;
					default:
						QualitySettings.vSyncCount  = 0;
						Application.targetFrameRate = v;
						break;
				}
			}
		}

		public override void OnValueChanged(float value) {
			Value = value;
			var v = Mathf.RoundToInt(value);

			switch (v) {
				case VSync:
					SetValueKey("settings.entry.performances.framerate.option.vsync");
					break;
				case Uncapped:
					SetValueKey("settings.entry.performances.framerate.option.uncapped");
					break;
				default:
					SetValueKey("settings.range.value.framerate");
					break;
			}
		}
	}
}