using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.Settings.Runtime;
using UnityEngine;
using UnityEngine.UI;
using Transform = UnityEngine.Transform;

namespace Nox.Settings.Clients {
	public class SettingsComponent : MonoBehaviour {
		public  Image                   labelIcon;
		public  RectTransform           content;
		public  GameObject              header;
		public  TextLanguage            title;
		private CancellationTokenSource _token;
		public  RectTransform           navigation;
		public  GameObject              leftContainer;
		public  TMPro.TMP_InputField    searchField;
		public  Button                  searchButton;
		public  Image                   searchImage;
		private CancellationTokenSource _searchToken;

		public SettingsPage Page;

		private readonly Dictionary<IHandler, GameObject> _handlerBoxes = new();
		private readonly Dictionary<GameObject, IHandler[]> _groupBoxes  = new();

		public static (GameObject, SettingsComponent) Generate(SettingsPage settingsPage, RectTransform parent) {
			var iconAsset      = Client.GetAsset<GameObject>("ui:prefabs/header_icon.prefab");
			var labelAsset     = Client.GetAsset<GameObject>("ui:prefabs/header_label.prefab");
			var withTitleAsset = Client.GetAsset<GameObject>("ui:prefabs/with_title.prefab");
			var listAsset      = Client.GetAsset<GameObject>("ui:prefabs/list.prefab");
			var scrollAsset    = Client.GetAsset<GameObject>("ui:prefabs/scroll.prefab");
			var containerAsset = Client.GetAsset<GameObject>("ui:prefabs/container.prefab");

			var content = Instantiate(Client.GetAsset<GameObject>("ui:prefabs/split.prefab"), parent);

			var component = content.AddComponent<SettingsComponent>();
			component.Page = settingsPage;
			content.name   = $"[{settingsPage.GetKey()}_{content.GetId()}]";

			var splitContent = Reference.GetComponent<RectTransform>("content", content);

			// left container
			component.leftContainer = Instantiate(containerAsset, splitContent);

			var withSearch = Instantiate(
				Client.GetAsset<GameObject>("ui:prefabs/with_search.prefab"),
				Reference.GetComponent<RectTransform>("content", component.leftContainer)
			);
			var searchHeader       = Reference.GetReference("header", withSearch);
			component.searchField  = Reference.GetComponent<TMPro.TMP_InputField>("input", searchHeader);
			component.searchButton = Reference.GetComponent<Button>("submit", searchHeader);
			component.searchImage  = component.searchButton
				? Reference.GetComponent<Image>("image", component.searchButton.gameObject)
				: null;

			if (component.searchImage)
				component.searchImage.sprite = Client.GetAsset<Sprite>("ui:icons/search.png");
			if (component.searchField) {
				component.searchField.onSubmit.AddListener(component.OnSearchSubmit);
				component.searchField.onValueChanged.AddListener(component.OnSearchChanged);
				var placeholder = component.searchField.placeholder
					? component.searchField.placeholder.GetComponent<TextLanguage>()
					: null;
				placeholder?.UpdateText("settings.search.placeholder");
				// No handler icon in the settings filter bar.
				var inputImageContainer = Reference.GetReference(
					"image_container",
					component.searchField.gameObject
				);
				inputImageContainer?.SetActive(false);
			}
			component.searchButton?.onClick.AddListener(component.OnSearchButton);

			var navigation = Instantiate(
				scrollAsset,
				Reference.GetComponent<RectTransform>("content", withSearch)
			);

			component.navigation = Reference.GetComponent<RectTransform>(
				"content", Instantiate(
					listAsset,
					Reference.GetComponent<RectTransform>("content", navigation)
				)
			);

			// container
			var container = Instantiate(Client.GetAsset<GameObject>("ui:prefabs/container_full.prefab"), splitContent);
			var withTitle = Instantiate(withTitleAsset, Reference.GetComponent<RectTransform>("content", container));

			component.header = Reference.GetReference("header", withTitle);
			var icon  = Instantiate(iconAsset, Reference.GetComponent<RectTransform>("before", component.header));
			var title = Instantiate(labelAsset, Reference.GetComponent<RectTransform>("content", component.header));

			component.labelIcon        = Reference.GetComponent<Image>("image", icon);
			component.title            = Reference.GetComponent<TextLanguage>("text", title);
			component.labelIcon.sprite = Client.GetAsset<Sprite>("ui:icons/globe.png");

			var contentDash = Reference.GetComponent<RectTransform>("content", withTitle);
			// setup scroll + list
			var scroll = Instantiate(scrollAsset, contentDash);
			var list   = Instantiate(listAsset, Reference.GetComponent<RectTransform>("content", scroll));
			component.content = Reference.GetComponent<RectTransform>("content", list);


			return (content, component);
		}


