using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.Settings;
using Nox.UI;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Nox.CCK.Settings {
	public abstract class ToggleHandler : IHandler {
		public abstract string[] GetPath();

		public virtual bool IsActive()
			=> true;

		public virtual void OnUpdated(IHandler handler) { }

		public virtual int GetOrder() => 0;

		public virtual int CompareTo(IHandler other)
			=> GetOrder().CompareTo(other.GetOrder());

		private Toggle _toggle;
		private TextLanguage _textLabel;
		private bool _value;
		private string _keyLabel;

		abstract protected GameObject GetPrefab();

		public virtual GameObject GetContent(RectTransform transform, IMenu menu) {
			var asset   = GetPrefab();
			var go      = asset.Instantiate(transform);
			var destroy = go.GetOrAddComponent<DestroyComponent>();
			destroy.Destroyed.AddListener(OnDestroy);
			_toggle = Reference.GetComponent<Toggle>("toggle", go);
			_toggle.onValueChanged.AddListener(OnInternalValueChanged);
			_textLabel = Reference.GetComponent<TextLanguage>("label", go);
			UpdateToggle();
			SetLabelKey(_keyLabel);
			return go;
		}

		private void OnInternalValueChanged(bool value) {
			_value = value;
			OnValueChanged(value);
			SettingsNotifier.NotifyUpdated(this);
		}

		public void SetLabelKey(string key) {
			_keyLabel = key;
			if (_textLabel)
				_textLabel.UpdateText(key);
		}

		abstract protected void OnValueChanged(bool value);

		public UniTask<GameObject> GetContentAsync(RectTransform transform, IMenu menu)
			=> UniTask.FromResult(GetContent(transform, menu));

		private void UpdateToggle() {
			if (!_toggle)
				return;
			_toggle.SetIsOnWithoutNotify(_value);
		}

		virtual protected void OnDestroy() {
			if (_toggle)
				_toggle.onValueChanged.RemoveListener(OnInternalValueChanged);
			_toggle = null;
		}

		public virtual void SetValue(bool value, bool notify = true) {
			_value = value;
			UpdateToggle();
			if (notify)
				OnValueChanged(value);
		}
	}
}