using System;
using System.Threading;
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
		/// Applies a value coming from outside the UI (terminal, config...), so the change takes effect
		/// instead of only being stored. Returns false when the handler cannot take a value that way.
		/// </summary>
		public bool Apply(object value) => false;

		/// <summary>
		/// Adds elements to a handler that holds <b>several values at once</b>, so the rest of its value is
		/// kept. Returns false when the handler holds a single value.
		/// </summary>
		/// <param name="value">Elements to add, space separated.</param>
		public bool Add(object value) => false;

		/// <summary>
		/// Removes elements from a handler that holds <b>several values at once</b>, leaving the other ones
		/// untouched. Returns false when the handler holds a single value.
		/// </summary>
		/// <param name="value">Elements to remove, space separated.</param>
		public bool Remove(object value) => false;

		/// <summary>
		/// Values this handler accepts, for completion (<c>settings set &lt;path&gt; ...</c>). Empty when the value
		/// is free (a range, a text): there is nothing to enumerate.
		/// </summary>
		public string[] Suggestions => Array.Empty<string>();

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
		/// <param name="transform"></param>
		/// <param name="menu"></param>
		/// <param name="cancellationToken"></param>
		public UniTask<GameObject> GetContentAsync(RectTransform transform, IMenu menu, CancellationToken cancellationToken = default);

		/// <summary>
		/// Handle updates to the handler.
		/// </summary>
		public void OnUpdated(IHandler handler);
	}
}
