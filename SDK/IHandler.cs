using System;
using Cysharp.Threading.Tasks;
using Nox.UI;
using UnityEngine;

namespace Nox.Settings {
	/// <summary>
	/// Interface for settings handlers,
	/// which can be used to create UI elements
	/// and handle user interactions.
	/// </summary>
	public interface IHandler : IComparable<IHandler> {
		/// <summary>
		/// Path of the handler in the settings hierarchy.
		/// </summary>
		public string[] Path { get; }

		/// <summary>
		/// Get the value of the handler.
		/// </summary>
		public object Value { get; }

		/// <summary>
		/// Whether the handler is active.
		/// </summary>
		public bool IsActive { get; }

		/// <summary>
		/// Order of the handler for sorting.
		/// </summary>
		public int Order { get; }
		/// <summary>
		/// Check if the handler can be triggered by a command.
		/// </summary>
		public bool IsTriggerable => false;

		/// <summary>
		/// Handle a click event on the handler.
		/// </summary>
		public void OnClick(IContext context) { }

		/// <summary>
		/// Get the content of the handler as a GameObject.
		/// </summary>
		public GameObject GetContent(RectTransform transform, IMenu menu);

		/// <summary>
		/// Get the content of the handler as a GameObject asynchronously.
		/// </summary>
		public UniTask<GameObject> GetContentAsync(RectTransform transform, IMenu menu);

		/// <summary>
		/// Handle updates to the handler.
		/// </summary>
		public void OnUpdated(IHandler handler);
	}
}