using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using RinaUtilityAI.Category;

namespace RinaUtilityAI.Editor {
	public class UtilityGraphWindow : EditorWindow {

		private UtilityGraphView _graphView;

		private ObjectField _rootCategoryObjectField;

		[MenuItem("Window/Rina Utility AI/Graph Editor")]
		public static void OpenWindow() {
			var window = GetWindow<UtilityGraphWindow>("Utility AI Graph");
			window.minSize = new Vector2(800, 600);
		}

		private void OnEnable() {
			rootVisualElement.Clear();
			rootVisualElement.style.flexDirection = FlexDirection.Column;
			rootVisualElement.style.flexGrow = 1;

			var toolbar = new Toolbar();
			toolbar.style.flexShrink = 0;

			_rootCategoryObjectField = new ObjectField("Root Category") {
				objectType = typeof(BehaviourCategory),
				allowSceneObjects = false
			};
			_rootCategoryObjectField.RegisterValueChangedCallback(evt => {
				if (_graphView != null) {
					_graphView.LoadGraph(evt.newValue as BehaviourCategory);
				}
			});
			toolbar.Add(_rootCategoryObjectField);

			var loadButton = new Button(() => {
				if (_graphView != null) {
					_graphView.LoadGraph(_rootCategoryObjectField.value as BehaviourCategory);
				}
			}) { text = "Load" };
			toolbar.Add(loadButton);

			var saveButton = new Button(() => {
				if (_graphView != null) {
					_graphView.SaveGraph();
				}
			}) { text = "Save" };
			toolbar.Add(saveButton);

			rootVisualElement.Add(toolbar);

			if (_graphView != null) {
				rootVisualElement.Remove(_graphView);
			}
			_graphView = new UtilityGraphView();
			_graphView.style.flexGrow = 1;
			rootVisualElement.Add(_graphView);
		}
	}
}