		public void UpdateTitles() {
			var settings = Page.GetCategory();
			if (settings == null) {
				title.UpdateText("settings.no_settings.title");
				return;
			}

			title.UpdateText(settings.GetTitle());
		}

		public async UniTask UpdateIcon() {
			var settings = Page.GetCategory();

			var sprite = settings != null
				? await settings.GetIcon()
				: null;

			labelIcon.sprite = sprite 
				?? await Client.GetAssetAsync<Sprite>("ui:icons/settings.png");
		}

		public async UniTask UpdateNavigation() {
			var btn = await Client.GetAssetAsync<GameObject>("ui:prefabs/btn_icon.prefab");

			var page = Page.GetCategories();

			foreach (Transform tf in navigation)
				Destroy(tf.gameObject);

			foreach (var p in page) {
				var o = Instantiate(btn, navigation);
				UpdateIcon(p, o).Forget();
				var text = Reference.GetComponent<TextLanguage>("text", o);
				text.UpdateText(p.GetLabel());

				var button = o.GetComponent<Button>();
				button.onClick.AddListener(() => OnChangePage(p.GetId()));

				o.name = $"{p.GetId()}_{o.GetId()}";
				o.SetActive(true);
			}

			UpdateLayout.UpdateImmediate(leftContainer);
		}

		private static async UniTask UpdateIcon(CategoryDetails category, GameObject o) {
			var image          = Reference.GetComponent<Image>("image", o);
			var imageContainer = Reference.GetComponent<RectTransform>("image_container", o);
			if (!image.sprite)
				imageContainer.gameObject.SetActive(false);
			var icon = await category.GetIcon();
			if (icon) {
				image.sprite = icon;
				imageContainer.gameObject.SetActive(true);
			} else imageContainer.gameObject.SetActive(false);

			UpdateLayout.UpdateImmediate(o);
		}

		private void OnHandlerValueChanged(IHandler _) {
			foreach (var (h, go) in _handlerBoxes)
				if (go)
					go.SetActive(h.IsActive);
			foreach (var (groupBox, handlers) in _groupBoxes)
				if (groupBox)
					groupBox.SetActive(handlers.Any(h => h.IsActive));
			UpdateLayout.UpdateImmediate(content);
		}

		private void OnDestroy() {
			Main.OnHandlerUpdated.RemoveListener(OnHandlerValueChanged);
			_token?.Cancel();
			_token?.Dispose();
			_token = null;
			_searchToken?.Cancel();
			_searchToken?.Dispose();
			_searchToken = null;
			if (searchField) {
				searchField.onSubmit.RemoveListener(OnSearchSubmit);
				searchField.onValueChanged.RemoveListener(OnSearchChanged);
			}
			if (searchButton)
				searchButton.onClick.RemoveListener(OnSearchButton);
		}

		/// <summary>Applied when the search field is submitted (Enter or the search button).</summary>
		public void OnSearchSubmit(string query)
			=> ApplyFilter(query);

