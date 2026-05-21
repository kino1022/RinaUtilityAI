using RinaUtilityAI.Category;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace RinaUtilityAI.Editor {
	public sealed class UtilityGraphWindow : EditorWindow {

		private UtilityGraphView graphView;
		private ObjectField rootObjectField;
		private Label statusLabel;

		[MenuItem("Window/Rina Utility AI/Graph Editor")]
		public static void OpenWindow() {
			var window = GetWindow<UtilityGraphWindow>("Utility AI Graph");
			window.minSize = new Vector2(900f, 620f);
		}

		[MenuItem("Assets/Rina Utility AI/Open Graph", true)]
		private static bool ValidateOpenFromAssets() {
			return Selection.activeObject is BehaviourCategory;
		}

		[MenuItem("Assets/Rina Utility AI/Open Graph")]
		private static void OpenFromAssets() {
			var window = GetWindow<UtilityGraphWindow>("Utility AI Graph");
			window.SetRootObject(Selection.activeObject);
		}

		[MenuItem("CONTEXT/PrioritizationMachine/Open Utility AI Graph")]
		private static void OpenFromPrioritizationMachine(MenuCommand command) {
			var window = GetWindow<UtilityGraphWindow>("Utility AI Graph");
			window.SetRootObject(command.context);
		}

		private void OnEnable() {
			Build();
		}

		private void Build() {
			rootVisualElement.Clear();
			rootVisualElement.style.flexDirection = FlexDirection.Column;
			rootVisualElement.style.flexGrow = 1f;

			var toolbar = new Toolbar();
			toolbar.style.flexShrink = 0f;

			rootObjectField = new ObjectField("Root") {
				objectType = typeof(Object),
				allowSceneObjects = true
			};
			rootObjectField.style.minWidth = 320f;
			rootObjectField.RegisterValueChangedCallback(evt => SetRootObject(evt.newValue));
			toolbar.Add(rootObjectField);

			toolbar.Add(new Button(() => SetRootObject(rootObjectField.value)) { text = "Reload" });
			toolbar.Add(new Button(() => graphView.SaveGraph()) { text = "Save" });

			statusLabel = new Label();
			statusLabel.style.marginLeft = 8f;
			toolbar.Add(statusLabel);

			rootVisualElement.Add(toolbar);

			graphView = new UtilityGraphView();
			graphView.style.flexGrow = 1f;
			rootVisualElement.Add(graphView);

			if (Selection.activeObject is BehaviourCategory || Selection.activeObject is PrioritizationMachine || Selection.activeObject is GameObject) {
				SetRootObject(Selection.activeObject);
			} else if (Selection.activeGameObject != null) {
				SetRootObject(Selection.activeGameObject);
			} else {
				SetStatus("PrioritizationMachine, BehaviourCategory, or a GameObject with PrioritizationMachine can be selected.");
			}
		}

		private void SetRootObject(Object value) {
			if (rootObjectField != null && rootObjectField.value != value) {
				rootObjectField.SetValueWithoutNotify(value);
			}

			if (graphView == null) {
				return;
			}

			var normalized = NormalizeRootObject(value);
			graphView.LoadGraph(normalized);
			SetStatus(normalized == null
				? "Root must be PrioritizationMachine, BehaviourCategory, or a GameObject with PrioritizationMachine."
				: $"Editing: {normalized.name}");
		}

		private static Object NormalizeRootObject(Object value) {
			if (value is GameObject gameObject) {
				return gameObject.GetComponent<PrioritizationMachine>();
			}

			if (value is PrioritizationMachine || value is BehaviourCategory) {
				return value;
			}

			return null;
		}

		private void SetStatus(string message) {
			if (statusLabel != null) {
				statusLabel.text = message;
			}
		}
	}
}
