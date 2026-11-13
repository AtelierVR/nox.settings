using System.Collections.Generic;
using Nox.CCK.Settings;
using Nox.CCK.Theme;
using Nox.CCK.Utils;
using Nox.Settings.Clients;
using Nox.Settings.Runtime;
using Nox.UI;
using Nox.UI.modals;
using UnityEngine;

namespace Nox.Settings.Handlers {
	/// <summary>
	/// Theme of the whole UI of the session: every panel marked as themed follows it.
	/// <para>
	/// It mirrors <see cref="ThemeManager"/> the way the language entry mirrors the language
	/// manager: the value is decided here whatever the source (dropdown, terminal, config) and the manager
	/// repaints the open panels.
	/// </para>
	/// </summary>
	public sealed class Theme : DropdownHandler {
		public override string[] Path
			=> new[] { "accessibility", "interface", "theme" };

		public override int Order
			=> 10001;

		protected override GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/dropdown.prefab");

		protected override IModalBuilder GetModalBuilder(IMenu menu)
			=> Client.UiAPI.MakeModal(menu);

		public Theme() {
			EnsureOptions();
			SetLabel($"settings.entry.{string.Join(".", Path)}.label");
			Value = Config.Load().Get(ConfigKey, Value);
			SetValue(ThemeManager.CurrentTheme, false);
		}

		/// <summary>
		/// The value is applied and stored here: this is the only place where the theme is decided, whatever
		/// the source (dropdown, terminal, config).
		/// </summary>
		protected override void OnValueChanged(string value)
			=> Value = value;

		/// <summary>Same path as the other settings, so the terminal and the file agree.</summary>
		private string ConfigKey
			=> $"settings.{string.Join(".", Path)}";

		private new string Value {
			get => ThemeManager.CurrentTheme;
			set {
				ThemeManager.CurrentTheme = value;
				var config = Config.Load();
				config.Set(ConfigKey, value);
				config.Save();
			}
		}

		/// <summary>
		/// Ids of the themes of every registered pack, with the language key of their name: the key is given to
		/// the text as it is and resolved there, like the language entry does, so a translation added later is
		/// picked up without rebuilding the options.
		/// </summary>
		private void EnsureOptions() {
			var options = new Dictionary<string, string[]>();
			foreach (var (id, key) in ThemeManager.GetAvailableThemes())
				options[id] = new[] { key };
			SetOptions(options);
			SetValue(ThemeManager.CurrentTheme, false);
		}
	}
}
