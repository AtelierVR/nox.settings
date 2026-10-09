using System;
using System.Collections.Generic;
using Nox.CCK.Settings;
using Nox.CCK.Utils;
using Nox.Settings.Clients;
using Nox.Settings.Runtime;
using Nox.UI;
using Nox.UI.modals;
using UnityEngine;

namespace Nox.Settings.Handlers {
	public sealed class SkinWeights : DropdownHandler {
		/// <summary>
		/// Values actually accepted by <see cref="QualitySettings.skinWeights"/>: the enum has a
		/// <c>None</c> (0) entry that Unity's setter rejects (ArgumentException), so it is excluded from the options.
		/// </summary>
		private static readonly UnityEngine.SkinWeights[] ValidValues = BuildValidValues();

		private static UnityEngine.SkinWeights[] BuildValidValues() {
			var list = new List<UnityEngine.SkinWeights>();
			foreach (UnityEngine.SkinWeights value in Enum.GetValues(typeof(UnityEngine.SkinWeights)))
				if (value != 0)
					list.Add(value);
			return list.ToArray();
		}

		public override string[] Path
			=> new[] { "performances", "skin_weights" };

		public override int Order => 20003;

		private static string[] GetConfigPath()
			=> new[] { "settings", "performances", "skin_weights" };

		protected override GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/dropdown.prefab");

		protected override IModalBuilder GetModalBuilder(IMenu menu)
			=> Client.UiAPI.MakeModal(menu);

		private static string KeyOf(UnityEngine.SkinWeights value)
			=> value.ToString().ToSnakeCase();

		private static Dictionary<string, string[]> BuildOptions() {
			var dict = new Dictionary<string, string[]>();
			foreach (var value in ValidValues)
				dict[KeyOf(value)] = new[] { $"settings.entry.performances.skin_weights.option.{KeyOf(value)}" };
			return dict;
		}

		public SkinWeights() {
			SetLabel($"settings.entry.{string.Join(".", Path)}.label");
			SetOptions(BuildOptions());
			var saved = Config.Load().Get(GetConfigPath(), (int)CurrentValue);
			CurrentValue = Normalize((UnityEngine.SkinWeights)saved);
			SetValue(KeyOf(CurrentValue), false);
		}

		protected override void OnValueChanged(string value) {
			foreach (var skinWeights in ValidValues) {
				if (KeyOf(skinWeights) != value) continue;
				CurrentValue = skinWeights;
				return;
			}
		}

		/// <summary>None (0) and any unknown value are not accepted by Unity, so it falls back to 4 bones.</summary>
		private static UnityEngine.SkinWeights Normalize(UnityEngine.SkinWeights value)
			=> Array.IndexOf(ValidValues, value) >= 0 ? value : UnityEngine.SkinWeights.FourBones;

		private static UnityEngine.SkinWeights CurrentValue {
			get => Normalize(QualitySettings.skinWeights);
			set {
				QualitySettings.skinWeights = value;
				var config = Config.Load();
				config.Set(GetConfigPath(), (int)value);
				config.Save();
			}
		}
	}
}
