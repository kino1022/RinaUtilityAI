using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using RinaUtilityAI.Category;
using RinaUtilityAI.Interface;

namespace RinaUtilityAI.Editor {
    public class UtilityGraphView : GraphView {

        private readonly Dictionary<AUtilityNode, UtilityNodeView> _nodeViewMap = new();
        private readonly GridBackground _gridBackground;
        private readonly GraphEdgeConnectorListener _edgeConnectorListener;

        public UtilityGraphView() {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            _edgeConnectorListener = new GraphEdgeConnectorListener(this);

            _gridBackground = new GridBackground();
            Insert(0, _gridBackground);
            _gridBackground.StretchToParentSize();

            style.flexGrow = 1;
        }

        public void LoadGraph(BehaviourCategory rootCategory) {
            _rootCategory = rootCategory;
            var elementsToRemove = new List<GraphElement>();
            foreach (var element in graphElements) {
                elementsToRemove.Add(element);
            }
            DeleteElements(elementsToRemove);
            Insert(0, _gridBackground);
            _gridBackground.StretchToParentSize();
            _nodeViewMap.Clear();

            if (rootCategory == null) return;

            GenerateNodesRecursive(rootCategory, new Vector2(100, 200));

            ConnectEdgesRecursive(rootCategory);
        }

        private BehaviourCategory _rootCategory;

        private void GenerateNodesRecursive(AUtilityNode node, Vector2 position) {
            if (node == null || _nodeViewMap.ContainsKey(node)) return;

            var nodeView = UtilityNodeView.CreateNode(node, position, _edgeConnectorListener);
            AddElement(nodeView);
            _nodeViewMap[node] = nodeView;

            if (node is IBehaviourCategory category && category.ChildNodes != null && category.ChildNodes.Count > 0) {
                float childY = position.y - (category.ChildNodes.Count * 60f) / 2f;
                foreach (var child in category.ChildNodes) {
                    if (child is not AUtilityNode childNode) continue;
                    GenerateNodesRecursive(childNode, new Vector2(position.x + 250f, childY));
                    childY += 120f;
                }
            }
        }

        private void ConnectEdgesRecursive(AUtilityNode node) {
            if (node == null) return;

            if (node is not IBehaviourCategory category) return;

            if (_nodeViewMap.TryGetValue(node, out var parentView) && parentView.OutputPort != null) {
                foreach (var child in category.ChildNodes) {
                    if (child is not AUtilityNode childNode) continue;
                    if (_nodeViewMap.TryGetValue(childNode, out var childView)) {
                        var edge = parentView.OutputPort.ConnectTo(childView.InputPort);
                        AddElement(edge);
                    }
                    ConnectEdgesRecursive(childNode);
                }
            }
        }

        public void SaveGraph() {
            foreach (var kvp in _nodeViewMap) {
                var parentView = kvp.Value;

                if (parentView.TargetNode is BehaviourCategory parentNode && parentView.OutputPort != null) {
                  Undo.RecordObject(parentNode, "Update Utility AI Connections");

                    var serializedObj = new SerializedObject(parentNode);
                    var childNodesProperty = serializedObj.FindProperty("childNodes");
                    childNodesProperty.ClearArray();

                    var connections = parentView.OutputPort.connections;
                    int index = 0;
                    foreach (var edge in connections) {
                        if (edge.input.node is UtilityNodeView childView) {
                            childNodesProperty.InsertArrayElementAtIndex(index);
                            childNodesProperty.GetArrayElementAtIndex(index).objectReferenceValue = childView.TargetNode;
                            index++;
                        }
                    }

                    serializedObj.ApplyModifiedProperties();
                    EditorUtility.SetDirty(parentNode);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("<color=green>【Utility AI】 グラフの保存が完了しました！</color>");
        }

        public void CreateChildCategory(BehaviourCategory parentCategory) {
            if (parentCategory == null) return;

            var childCategory = ScriptableObject.CreateInstance<BehaviourCategory>();
            childCategory.name = "New BehaviourCategory";

            var parentPath = AssetDatabase.GetAssetPath(parentCategory);
            var folderPath = string.IsNullOrEmpty(parentPath) ? "Assets" : Path.GetDirectoryName(parentPath);
            if (string.IsNullOrEmpty(folderPath)) {
                folderPath = "Assets";
            }

            var assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folderPath, childCategory.name + ".asset"));

            Undo.RegisterCreatedObjectUndo(childCategory, "Create BehaviourCategory");
            AssetDatabase.CreateAsset(childCategory, assetPath);
            AssetDatabase.SaveAssets();

            var serializedObj = new SerializedObject(parentCategory);
            var childNodesProperty = serializedObj.FindProperty("childNodes");
            var index = childNodesProperty.arraySize;
            childNodesProperty.InsertArrayElementAtIndex(index);
            childNodesProperty.GetArrayElementAtIndex(index).objectReferenceValue = childCategory;
            serializedObj.ApplyModifiedProperties();
            EditorUtility.SetDirty(parentCategory);

            LoadGraph(_rootCategory ?? parentCategory);
            Selection.activeObject = childCategory;
        }

