using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.Settings;
using Nox.UI;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Nox.CCK.Settings {
	public abstract class ButtonHandler : IHandler {
		public abstract string[] GetPath();

		public virtual object Value => null;

		public virtual bool IsTriggerable => true;

		public abstract void OnClick(IContext context);

		public virtual bool IsActive()
			=> true;

		public virtual void OnUpdated(IHandler handler) { }

		public virtual int GetOrder() => 0;

		public virtual int CompareTo(IHandler other)
			=> GetOrder().CompareTo(other.GetOrder());

		protected Button _button;
		private TextLanguage _textLabel;
		private string[] _keyLabel;
		private string[] _keyButtonText;
		protected bool _interactable = true;


		private TextLanguage _buttonText;

		abstract protected GameObject GetPrefab();

		public virtual GameObject GetContent(RectTransform transform, IMenu menu) {
			var asset = GetPrefab();
			var go    = asset.Instantiate(transform);
			var destroy = go.GetOrAddComponent<DestroyComponent>();
			destroy.Destroyed.AddListener(OnDestroy);
			_button     = Reference.GetComponent<Button>("button", go);
			_textLabel  = Reference.GetComponent<TextLanguage>("label", go);
			_buttonText = Reference.GetComponent<TextLanguage>("button_text", go);

			if (_button)
				_button.onClick.AddListener(() => OnClick(new Context().Set("menu", menu)));

			if (_keyLabel != null)
				SetLabel(_keyLabel[0], _keyLabel.Skip(1).ToArray());
			else
				SetLabel(null);

			if (_keyButtonText != null)
				SetButtonText(_keyButtonText[0], _keyButtonText.Skip(1).ToArray());
			else
				SetButtonText(null);

			SetInteractable(_interactable);
			return go;
		}

		protected void SetLabel(string key, params string[] @params) {
			key       ??= "label.default";
			@params   ??= Array.Empty<string>();
			_keyLabel =   new[] { key }.Concat(@params).ToArray();
			if (_textLabel)
				_textLabel.UpdateText(key);
		}

		protected void SetButtonText(string key, params string[] @params) {
			key            ??= "button.default";
			@params        ??= Array.Empty<string>();
			_keyButtonText =   new[] { key }.Concat(@params).ToArray();
			if (_buttonText)
				_buttonText.UpdateText(key, @params);
		}

		virtual protected void OnDestroy() {
			if (_button)
				_button.onClick.RemoveAllListeners();
			_button = null;
		}

		public virtual UniTask<GameObject> GetContentAsync(RectTransform transform, IMenu menu)
			=> UniTask.FromResult(GetContent(transform, menu));

		virtual protected void SetInteractable(bool interactable) {
			_interactable = interactable;
			if (!_button)
				return;
			_button.interactable = interactable;
		}
	}
}