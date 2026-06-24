using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.Settings;
using Nox.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Nox.CCK.Settings {
	public abstract class RangeHandler : IHandler {
		public abstract string[] GetPath();

		public virtual bool IsActive()
			=> true;

		public virtual void OnUpdated(IHandler handler) { }

		public virtual int GetOrder() => 0;

		public virtual int CompareTo(IHandler other)
			=> GetOrder().CompareTo(other.GetOrder());

		private Slider _range;
		private TextLanguage _textLabel;
		private TextLanguage _textValue;
		private TextLanguage _textType;
		private float _min;
		private float _max;
		private float _value;
		private float _step;
		private string _keyLabel;
		private string _keyType = "settings.range.type.empty";
		private string _keyValue = "settings.range.value.float";

		private float Min
			=> Mathf.Ceil(_min / Step) * Step;

		private float Max
			=> Mathf.Floor(_max / Step) * Step;

		public float Value
			=> Mathf.Round(Mathf.Clamp(_value, Min, Max) / Step) * Step;

		object IHandler.Value => Value;

		private float Step
			=> _step > 0 ? _step : float.Epsilon;

		abstract protected GameObject GetPrefab();

		public virtual GameObject GetContent(RectTransform transform, IMenu menu) {
			var asset   = GetPrefab();
			var go      = asset.Instantiate(transform);
			var destroy = go.GetOrAddComponent<DestroyComponent>();
			destroy.Destroyed.AddListener(OnDestroy);
			_range = Reference.GetComponent<Slider>("range", go);
			_textLabel = Reference.GetComponent<TextLanguage>("label", go);
			_textValue = Reference.GetComponent<TextLanguage>("value", go);
			_textType = Reference.GetComponent<TextLanguage>("type", go);
			UpdateSlider();
			UpdateValue();
			SetLabelKey(_keyLabel);
			SetTypeKey(_keyType);
			SetValueKey(_keyValue);
			_range.onValueChanged.AddListener(OnInternalValueChanged);
			return go;
		}

		private void OnInternalValueChanged(float value) {
			if (Mathf.Approximately(_value, value)) return;
			_value = value;
			UpdateValue();
			OnValueChanged(Value);
			SettingsNotifier.NotifyUpdated(this);
		}

		protected void SetLabelKey(string key) {
			_keyLabel = key;
			if (_textLabel)
				_textLabel.UpdateText(key);
		}

		protected void SetTypeKey(string key, params string[] @params) {
			_keyType = key;
			if (_textType)
				_textType.UpdateText(key, @params);
		}

		protected void SetValueKey(string key) {
			_keyValue = key;
			if (_textValue)
				_textValue.UpdateText(key);
		}

		virtual protected void OnDestroy() {
			_range.onValueChanged.RemoveListener(OnInternalValueChanged);
			_range = null;
			_textLabel = null;
			_textValue = null;
			_textType = null;
		}


		abstract protected void OnValueChanged(float value);

		public UniTask<GameObject> GetContentAsync(RectTransform transform, IMenu menu)
			=> UniTask.FromResult(GetContent(transform, menu));

		private void UpdateSlider() {
			if (!_range) return;
			_range.minValue = Min;
			_range.maxValue = Max;
			_range.wholeNumbers = _step % 1 == 0;
			_range.SetValueWithoutNotify(Value);
		}

		private void UpdateValue() {
			if (!_textValue) return;
			_textValue.UpdateText(
				new[] {
					Value.ToString("0"),
					Value.ToString("0.00"),
					(Value * 100).ToString("0"),
					(Value * 100).ToString("0.00"),
					string.Concat(BitConverter.GetBytes(Value).Select(b => Convert.ToString(b, 2).PadLeft(8, '0'))),
					string.Concat(BitConverter.GetBytes(Value).Select(b => b.ToString("X2"))),
				}
			);
		}

		virtual protected void SetStep(float step) {
			_step = step;
			UpdateSlider();
		}

		virtual protected void SetRange(float min, float max) {
			_min = min;
			_max = max;
			UpdateSlider();
		}

		virtual protected void SetValue(float value, bool notify = true) {
			_value = value;
			UpdateSlider();
			UpdateValue();
			if (notify)
				OnValueChanged(Value);
		}
	}
}