using Nox.CCK.Settings;
using Nox.CCK.Utils;
using Nox.Settings.Runtime;
using UnityEngine;

namespace Nox.Settings.Handlers {
	/// <summary>
	/// Shows the FPS readout of the menu zones (see <c>Nox.UI.FpsCounter</c>): the top-right zone is where
	/// the system overlays live, the ones a taskbar or an Android status bar would show there.
	/// <para>
	/// The value is read back from the config on every update, so a change made outside the UI (terminal,
	/// config file) is reflected in the row instead of being overwritten by a stale one.
	/// </para>
	/// </summary>
	public class FpsCounterDisplay : ToggleHandler {
		public override string[] Path
			=> new[] { "performances", "fps_counter" };

		public override int Order
			=> 20100;

		private static string[] GetConfigPath()
			=> new[] { "settings", "ui", "fps_counter" };

		public static new bool Value {
			get => Config.Load().Get(GetConfigPath(), false);
			set {
				var config = Config.Load();
				if (config.Get(GetConfigPath(), false) == value)
					return;
				config.Set(GetConfigPath(), value);
				config.Save();
			}
		}

		protected override GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("settings:prefabs/toggle.prefab");

		public FpsCounterDisplay() {
			SetLabelKey($"settings.entry.{string.Join(".", Path)}.label");
			SetValue(Value, notify: false);
		}

		protected override void OnValueChanged(bool value)
			=> Value = value;

		public override void OnUpdated(IHandler handler) {
			if (handler == this)
				return;
			SetValue(Value, notify: false);
		}
	}
}
