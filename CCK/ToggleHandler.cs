using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Theme;
using Nox.CCK.Utils;
using Nox.Settings;
using Nox.UI;
using UnityEngine;

namespace Nox.CCK.Settings {
	public abstract class ToggleHandler : IHandler {
		public abstract string[] Path { get; }

		public virtual bool IsActive
			=> true;

		/// <summary>See <see cref="IHandler.IsEditable"/>.</summary>
		public virtual bool IsEditable
			=> _interactable;

		public virtual void OnUpdated(IHandler handler) { }

		public virtual int Order => 0;

		public virtual int CompareTo(IHandler other)
			=> Order.CompareTo(other.Order);

		private ToggleField _field;
		private bool _value;
		private string _keyLabel;
		private bool _interactable = true;

		public bool Value => _value;
		object IHandler.Value => Value;
		abstract protected GameObject GetPrefab();

		public virtual GameObject GetContent(RectTransform transform, IMenu menu) {
			var asset   = GetPrefab();
			var go      = asset.Instantiate(transform);
			var destroy = go.GetOrAddComponent<DestroyComponent>();
			destroy.Destroyed.AddListener(OnDestroy);

			_field = go.GetOrAddComponent<ToggleField>();
			_field.OnValueChanged.AddListener(OnInternalValueChanged);
			UpdateToggle();
			SetLabelKey(_keyLabel);
			RefreshEditable();

			ThemeManager.Apply(go);
			return go;
		}

		private void OnInternalValueChanged(bool value) {
			_value = value;
			OnValueChanged(value);
			SettingsNotifier.NotifyUpdated(this);
		}

		public void SetLabelKey(string key) {
			_keyLabel = key;
			_field?.SetLabelKey(key);
		}

		virtual protected void SetInteractable(bool interactable) {
			_interactable = interactable;
			RefreshEditable();
		}

		/// <summary>Re-applies <see cref="IsEditable"/> on the widget, after the availability changed.</summary>
		protected void RefreshEditable() {
			if (_field && _field.toggle)
				_field.toggle.interactable = IsEditable;
		}

		abstract protected void OnValueChanged(bool value);

		public UniTask<GameObject> GetContentAsync(RectTransform transform, IMenu menu, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			return UniTask.FromResult(GetContent(transform, menu));
		}

		private void UpdateToggle()
			=> _field?.SetValue(_value);

		virtual protected void OnDestroy() {
			_field?.OnValueChanged.RemoveListener(OnInternalValueChanged);
			_field = null;
		}

		public virtual void SetValue(bool value, bool notify = true) {
			_value = value;
			UpdateToggle();
			if (notify)
				OnValueChanged(value);
		}

		/// <summary>See <see cref="IHandler.Apply"/>.</summary>
		public bool Apply(object value) {
			if (value == null)
				return false;

			try {
				SetValue(System.Convert.ToBoolean(value));
				return true;
			} catch (System.Exception) {
				return false;
			}
		}

		/// <summary>A toggle only takes two values.</summary>
		public string[] Suggestions
			=> new[] { "true", "false" };
	}
}
