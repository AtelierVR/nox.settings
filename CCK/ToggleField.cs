using Nox.CCK.Events;
using Nox.CCK.Language;
using Nox.CCK.Theme;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Nox.CCK.Settings {
	/// <summary>
	/// Toggle row of a settings entry: the toggle, its label and the animation of its knob, as one piece the
	/// handlers share.
	/// <para>
	/// A handler that owns a whole row adds it to the instantiated prefab; a handler that builds a row from other
	/// widgets (a volume slider with a mute switch, for instance) adds it on the object that carries the toggle:
	/// the value, the label and the animation then come from this class in both cases.
	/// </para>
	/// <para>
	/// The references are resolved from the hierarchy by name when they are left empty (<c>toggle</c>, <c>label</c>,
	/// <c>Knob</c>), which is how the prefabs of the settings rows are authored.
	/// </para>
	/// </summary>
	[AddComponentMenu("Nox/Settings/Toggle Field")]
	public class ToggleField : MonoBehaviour {
		/// <summary>Distance the knob travels from the centre of the track, in units of the row.</summary>
		private const float DEFAULT_OFFSET = 17f;

		[Tooltip("Ligne de toggle à instancier dans ce champ. Vide = la ligne est déjà là (le toggle est un enfant).")]
		public GameObject prefab;

		[Tooltip("Parent de la ligne instanciée. Vide = l'emplacement du toggle de cet objet.")]
		public RectTransform container;

		[Tooltip("Garde le label de la ligne instanciée. À décocher quand la ligne hôte a déjà le sien.")]
		public bool keepLabel = true;

		[Tooltip("Toggle de la ligne. Vide = l'enfant nommé 'toggle'.")]
		public Toggle toggle;

		[Tooltip("Label de la ligne. Vide = l'enfant nommé 'label'.")]
		public TextLanguage label;

		[Tooltip("Encombrement du switch instancié, tel que l'auteurise la ligne de toggle (70 x 60 : la piste fait 36 de haut et 70 de large).")]
		public Vector2 switchSize = new(70f, 60f);

		[Tooltip("Bouton du toggle. Vide = l'enfant nommé 'Knob'.")]
		public RectTransform knob;

		[Tooltip("Déplace le bouton quand la valeur change. À laisser vide si un autre script du prefab le fait déjà.")]
		public bool animateKnob = true;

		[Tooltip("Distance parcourue par le bouton de part et d'autre du centre.")]
		public float offset = DEFAULT_OFFSET;

		[Tooltip("Vitesse de glissement du bouton (1/s). 0 = instantané.")]
		public float smoothing = 18f;

		/// <summary>Raised when the toggle is clicked, with its new value.</summary>
		public readonly NoxEvent<bool> OnValueChanged = new();

		private bool _value;
		private string _keyLabel;
		private float _knobX;

		/// <summary>Row instantiated from <see cref="prefab"/>, kept so a second <see cref="Build"/> does not duplicate it.</summary>
		private GameObject _row;

		/// <summary>Value shown by the toggle.</summary>
		public bool Value
			=> _value;

		private void Awake()
			=> Build();

		private void OnDestroy() {
			if (toggle)
				toggle.onValueChanged.RemoveListener(OnInternalValueChanged);
		}

		private void LateUpdate() {
			if (!animateKnob || !knob)
				return;

			var target = _value ? offset : -offset;

			_knobX = smoothing > 0f
				? Mathf.Lerp(_knobX, target, 1f - Mathf.Exp(-smoothing * Time.deltaTime))
				: target;

			var position = knob.localPosition;
			position.x = _knobX;
			knob.localPosition = position;
		}

		/// <summary>Shows a value, optionally raising <see cref="OnValueChanged"/> as a user click would.</summary>
		/// <param name="value">Value to show.</param>
		/// <param name="notify">Whether the change comes from the user.</param>
		public void SetValue(bool value, bool notify = false) {
			_value = value;

			if (toggle)
				toggle.SetIsOnWithoutNotify(value);

			if (notify)
				OnValueChanged.Invoke(value);
		}

		/// <summary>Sets the language key of the label.</summary>
		/// <param name="key">Key of the label, translated by the language packs.</param>
		public void SetLabelKey(string key) {
			_keyLabel = key;

			if (label)
				label.UpdateText(key);
		}

		/// <summary>
		/// Resolves the parts of the row through the <see cref="Reference"/> keys of the prefab, then binds them. When a
		/// prefab is given it is instantiated where the host row ships its own toggle and that toggle is switched off,
		/// so a widget like the volume row shows the very switch every other entry shows.
		/// </summary>
		public void Build() {
			var inline = Reference.GetComponent<Toggle>("toggle", gameObject);

			if (prefab && !_row) {
				var parent = container
					? container
					: inline && inline.transform.parent is RectTransform slot
						? slot
						: (RectTransform)transform;

				_row = prefab.Instantiate(parent);

				toggle = Reference.GetComponent<Toggle>("toggle", _row);
				label = Reference.GetComponent<TextLanguage>("label", _row);
				knob = Reference.GetComponent<RectTransform>("knob", _row);

				if (!keepLabel && label) {
					label.gameObject.SetActive(false);
					label = null;
				}

				if (_row.transform is RectTransform rowRect) {
					rowRect.anchorMin = new Vector2(1f, 0.5f);
					rowRect.anchorMax = new Vector2(1f, 0.5f);
					rowRect.pivot = new Vector2(1f, 0.5f);
					rowRect.anchoredPosition = Vector2.zero;
					rowRect.sizeDelta = switchSize;
					rowRect.localScale = Vector3.one;
				}

				if (inline && toggle && inline.gameObject != toggle.gameObject)
					inline.gameObject.SetActive(false);
			} else {
				if (!toggle)
					toggle = inline;

				if (!label)
					label = Reference.GetComponent<TextLanguage>("label", gameObject);

				if (!knob)
					knob = Reference.GetComponent<RectTransform>("knob", gameObject);
			}

			if (!toggle)
				return;

			if (_row)
				ThemeManager.Apply(_row);

			toggle.onValueChanged.RemoveListener(OnInternalValueChanged);
			toggle.onValueChanged.AddListener(OnInternalValueChanged);
			toggle.SetIsOnWithoutNotify(_value);

			_knobX = knob
				? knob.localPosition.x
				: 0f;
		}

		private void OnInternalValueChanged(bool value) {
			_value = value;
			OnValueChanged.Invoke(value);
		}
	}
}
