using System.Linq;
using Nox.CCK.Settings;
using Nox.CCK.Utils;
using Nox.UI;
using Nox.UI.modals;
using UnityEngine;
using Nox.Settings.Clients;
using Nox.Settings.Runtime;

namespace Nox.Settings.Handlers {
	public sealed class Quality : DropdownHandler {
		public override string[] GetPath()
			=> new[] { "graphic", "quality" };

		private static string[] GetConfigPath()
			=> new[] { "settings", "graphic", "quality" };

		protected override GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/dropdown.prefab");

		protected override IModalBuilder GetModalBuilder(IMenu menu)
			=> Client.UiAPI.MakeModal(menu);

		public Quality() {
			SetLabel($"settings.entry.{string.Join(".", GetPath())}.label");
			var res = QualitySettings.names
				.ToDictionary(quality => quality, quality => new[] { "value", quality });
			SetOptions(res);
			Value = Config.Load().Get(GetConfigPath(), Value);
			SetValue(QualitySettings.names[Value], false);
		}

		protected override void OnValueChanged(string value) {
			var index = System.Array.IndexOf(QualitySettings.names, value);
			if (index < 0) return;
			Value = index;
		}

		private static int Value {
			get => QualitySettings.GetQualityLevel();
			set {
				QualitySettings.SetQualityLevel(value);
				var config = Config.Load();
				config.Set(GetConfigPath(), value);
				config.Save();
			}
		}
	}
}