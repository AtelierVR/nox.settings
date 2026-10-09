using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.Settings.Runtime;
using Nox.UI;
using UnityEngine;

namespace Nox.Settings.Clients {
	public class SettingsPage : IPage {
		internal static string GetStaticKey()
			=> "settings";

		public string GetKey()
			=> GetStaticKey();

		internal int               MId;
		private  object[]          _context;
		private  GameObject        _content;
		private  SettingsComponent _component;
		private  string[]          _current;
		internal string            Filter = string.Empty;

		public void OnRefresh()
			=> Refresh();

		private static bool T<T>(object[] o, int index, out T value) {
			if (o.Length > index && o[index] is T t) {
				value = t;
				return true;
			}

			value = default;
			return false;
		}

		internal static IPage OnGotoAction(IMenu menu, object[] context)
			=> new SettingsPage {
				MId      = menu.Id,
				_context = context,
				_current = T(context, 0, out string[] path) ? path : Array.Empty<string>()
			};


		private void Refresh() {
			if (!_component) return;
			_component.UpdateNavigation().Forget();
			_component.UpdateContent().Forget();
			_component.UpdateIcon().Forget();
			_component.UpdateTitles();
		}

		public void OnDisplay(IPage lastPage)
			=> Refresh();

		public object[] GetContext()
			=> _context;

		public IMenu GetMenu()
			=> Client.UiAPI.Get<IMenu>(MId);

		public GameObject GetContent(RectTransform parent) {
			if (_content) return _content;
			(_content, _component) = SettingsComponent.Generate(this, parent);
			UpdateLayout.UpdateImmediate(_content);
			return _content;
		}

		public void OnOpen(IPage lastPage) {
			Main.OnHandlerAdded.AddListener(OnSettingsChanged);
			Main.OnHandlerRemoved.AddListener(OnSettingsChanged);
		}

		private void OnSettingsChanged(IHandler arg0)
			=> Refresh();

		public void OnRemove() {
			Main.OnHandlerAdded.RemoveListener(OnSettingsChanged);
			Main.OnHandlerRemoved.RemoveListener(OnSettingsChanged);
		}

		/// <summary>
		/// Whether the handler matches the current filter (label or path).
		/// A handler that is not displayed (order handler) never matches.
		/// </summary>
		internal bool MatchesFilter(IHandler handler) {
			if (handler == null)
				return false;
			if (string.IsNullOrWhiteSpace(Filter))
				return true;
			var query = Filter.Trim();
			var path  = string.Join(".", handler.Path);
			if (path.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
				return true;
			var label = LanguageManager.Get($"settings.entry.{path}.label");
			return !string.IsNullOrEmpty(label)
				&& label.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		/// <summary>
		/// All displayable settings pages (categories) that contain at least one
		/// active handler matching the filter, ordered by the greatest
		/// <see cref="IHandler.Order"/> found among their handlers.
		/// </summary>
		public CategoryDetails[] GetCategories() {
			var handlers = Main.Handlers
				.Where(h => h.IsActive && MatchesFilter(h))
				.Where(h => !string.IsNullOrEmpty(h.Split().Item1))
				.ToArray();

			return handlers
				.Select(h => h.Split().Item1)
				.Distinct()
				// OrderBy is stable: categories sharing the same order keeps
				// their registration order.
				.OrderBy(id => handlers
					.Where(h => h.Split().Item1 == id)
					.Max(h => h.Order))
				.Select(id => new CategoryDetails(id))
				.ToArray();
		}

		public CategoryDetails GetCategory() {
			var categories = GetCategories();
			if (categories.Length == 0)
				return null;
			var current = _current.Length > 0 ? _current[0] : null;
			return categories.FirstOrDefault(c => c.GetId() == current) ?? categories[0];
		}

		public CategoryDetails GetCategory(string category)
			=> Main.Handlers.Any(h => h.IsActive && h.Split().Item1 == category)
				? new CategoryDetails(category)
				: null;

		public GroupDetails[] GetGroups(string category)
			=> Main.Handlers
				.Where(h => h.IsActive && MatchesFilter(h) && h.Split().Item1 == category)
				.GroupBy(h => h.Split().Item2)
				.Select(g => {
					var ordered = g.ToArray().OrderBy(h => h).ToArray();
					return new GroupDetails {
						Handlers = ordered,
						Category = category,
						Group    = g.Key,
						Order    = ordered.Length > 0 ? ordered[0].Order : int.MaxValue
					};
				})
				.OrderBy(g => g)
				.ToArray();

		public void SetCurrent(string id) {
			var category = GetCategory(id);
			if (category == null) return;
			_current = new[] { id };
			if (!_component) return;
			_component.UpdateContent().Forget();
			_component.UpdateIcon().Forget();
			_component.UpdateTitles();
		}
	}

	public class GroupDetails : IComparable<GroupDetails> {
		public IHandler[] Handlers = Array.Empty<IHandler>();
		public string     Category;
		public string     Group;
		public int        Order;

		public string GetLabel()
			=> $"settings.group.{Category}.{Group}.label";

		public int CompareTo(GroupDetails other)
			=> Order.CompareTo(other.Order);
	}

	public class CategoryDetails {
		private readonly string _id;

		public CategoryDetails(string id)
			=> _id = id;

		public string GetId()
			=> _id;

		public string GetTitle()
			=> $"settings.page.{_id}.title";

		public async UniTask<Sprite> GetIcon()
			=> await Client.GetAssetAsync<Sprite>($"icons/{_id}.png");

		public string GetLabel()
			=> $"settings.page.{_id}.label";
	}
}