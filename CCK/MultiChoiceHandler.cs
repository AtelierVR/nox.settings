using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Language;
using Nox.Settings;
using Nox.UI;
using Nox.UI.modals;
using UnityEngine;

namespace Nox.CCK.Settings {
	/// <summary>
	/// A setting that holds <b>several values at once</b>: the button opens a list of switches the player turns
	/// on and off, and the list stays open while its rows change, so the whole group is set in one go.
	/// <para>
	/// The value is the keys of the rows that are on, joined by spaces, which is what the config stores and
	/// what <see cref="Apply"/> accepts back. A derived handler only declares its choices, the prefab of a
	/// row and what to do with them (<see cref="OnSelectionChanged"/>).
	/// </para>
	/// </summary>
	public abstract class MultiChoiceHandler : ButtonHandler {
		/// <summary>Label key of every row, then the arguments it takes.</summary>
		private readonly Dictionary<string, string[]> _labels = new();

		/// <summary>Keys of the rows currently turned on.</summary>
		private readonly HashSet<string> _selected = new();

		/// <summary>Language key of the summary shown on the button.</summary>
		private string _summaryKey = "settings.multi.summary";

		/// <summary>List currently open, or <c>null</c>.</summary>
		private IModal _modal;

		/// <summary>Menu the list is opened in, kept to open it again when it was closed.</summary>
		private IMenu _menu;

		/// <summary>Keys of the rows currently turned on.</summary>
		protected IEnumerable<string> Selected
			=> _selected;

		/// <summary>Keys of every row, in the order they were declared.</summary>
		protected IEnumerable<string> Choices
			=> _labels.Keys;

		/// <summary>Value stored in the config: the keys of the rows that are on, space separated.</summary>
		public override object Value
			=> string.Join(" ", _selected);

		/// <summary>Keys of every row: what <see cref="Apply"/>, <see cref="Add"/> and <see cref="Remove"/> accept.</summary>
		public override string[] Suggestions
			=> _labels.Keys.ToArray();

		/// <summary>Builder of the modal the list is shown in.</summary>
		/// <param name="menu">Menu asking for the list.</param>
		abstract protected IModalBuilder GetModalBuilder(IMenu menu);

		/// <summary>Prefab of one row of the list: a switch, with its <c>toggle</c> and <c>label</c> references.</summary>
		abstract protected GameObject GetRowPrefab();

		/// <summary>Declares a row of the list.</summary>
		/// <param name="key">Value of the row, written in the config when it is on.</param>
		/// <param name="labelKey">Language key of the row.</param>
		/// <param name="arguments">Arguments of the label.</param>
		protected void AddChoice(string key, string labelKey, params string[] arguments)
			=> _labels[key] = new[] { labelKey }
				.Concat(arguments ?? Array.Empty<string>())
				.ToArray();

		/// <summary>Removes every declared row.</summary>
		protected void ClearChoices()
			=> _labels.Clear();

		/// <summary>Language key of the summary written on the button, taking the list of names as argument.</summary>
		/// <param name="key">Language key.</param>
		protected void SetSummaryKey(string key)
			=> _summaryKey = key;

		/// <summary>Called after every change of the selection.</summary>
		protected virtual void OnSelectionChanged() { }

		public override GameObject GetContent(RectTransform transform, IMenu menu) {
			var go = base.GetContent(transform, menu);

			UpdateSummary();
			return go;
		}

		public override void OnClick(IContext context) {
			if (!context.Has("menu"))
				return;

			Open(context.Get<IMenu>("menu"));
		}

		/// <summary>See <see cref="IHandler.Apply"/>: takes the space separated keys of the rows to turn on.</summary>
		public override bool Apply(object value) {
			if (value == null)
				return false;

			_selected.Clear();

			foreach (var key in KeysOf(value))
				_selected.Add(key);

			Rethink();
			return true;
		}

		/// <summary>
		/// See <see cref="IHandler.Add"/>: turns the space separated keys on and keeps the other rows as they
		/// are. Returns false when none of the keys is a declared row.
		/// </summary>
		public override bool Add(object value)
			=> Change(value, on: true);

		/// <summary>
		/// See <see cref="IHandler.Remove"/>: turns the space separated keys off and keeps the other rows as
		/// they are. Returns false when none of the keys is a declared row.
		/// </summary>
		public override bool Remove(object value)
			=> Change(value, on: false);

		/// <summary>Turns the rows named by a value on or off, leaving the other ones untouched.</summary>
		/// <param name="value">Keys of the rows, separated by spaces, commas or semicolons.</param>
		/// <param name="on">Whether the rows are turned on or off.</param>
		private bool Change(object value, bool on) {
			var keys = KeysOf(value);

			if (keys.Length == 0)
				return false;

			foreach (var key in keys) {
				if (on)
					_selected.Add(key);
				else
					_selected.Remove(key);
			}

			Rethink();
			return true;
		}

		/// <summary>Keys of a value, keeping only the ones this handler declares.</summary>
		/// <param name="value">Value coming from the config or the terminal.</param>
		private string[] KeysOf(object value)
			=> value == null
				? Array.Empty<string>()
				: value
					.ToString()
					.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
					.Where(_labels.ContainsKey)
					.ToArray();

		/// <summary>Shows the new answer of the button and gives it to the derived handler.</summary>
		private void Rethink() {
			UpdateSummary();
			OnSelectionChanged();
		}

		/// <summary>Opens the list of switches.</summary>
		/// <param name="menu">Menu the list belongs to.</param>
		private void Open(IMenu menu) {
			var builder = GetModalBuilder(menu);
			if (builder == null)
				return;

			_menu = menu;

			builder.SetTitle($"settings.entry.{string.Join(".", Path)}.label");
			builder.SetClosable(true);
			builder.SetOptions(OnPicked, GetRows(), GetRowPrefab(), IsOn);

			_modal = builder.Build();
			_modal.Show();
		}

		/// <summary>Rows of the list: one switch per declared choice.</summary>
		private Dictionary<string, string[]> GetRows()
			=> _labels.ToDictionary(pair => pair.Key, pair => pair.Value);

		/// <summary>Whether a row is on.</summary>
		/// <param name="key">Key of the row.</param>
		private bool IsOn(string key)
			=> _selected.Contains(key);

		/// <summary>Turns a row on or off.</summary>
		/// <param name="key">Row picked.</param>
		private void OnPicked(string key) {
			if (!_selected.Remove(key))
				_selected.Add(key);

			UpdateSummary();
			OnSelectionChanged();

			// A switch leaves the list open: it only has to be opened again when a button row closed it.
			if (_modal == null || !_modal.IsOpen())
				Reopen();
		}

		/// <summary>Opens the list again after a row closed it.</summary>
		private void Reopen() {
			var menu  = _menu;
			var modal = _modal;

			_modal = null;
			modal?.Dispose();

			if (menu != null)
				Open(menu);
		}

		/// <summary>Writes the names of the rows that are on, in the language of the session.</summary>
		private void UpdateSummary() {
			var names = _labels
				.Where(pair => _selected.Contains(pair.Key))
				.Select(pair => LanguageManager.Get(pair.Value[0], pair.Value.Skip(1).Cast<object>().ToArray()))
				.ToArray();

			SetButtonText(_summaryKey, names.Length == 0 ? "\u2014" : string.Join(", ", names));
		}
	}
}
