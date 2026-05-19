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
			// ツールバー（上部メニュー）の作成
			var toolbar = new Toolbar();

			// 1. ルートアセット選択用の窓
			_rootCategoryObjectField = new ObjectField("Root Category") {
				objectType = typeof(BehaviourCategory),
				allowSceneObjects = false
			};
			// アセットが変更されたら自動ロード
			_rootCategoryObjectField.RegisterValueChangedCallback(evt => {
				_graphView.LoadGraph(evt.newValue as BehaviourCategory);
			});
			toolbar.Add(_rootCategoryObjectField);

			// 2. ロードボタン
			var loadButton = new Button(() => {
				_graphView.LoadGraph(_rootCategoryObjectField.value as BehaviourCategory);
			}) { text = "Load" };
			toolbar.Add(loadButton);

			// 3. セーブボタン
			var saveButton = new Button(() => {
				_graphView.SaveGraph();
			}) { text = "Save" };
			toolbar.Add(saveButton);

			rootVisualElement.Add(toolbar);

			// グラフ本体の追加
			_graphView = new UtilityGraphView();
			_graphView.StretchToParentSize();
			rootVisualElement.Add(_graphView);
		}
	}
}
