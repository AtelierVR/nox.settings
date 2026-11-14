using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Theme;
using Nox.CCK.Utils;
using Nox.Settings;
using Nox.UI;
using UnityEngine;

namespace Nox.CCK.Settings {
	/// <summary>
	/// Read-only row showing a text, used to surface information in the settings page (state, counters...).
	/// <para>
	/// The prefab carries a <c>text</c> reference to the label it fills; the handler never stores a value and
	/// cannot be interacted with. The derived handler owns the content of the text and refreshes it from
	/// <see cref="SetText"/>.
	/// </para>
	/// </summary>
	public abstract class TextHandler : IHandler {
		public abstract string[] Path { get; }

		public virtual object Value => null;

		public virtual bool IsActive
			=> true;

		public virtual bool IsTriggerable
			=> false;

		public virtual int Order
			=> 0;

		public virtual int CompareTo(IHandler other)
			=> Order.CompareTo(other.Order);

		public virtual void OnUpdated(IHandler handler) { }

		public virtual string[] Suggestions
			=> Array.Empty<string>();

		public virtual bool Apply(object value)
			=> false;

		private TextLanguage _text;
		private string _key = string.Empty;
		private string[] _args = Array.Empty<string>();

		abstract protected GameObject GetPrefab();

		public virtual GameObject GetContent(RectTransform transform, IMenu menu) {
			var asset   = GetPrefab();
			var go      = asset.Instantiate(transform);
			var destroy = go.GetOrAddComponent<DestroyComponent>();
			destroy.Destroyed.AddListener(OnDestroy);

			_text = Reference.GetComponent<TextLanguage>("text", go);
			UpdateText();

			ThemeManager.Apply(go);
			return go;
		}

		public UniTask<GameObject> GetContentAsync(RectTransform transform, IMenu menu, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			return UniTask.FromResult(GetContent(transform, menu));
		}

		/// <summary>Sets the localized text shown by the row (applied immediately when the row is built).</summary>
		protected void SetText(string key, params string[] args) {
			_key  = key ?? string.Empty;
			_args = args ?? Array.Empty<string>();
			UpdateText();
		}

		private void UpdateText() {
			if (_text && !string.IsNullOrEmpty(_key))
				_text.UpdateText(_key, _args);
		}

		virtual protected void OnDestroy()
			=> _text = null;
	}
}
