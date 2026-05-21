using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RinaUtilityAI.Category;
using RinaUtilityAI.Interface;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace RinaUtilityAI.Editor {
	public sealed class UtilityGraphView : GraphView {

		private const float NodeWidth = 260f;
		private const float HorizontalSpacing = 340f;
		private const float VerticalSpacing = 170f;

		private static readonly BindingFlags InstanceFieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		private static readonly FieldInfo MachineNodesField = typeof(PrioritizationMachine).GetField("nodes", InstanceFieldFlags);
		private static readonly FieldInfo CategoryChildrenField = typeof(BehaviourCategory).GetField("childNodes", InstanceFieldFlags);

		private readonly Dictionary<AUtilityNode, UtilityNodeView> nodeViewMap = new();
		private readonly UtilityEdgeConnectorListener edgeConnectorListener;
		private readonly GridBackground gridBackground;
		private UtilityNodeView rootView;
		private Object rootObject;

		public UtilityGraphView() {
			SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
			this.AddManipulator(new ContentDragger());
			this.AddManipulator(new SelectionDragger());
			this.AddManipulator(new RectangleSelector());

			edgeConnectorListener = new UtilityEdgeConnectorListener(this);
			gridBackground = new GridBackground();
			Insert(0, gridBackground);
			gridBackground.StretchToParentSize();

			style.flexGrow = 1;
		}

		public void LoadGraph(Object newRootObject) {
			rootObject = NormalizeRootObject(newRootObject);
			ClearGraph();

			if (rootObject == null) {
				return;
			}

			if (rootObject is PrioritizationMachine machine) {
				rootView = UtilityNodeView.CreateRootNode(machine, new Vector2(80f, 220f), edgeConnectorListener);
				AddElement(rootView);
				BuildTree(GetMachineRootNodes(machine), rootView, 220f, new HashSet<AUtilityNode>());
			} else if (rootObject is BehaviourCategory category) {
				var categoryView = CreateOrGetNodeView(category, new Vector2(80f, 220f));
				rootView = categoryView;
				BuildTree(GetCategoryChildren(category), categoryView, 220f, new HashSet<AUtilityNode> { category });
			}

			FrameAllSoon();
		}

		public void SaveGraph() {
			if (rootObject == null) {
				Debug.LogWarning("【Utility AI】保存対象が選択されていません。");
				return;
			}

			if (rootObject is PrioritizationMachine machine) {
				SetMachineRootNodes(machine, GetConnectedChildren(rootView));
				MarkChanged(machine);
			}

			foreach (var pair in nodeViewMap) {
				var category = pair.Key as BehaviourCategory;
				if (category == null) {
					continue;
				}

				SetCategoryChildren(category, GetConnectedChildren(pair.Value));
				MarkChanged(category);
			}

			AssetDatabase.SaveAssets();
			Debug.Log("【Utility AI】グラフを保存しました。");
		}

		public void ShowAddChildMenu(UtilityNodeView parentView, Vector2 position) {
			if (parentView == null || !parentView.CanHaveChildren) {
				return;
			}

			var menu = new GenericMenu();
			menu.AddItem(new GUIContent("Add Existing Node..."), false, () => ShowExistingNodeSelectionMenu(parentView, position));
			menu.AddItem(new GUIContent("Create Category"), false, () => CreateCategoryChild(parentView, position));
			menu.DropDown(new Rect(position, Vector2.zero));
		}

		public void ShowExistingNodeSelectionMenu(UtilityNodeView parentView, Vector2 position) {
			if (parentView == null || !parentView.CanHaveChildren) {
				return;
			}

			var nodes = FindUtilityNodeAssets()
				.Where(node => node != null)
				.Where(node => node != parentView.TargetNode)
				.OrderBy(node => node.GetType().Name)
				.ThenBy(node => node.name)
				.ToList();

			var menu = new GenericMenu();
			var added = false;

			foreach (var node in nodes) {
				var status = CanConnect(parentView, node) ? MenuStatus.Enabled : MenuStatus.Disabled;
				var label = $"{node.GetType().Name}/{node.name}";
				if (status == MenuStatus.Enabled) {
					menu.AddItem(new GUIContent(label), false, () => AddChildVisual(parentView, node, position));
				} else {
					menu.AddDisabledItem(new GUIContent(label));
				}
				added = true;
			}

			if (!added) {
				menu.AddDisabledItem(new GUIContent("No AUtilityNode assets found"));
			}

			menu.DropDown(new Rect(position, Vector2.zero));
		}

		public void CreateCategoryChild(UtilityNodeView parentView, Vector2 position) {
			if (parentView == null || !parentView.CanHaveChildren) {
				return;
			}

			var category = ScriptableObject.CreateInstance<BehaviourCategory>();
			category.name = "New BehaviourCategory";

			var folderPath = GetCreationFolder();
			var assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folderPath, category.name + ".asset"));
			Undo.RegisterCreatedObjectUndo(category, "Create Utility AI Category");
			AssetDatabase.CreateAsset(category, assetPath);
			AssetDatabase.SaveAssets();

			AddChildVisual(parentView, category, position);
			Selection.activeObject = category;
			EditorGUIUtility.PingObject(category);
		}

		public void RegisterEdge(Edge edge) {
			var parentView = edge?.output?.node as UtilityNodeView;
			var childView = edge?.input?.node as UtilityNodeView;
			if (parentView == null || childView == null) {
				return;
			}

			if (!CanConnect(parentView, childView.TargetNode)) {
				edge.output.Disconnect(edge);
				edge.input.Disconnect(edge);
				RemoveEdgeIfAttached(edge);
				return;
			}

			if (HasExistingEdge(parentView, childView, edge)) {
				edge.output.Disconnect(edge);
				edge.input.Disconnect(edge);
				RemoveEdgeIfAttached(edge);
				return;
			}

			if (edge.GetFirstAncestorOfType<GraphView>() == null) {
				AddElement(edge);
			}
		}

		private void RemoveEdgeIfAttached(Edge edge) {
			if (edge != null && edge.GetFirstAncestorOfType<GraphView>() != null) {
				RemoveElement(edge);
			}
		}

		public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) {
			var compatiblePorts = new List<Port>();
			var startView = startPort?.node as UtilityNodeView;
			if (startView == null) {
				return compatiblePorts;
			}

			ports.ForEach(port => {
				if (port == startPort || port.node == startPort.node || port.direction == startPort.direction) {
					return;
				}

				var outputView = startPort.direction == Direction.Output ? startView : port.node as UtilityNodeView;
				var inputView = startPort.direction == Direction.Input ? startView : port.node as UtilityNodeView;
				if (outputView == null || inputView == null) {
					return;
				}

				if (CanConnect(outputView, inputView.TargetNode)) {
					compatiblePorts.Add(port);
				}
			});

			return compatiblePorts;
		}

		public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
			base.BuildContextualMenu(evt);

			var parentView = GetSelectedParentView() ?? rootView;
			if (parentView == null || !parentView.CanHaveChildren) {
				return;
			}

			evt.menu.AppendSeparator();
			evt.menu.AppendAction("Add Existing Node...", _ => ShowExistingNodeSelectionMenu(parentView, evt.mousePosition));
			evt.menu.AppendAction("Create Category", _ => CreateCategoryChild(parentView, evt.mousePosition));
		}

		private void ClearGraph() {
			var elements = graphElements.ToList();
			DeleteElements(elements);
			nodeViewMap.Clear();
			rootView = null;
			Insert(0, gridBackground);
			gridBackground.StretchToParentSize();
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

		private float BuildTree(IReadOnlyList<AUtilityNode> children, UtilityNodeView parentView, float startY, HashSet<AUtilityNode> ancestors) {
			if (children == null || children.Count == 0) {
				return startY + VerticalSpacing;
			}

			var parentPosition = parentView.GetPosition();
			var childX = parentPosition.x + HorizontalSpacing;
			var y = startY;
			foreach (var child in children) {
				if (child == null) {
					continue;
				}

				if (ancestors.Contains(child)) {
					Debug.LogWarning($"【Utility AI】循環参照を検出したため、{parentView.title} -> {child.name} の表示をスキップしました。");
					y += VerticalSpacing;
					continue;
				}

				var childView = CreateOrGetNodeView(child, new Vector2(childX, y));
				ConnectViews(parentView, childView);

				if (child is BehaviourCategory category && !ancestors.Contains(child)) {
					var nextAncestors = new HashSet<AUtilityNode>(ancestors) { child };
					var nextY = BuildTree(GetCategoryChildren(category), childView, y, nextAncestors);
					var subtreeHeight = nextY - y;
					childView.SetPosition(new Rect(childX, y + Mathf.Max(0f, subtreeHeight - VerticalSpacing) * 0.5f, NodeWidth, 130f));
					y = nextY;
				} else {
					y += VerticalSpacing;
				}
			}

			return y;
		}

		private UtilityNodeView CreateOrGetNodeView(AUtilityNode node, Vector2 position) {
			if (nodeViewMap.TryGetValue(node, out var existingView)) {
				return existingView;
			}

			var nodeView = UtilityNodeView.CreateUtilityNode(node, position, edgeConnectorListener);
			AddElement(nodeView);
			nodeViewMap[node] = nodeView;
			return nodeView;
		}

		private void AddChildVisual(UtilityNodeView parentView, AUtilityNode childNode, Vector2 position) {
			if (!CanConnect(parentView, childNode)) {
				return;
			}

			var graphPosition = this.ChangeCoordinatesTo(contentViewContainer, position);
			if (float.IsNaN(graphPosition.x) || float.IsNaN(graphPosition.y) || graphPosition == Vector2.zero) {
				var parentPosition = parentView.GetPosition();
				graphPosition = new Vector2(parentPosition.x + HorizontalSpacing, parentPosition.y);
			}

			var childView = CreateOrGetNodeView(childNode, graphPosition);
			ConnectViews(parentView, childView);

			if (childNode is BehaviourCategory category) {
				BuildTree(GetCategoryChildren(category), childView, graphPosition.y, new HashSet<AUtilityNode> { category });
			}
		}

		private void ConnectViews(UtilityNodeView parentView, UtilityNodeView childView) {
			if (parentView?.OutputPort == null || childView?.InputPort == null || HasExistingEdge(parentView, childView, null)) {
				return;
			}

			var edge = parentView.OutputPort.ConnectTo(childView.InputPort);
			AddElement(edge);
		}

		private bool CanConnect(UtilityNodeView parentView, AUtilityNode childNode) {
			if (parentView == null || childNode == null || !parentView.CanHaveChildren) {
				return false;
			}

			if (parentView.TargetNode == childNode) {
				return false;
			}

			return parentView.IsGraphRoot || (!HasPath(childNode, parentView.TargetNode) && !HasSerializedPath(childNode, parentView.TargetNode, new HashSet<AUtilityNode>()));
		}

		private bool HasPath(AUtilityNode fromNode, AUtilityNode targetNode) {
			if (fromNode == null || targetNode == null) {
				return false;
			}

			if (fromNode == targetNode) {
				return true;
			}

			if (!nodeViewMap.TryGetValue(fromNode, out var fromView) || fromView.OutputPort == null) {
				return false;
			}

			foreach (var edge in fromView.OutputPort.connections) {
				var childView = edge.input?.node as UtilityNodeView;
				if (childView == null || childView.TargetNode == null) {
					continue;
				}

				if (HasPath(childView.TargetNode, targetNode)) {
					return true;
				}
			}

			return false;
		}

		private static bool HasSerializedPath(AUtilityNode fromNode, AUtilityNode targetNode, HashSet<AUtilityNode> visited) {
			if (fromNode == null || targetNode == null || !visited.Add(fromNode)) {
				return false;
			}

			if (fromNode == targetNode) {
				return true;
			}

			var category = fromNode as BehaviourCategory;
			if (category == null) {
				return false;
			}

			foreach (var child in GetCategoryChildren(category)) {
				if (HasSerializedPath(child, targetNode, visited)) {
					return true;
				}
			}

			return false;
		}

		private static bool HasExistingEdge(UtilityNodeView parentView, UtilityNodeView childView, Edge ignoredEdge) {
			if (parentView?.OutputPort == null || childView?.InputPort == null) {
				return false;
			}

			foreach (var edge in parentView.OutputPort.connections) {
				if (edge == ignoredEdge) {
					continue;
				}

				if (edge.input?.node == childView) {
					return true;
				}
			}

			return false;
		}

		private List<AUtilityNode> GetConnectedChildren(UtilityNodeView parentView) {
			var result = new List<AUtilityNode>();
			if (parentView?.OutputPort == null) {
				return result;
			}

			var edges = parentView.OutputPort.connections
				.Where(edge => edge.input?.node is UtilityNodeView)
				.OrderBy(edge => ((UtilityNodeView)edge.input.node).GetPosition().y)
				.ThenBy(edge => ((UtilityNodeView)edge.input.node).GetPosition().x);

			foreach (var edge in edges) {
				if (edge.input.node is UtilityNodeView childView && childView.TargetNode != null && !result.Contains(childView.TargetNode)) {
					result.Add(childView.TargetNode);
				}
			}

			return result;
		}

		private UtilityNodeView GetSelectedParentView() {
			foreach (var item in selection) {
				if (item is UtilityNodeView nodeView && nodeView.CanHaveChildren) {
					return nodeView;
				}
			}

			return null;
		}

		private static List<AUtilityNode> GetMachineRootNodes(PrioritizationMachine machine) {
			var result = new List<AUtilityNode>();
			if (machine == null || MachineNodesField == null) {
				return result;
			}

			var values = MachineNodesField.GetValue(machine) as IEnumerable;
			if (values == null) {
				return result;
			}

			foreach (var value in values) {
				if (value is AUtilityNode node) {
					result.Add(node);
				}
			}

			return result;
		}

		private static void SetMachineRootNodes(PrioritizationMachine machine, IReadOnlyList<AUtilityNode> nodes) {
			if (machine == null || MachineNodesField == null) {
				return;
			}

			Undo.RecordObject(machine, "Save Utility AI Graph");
			var list = MachineNodesField.GetValue(machine) as List<IUtilityNode>;
			if (list == null) {
				list = new List<IUtilityNode>();
				MachineNodesField.SetValue(machine, list);
			}

			list.Clear();
			foreach (var node in nodes) {
				list.Add(node);
			}
		}

		private static List<AUtilityNode> GetCategoryChildren(BehaviourCategory category) {
			if (category == null || CategoryChildrenField == null) {
				return new List<AUtilityNode>();
			}

			return CategoryChildrenField.GetValue(category) as List<AUtilityNode> ?? new List<AUtilityNode>();
		}

		private static void SetCategoryChildren(BehaviourCategory category, IReadOnlyList<AUtilityNode> nodes) {
			if (category == null || CategoryChildrenField == null) {
				return;
			}

			Undo.RecordObject(category, "Save Utility AI Graph");
			var list = CategoryChildrenField.GetValue(category) as List<AUtilityNode>;
			if (list == null) {
				list = new List<AUtilityNode>();
				CategoryChildrenField.SetValue(category, list);
			}

			list.Clear();
			list.AddRange(nodes);
		}

		private static IEnumerable<AUtilityNode> FindUtilityNodeAssets() {
			var guids = AssetDatabase.FindAssets("t:ScriptableObject");
			var emitted = new HashSet<AUtilityNode>();
			foreach (var guid in guids) {
				var path = AssetDatabase.GUIDToAssetPath(guid);
				foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path)) {
					if (asset is AUtilityNode node && emitted.Add(node)) {
						yield return node;
					}
				}
			}
		}

		private string GetCreationFolder() {
			if (Selection.activeObject != null) {
				var selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
				if (!string.IsNullOrEmpty(selectedPath)) {
					if (Directory.Exists(selectedPath)) {
						return selectedPath;
					}

					var selectedFolder = Path.GetDirectoryName(selectedPath);
					if (!string.IsNullOrEmpty(selectedFolder)) {
						return selectedFolder;
					}
				}
			}

			if (rootObject != null) {
				var rootPath = AssetDatabase.GetAssetPath(rootObject);
				if (!string.IsNullOrEmpty(rootPath)) {
					var rootFolder = Path.GetDirectoryName(rootPath);
					if (!string.IsNullOrEmpty(rootFolder)) {
						return rootFolder;
					}
				}
			}

			return "Assets";
		}

		private static void MarkChanged(Object target) {
			if (target == null) {
				return;
			}

			EditorUtility.SetDirty(target);
			if (target is Component component) {
				if (PrefabUtility.IsPartOfPrefabInstance(component)) {
					PrefabUtility.RecordPrefabInstancePropertyModifications(component);
				}
				if (!EditorUtility.IsPersistent(component)) {
					EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
				}
			}
		}

		private void FrameAllSoon() {
			schedule.Execute(() => {
				if (panel != null) {
					FrameAll();
				}
			}).ExecuteLater(50);
		}

		private enum MenuStatus {
			Enabled,
			Disabled
		}
	}
}
