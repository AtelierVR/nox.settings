using Nox.CCK.Settings;
using Nox.UI;
using Nox.UI.modals;
using UnityEngine;
using Nox.Settings.Clients;
using Nox.Settings.Runtime;

namespace Nox.Settings.Handlers {
	public sealed class ModalTesting : ButtonHandler {
		public override string[] GetPath()
			=> new[] { "debug", "modal" };

		protected override GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/button.prefab");

		public override GameObject GetContent(RectTransform transform, IMenu menu) {
			var go = base.GetContent(transform, menu);
			SetInteractable(menu is IModalMenu);
			return go;
		}

		public ModalTesting() {
			SetInteractable(true);
			SetLabel("settings.debug.modal");
			SetButtonText("settings.debug.modal.open");
		}

		public override void OnClick(IMenu menu) {
			var builder = Client.UiAPI.MakeModal(menu);
			if (builder == null) return;
			builder.SetTitle("value", "Test Modal");
			builder.SetClosable(true);
			builder.SetContent("value", "This is a test modal.\nYou can close it by clicking the close button or outside the modal.");
			var modal = builder.Build();
			modal.Show();
		}
	}
}