using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Language;
using Nox.CCK.Settings;
using Nox.CCK.Utils;
using Nox.UI;
using Nox.UI.modals;
using UnityEngine;
using Nox.Settings.Clients;
using Nox.Settings.Runtime;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Settings.Handlers {
	public sealed class Language : DropdownHandler, IDisposable {
		public override string[] Path
			=> new[] { "accessibility", "interface", "language" };

		public override int Order => 10000;

		protected override GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/dropdown.prefab");

		protected override IModalBuilder GetModalBuilder(IMenu menu)
			=> Client.UiAPI.MakeModal(menu);

		public Language() {
			LanguageManager.OnPackListUpdated.AddListener(OnPacksUpdated);
			LanguageManager.OnLanguageChanged.AddListener(OnLanguageChanged);
			OnPacksUpdated();
			SetLabel($"settings.entry.{string.Join(".", Path)}.label");
			Value = Config.Load().Get(ConfigKey, Value);
			SetValue(LanguageManager.CurrentLanguage, false);
		}

		/// <summary>
		/// The value is applied and stored here: this is the only place where the language is decided,
		/// whatever the source (dropdown, terminal, config).
		/// </summary>
		protected override void OnValueChanged(string value)
			=> Value = value;

		/// <summary>Same path as the other settings, so the terminal and the file agree.</summary>
		private string ConfigKey
			=> $"settings.{string.Join(".", Path)}";

		private new string Value {
			get => LanguageManager.CurrentLanguage;
			set {
				LanguageManager.CurrentLanguage = value;
				var config = Config.Load();
				config.Set(ConfigKey, value);
				config.Save();
			}
		}

		private void OnPacksUpdated() {
			var langs = LanguageManager.GetAvailableLanguages();
			var res   = new Dictionary<string, string[]>();

			foreach (var lang in langs) {
				var name = LanguageManager.Get(lang, "language");
				if (string.IsNullOrEmpty(name))
					name = lang;
				res.Add(lang, new[] { "value", name });
			}

			SetOptions(res);
			SetValue(LanguageManager.CurrentLanguage, false);
		}

		private void OnLanguageChanged(string lang)
			=> SetValue(lang, false);


		public void Dispose() {
			LanguageManager.OnPackListUpdated.RemoveListener(OnPacksUpdated);
			LanguageManager.OnLanguageChanged.RemoveListener(OnLanguageChanged);
		}
	}
}