		/// <summary>Applied when the search button is clicked.</summary>
		public void OnSearchButton()
			=> ApplyFilter(searchField ? searchField.text : null);

		private void OnSearchChanged(string query) {
			if (Page == null)
				return;
			// Debounce while typing so the list is not rebuilt on every keystroke.
			_searchToken?.Cancel();
			_searchToken?.Dispose();
			_searchToken = new CancellationTokenSource();
			DebounceFilter(query, _searchToken.Token).Forget();
		}

		private async UniTaskVoid DebounceFilter(string query, CancellationToken token) {
			var cancelled = await UniTask.Delay(200, ignoreTimeScale: true, cancellationToken: token)
				.SuppressCancellationThrow();
			if (cancelled || Page == null)
				return;
			ApplyFilter(query);
		}

		private void ApplyFilter(string query) {
			if (Page == null)
				return;
			query = (query ?? string.Empty).Trim();
			if (Page.Filter == query)
				return;
			Page.Filter = query;
			Page.OnRefresh();
		}

		internal async UniTask UpdateContent() {
			_token?.Cancel();
			_token?.Dispose();
			_token = new CancellationTokenSource();
			var token = _token.Token;

			if (!content) return;

			var box  = Client.GetAsset<GameObject>("ui:prefabs/box.prefab");
			var list = Client.GetAsset<GameObject>("ui:prefabs/list.prefab");

			Main.OnHandlerUpdated.RemoveListener(OnHandlerValueChanged);
			_handlerBoxes.Clear();
			_groupBoxes.Clear();

			foreach (Transform tf in content)
				Destroy(tf.gameObject);

			var details = Page.GetCategory();
			if (details == null) {
				Debug.LogWarning("No category selected.");
				return;
			}

			var groups = Page.GetGroups(details.GetId());

			foreach (var group in groups) {
				token.ThrowIfCancellationRequested();

				var groupBox = await box.InstantiateAsync(content, cancellationToken: token);
				if (!groupBox) continue;
				groupBox.transform.localPosition = Vector3.zero;
				groupBox.transform.localRotation = Quaternion.identity;
				groupBox.transform.localScale = Vector3.one;

				var cont = Reference.GetComponent<RectTransform>("content", groupBox);
				var text = Reference.GetComponent<TextLanguage>("text", groupBox);
				text.UpdateText(group.GetLabel());
				var listBox = await list.InstantiateAsync(cont, cancellationToken: token);
				if (!listBox) continue;
				listBox.transform.localPosition = Vector3.zero;
				listBox.transform.localRotation = Quaternion.identity;
				listBox.transform.localScale = Vector3.one;

				cont = Reference.GetComponent<RectTransform>("content", listBox);
				var menu = Page.GetMenu();
				foreach (var handler in group.Handlers) {
					token.ThrowIfCancellationRequested();

					var handlerBox = await handler.GetContentAsync(cont, menu)
						?? handler.GetContent(cont, menu);
					token.ThrowIfCancellationRequested();
					if (!handlerBox) {
						Debug.LogWarning($"Handler {handler.ToID()} does not have content.");
						continue;
					}

					handlerBox.name = $"{handler.Path.LastOrDefault()}_{handlerBox.GetId()}";
					_handlerBoxes[handler] = handlerBox;
					handlerBox.SetActive(handler.IsActive);
					handlerBox.transform.localPosition = Vector3.zero;
					handlerBox.transform.localRotation = Quaternion.identity;
					handlerBox.transform.localScale = Vector3.one;
				}

				token.ThrowIfCancellationRequested();
				_groupBoxes[groupBox] = group.Handlers;
				groupBox.SetActive(group.Handlers.Any(h => h.IsActive));
			}

			token.ThrowIfCancellationRequested();
			UpdateLayout.UpdateImmediate(content);
			Main.OnHandlerUpdated.AddListener(OnHandlerValueChanged);
		}


		private void OnChangePage(string page)
			=> Page.SetCurrent(page);
	}
}