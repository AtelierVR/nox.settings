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
		private CancellationTokenSource _thumbnailTokenSource;
		public  RectTransform           navigation;
		public  GameObject              leftContainer;

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
			content.name   = $"[{settingsPage.GetKey()}_{content.GetEntityId().GetHashCode()}]";

			var splitContent = Reference.GetComponent<RectTransform>("content", content);

			// left container

			component.leftContainer = Instantiate(containerAsset, splitContent);

			var navigation = Instantiate(
				scrollAsset,
				Reference.GetComponent<RectTransform>("content", component.leftContainer)
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

			var page = Main.Handlers
				.Select(hand => hand.GetPath().FirstOrDefault())
				.Distinct()
				.Select(p => Page.GetCategory(p));

			foreach (Transform tf in navigation)
				Destroy(tf.gameObject);

			foreach (var p in page) {
				var o = Instantiate(btn, navigation);
				UpdateIcon(p, o).Forget();
				var text = Reference.GetComponent<TextLanguage>("text", o);
				text.UpdateText(p.GetLabel());

				var button = o.GetComponent<Button>();
				button.onClick.AddListener(() => OnChangePage(p.GetId()));

				o.name = $"{p.GetId()}_{o.GetEntityId().GetHashCode()}";
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
			foreach (var (h, go) in _handlerBoxes) {
				if (go)
					go.SetActive(h.IsActive());
			}
			foreach (var (groupBox, handlers) in _groupBoxes) {
				if (groupBox)
					groupBox.SetActive(handlers.Any(h => h.IsActive()));
			}
			UpdateLayout.UpdateImmediate(content);
		}

		private void OnDestroy()
			=> Main.OnHandlerUpdated.RemoveListener(OnHandlerValueChanged);

		internal async UniTask UpdateContent() {
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
				var groupBox = (await InstantiateAsync(box, content)).FirstOrDefault();
				if (!groupBox) continue;
				groupBox.transform.localPosition = Vector3.zero;
				groupBox.transform.localRotation = Quaternion.identity;
				groupBox.transform.localScale = Vector3.one;
				
				var cont = Reference.GetComponent<RectTransform>("content", groupBox);
				var text = Reference.GetComponent<TextLanguage>("text", groupBox);
				text.UpdateText(group.GetLabel());
				var listBox = (await InstantiateAsync(list, cont)).FirstOrDefault();
				if (!listBox) continue;
				listBox.transform.localPosition = Vector3.zero;
				listBox.transform.localRotation = Quaternion.identity;
				listBox.transform.localScale = Vector3.one;
				
				cont = Reference.GetComponent<RectTransform>("content", listBox);
				var menu = Page.GetMenu();
				foreach (var handler in group.Handlers) {
					var handlerBox = await handler.GetContentAsync(cont, menu) 
						?? handler.GetContent(cont, menu);
					if (!handlerBox) {
						Debug.LogWarning($"Handler {handler.ToID()} does not have content.");
						continue;
					}

					handlerBox.name = $"{handler.GetPath().LastOrDefault()}_{handlerBox.GetEntityId().GetHashCode()}";
					_handlerBoxes[handler] = handlerBox;
					handlerBox.SetActive(handler.IsActive());
					handlerBox.transform.localPosition = Vector3.zero;
					handlerBox.transform.localRotation = Quaternion.identity;
					handlerBox.transform.localScale = Vector3.one;
				}

				_groupBoxes[groupBox] = group.Handlers;
				groupBox.SetActive(group.Handlers.Any(h => h.IsActive()));
			}

			UpdateLayout.UpdateImmediate(content);
			Main.OnHandlerUpdated.AddListener(OnHandlerValueChanged);
		}


		private void OnChangePage(string page)
			=> Page.SetCurrent(page);
	}
}