using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Theme;
using Nox.CCK.Utils;
using Nox.Settings;
using Nox.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Nox.CCK.Settings {
	public abstract class ButtonHandler : IHandler {
		public abstract string[] Path { get; }

		public virtual object Value => null;

		public virtual bool IsTriggerable => true;

		public abstract void OnClick(IContext context);

		public virtual bool IsActive
			=> true;

		/// <summary>
		/// See <see cref="IHandler.Apply"/>. Declared here (and not only in the derived handlers) on
		/// purpose: the interface is implemented by this class, so its members are mapped against this
		/// class. A method added by a derived handler without an override here never implements
		/// <see cref="IHandler.Apply"/>, and the call falls back to the interface default (false).
		/// </summary>
		public virtual bool Apply(object value)
			=> false;

		/// <summary>
		/// See <see cref="IHandler.Add"/>. Declared here for the same reason as <see cref="Apply"/>: a derived
		/// handler that overrides it only implements the interface through this class.
		/// </summary>
		public virtual bool Add(object value)
			=> false;

		/// <summary>See <see cref="IHandler.Remove"/>, declared here for the same reason as <see cref="Apply"/>.</summary>
		public virtual bool Remove(object value)
			=> false;

		/// <summary>
		/// See <see cref="IHandler.Suggestions"/>, declared here for the same reason as <see cref="Apply"/>:
		/// a button has no enumerated values unless a derived handler says so.
		/// </summary>
		public virtual string[] Suggestions
			=> Array.Empty<string>();

		public virtual void OnUpdated(IHandler handler) { }

		public virtual int Order => 0;

		public virtual int CompareTo(IHandler other)
			=> Order.CompareTo(other.Order);

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

			ThemeManager.Apply(go);
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

		public virtual UniTask<GameObject> GetContentAsync(RectTransform transform, IMenu menu, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			return UniTask.FromResult(GetContent(transform, menu));
		}

		virtual protected void SetInteractable(bool interactable) {
			_interactable = interactable;
			if (!_button)
				return;
			_button.interactable = interactable;
		}
	}
}