        public void BeginPickExistingChildNode(BehaviourCategory parentCategory) {
            ShowExistingNodeSelectionMenu(parentCategory, Vector2.zero);
        }

        public void AddExistingChildNode(BehaviourCategory parentCategory, AUtilityNode childNode) {
            if (parentCategory == null || childNode == null) return;

            var serializedObj = new SerializedObject(parentCategory);
            var childNodesProperty = serializedObj.FindProperty("childNodes");

            for (var i = 0; i < childNodesProperty.arraySize; i++) {
                if (childNodesProperty.GetArrayElementAtIndex(i).objectReferenceValue == childNode) {
                    LoadGraph(_rootCategory ?? parentCategory);
                    Selection.activeObject = childNode;
                    return;
                }
            }

            Undo.RecordObject(parentCategory, "Add Existing Child Node");
            var index = childNodesProperty.arraySize;
            childNodesProperty.InsertArrayElementAtIndex(index);
            childNodesProperty.GetArrayElementAtIndex(index).objectReferenceValue = childNode;
            serializedObj.ApplyModifiedProperties();
            EditorUtility.SetDirty(parentCategory);

            LoadGraph(_rootCategory ?? parentCategory);
            Selection.activeObject = childNode;
        }

        public void ShowAddChildMenu(BehaviourCategory parentCategory, Vector2 position) {
            if (parentCategory == null) return;

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Add Existing AUtilityNode..."), false, () => ShowExistingNodeSelectionMenu(parentCategory, position));
            menu.AddItem(new GUIContent("Create New Category"), false, () => CreateChildCategory(parentCategory));
            menu.DropDown(new Rect(position, Vector2.zero));
        }

        private void ShowExistingNodeSelectionMenu(BehaviourCategory parentCategory, Vector2 position) {
            if (parentCategory == null) return;

            var menu = new GenericMenu();
            var assetGuids = AssetDatabase.FindAssets("t:AUtilityNode");
            var hasAnyItem = false;

            foreach (var guid in assetGuids) {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var node = AssetDatabase.LoadAssetAtPath<AUtilityNode>(assetPath);
                if (node == null || node == parentCategory) {
                    continue;
                }

                hasAnyItem = true;
                var label = string.IsNullOrEmpty(node.name) ? node.GetType().Name : node.name;
                menu.AddItem(new GUIContent(label), false, () => AddExistingChildNode(parentCategory, node));
            }

            if (!hasAnyItem) {
                menu.AddDisabledItem(new GUIContent("No AUtilityNode assets found"));
            }

            menu.DropDown(new Rect(position, Vector2.zero));
        }

        private BehaviourCategory GetSelectedCategoryOrRoot() {
            foreach (var selectable in selection) {
                if (selectable is UtilityNodeView nodeView && nodeView.TargetNode is BehaviourCategory category) {
                    return category;
                }
            }
            return _rootCategory;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
            base.BuildContextualMenu(evt);

            var targetCategory = GetSelectedCategoryOrRoot();
            if (targetCategory != null) {
                evt.menu.AppendAction("Add Existing AUtilityNode...", _ => ShowExistingNodeSelectionMenu(targetCategory, Vector2.zero));
                evt.menu.AppendAction("Create New Category", _ => CreateChildCategory(targetCategory));
            }
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) {
            var compatiblePorts = new List<Port>();
            ports.ForEach((port) => {
                if (startPort != port && startPort.node != port.node && startPort.direction != port.direction) {
                    compatiblePorts.Add(port);
                }
            });
            return compatiblePorts;
        }

        private sealed class GraphEdgeConnectorListener : IEdgeConnectorListener {

            private readonly UtilityGraphView _graphView;

            public GraphEdgeConnectorListener(UtilityGraphView graphView) {
                _graphView = graphView;
            }

            public void OnDropOutsidePort(Edge edge, Vector2 position) {
                if (edge?.output?.node is not UtilityNodeView sourceNodeView) {
                    return;
                }

                if (sourceNodeView.TargetNode is not BehaviourCategory parentCategory) {
                    return;
                }

                _graphView.ShowAddChildMenu(parentCategory, position);
            }

            public void OnDrop(GraphView graphView, Edge edge) {
                // 接続自体はGraphView側で保持されるため、
                // データ同期はSaveGraph時にまとめて行う。
            }
        }
    }
}
