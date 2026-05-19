using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using RinaUtilityAI.Category; // BehaviourCategoryのネームスペース
using RinaUtilityAI.Interface;

namespace RinaUtilityAI.Editor {
    public class UtilityGraphView : GraphView {

        // 画面上の全ノードを管理する辞書（実データのインスタンスID -> View）
        private Dictionary<int, UtilityNodeView> _nodeViewMap = new();

        public UtilityGraphView() {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
        }

        // ★★★ 【ロード処理】 ScriptableObjectのツリー構造からグラフを生成 ★★★
        public void LoadGraph(BehaviourCategory rootCategory) {
            // 一旦画面をクリア
            DeleteElements(graphElements);
            _nodeViewMap.Clear();

            if (rootCategory == null) return;

            // 1. ノードの再帰的生成
            GenerateNodesRecursive(rootCategory, new Vector2(100, 200));

            // 2. エッジ（線）の再帰的接続
            ConnectEdgesRecursive(rootCategory);
        }

        private void GenerateNodesRecursive(AUtilityNode node, Vector2 position) {
            if (node == null || _nodeViewMap.ContainsKey(node.GetInstanceID())) return;

            var nodeView = UtilityNodeView.CreateNode(node, position);
            AddElement(nodeView);
            _nodeViewMap[node.GetInstanceID()] = nodeView;

            // カテゴリ（枝）だった場合は、その子ノードも再帰的に生成
            if (node is BehaviourCategory category) {
                float childY = position.y - (category.ChildNodes.Count * 60f) / 2f;
                foreach (var child in category.ChildNodes) {
                    if (child == null) continue;
                    // 横にずらして配置
                    GenerateNodesRecursive(child as AUtilityNode, new Vector2(position.x + 250f, childY));
                    childY += 120f;
                }
            }
        }

        private void ConnectEdgesRecursive(AUtilityNode node) {
            if (node == null || node is not BehaviourCategory category) return;

            if (_nodeViewMap.TryGetValue(node.GetInstanceID(), out var parentView)) {
                foreach (var child in category.ChildNodes) {
                    if (child == null) continue;
                    if (_nodeViewMap.TryGetValue((child as AUtilityNode).GetInstanceID(), out var childView)) {
                        // 線（Edge）を作って繋ぐ
                        var edge = parentView.OutputPort.ConnectTo(childView.InputPort);
                        AddElement(edge);
                    }
                    // 子ノードのその先へ
                    ConnectEdgesRecursive(child as AUtilityNode);
                }
            }
        }

        // ★★★ 【セーブ処理】 画面上の接続からScriptableObjectのListを更新 ★★★
        public void SaveGraph() {
            // 画面上のすべてのノードViewをループ
            foreach (var kvp in _nodeViewMap) {
                var parentView = kvp.Value;

                // もしこのノードがカテゴリ（枝）なら、接続されている子ノードのリストを再構築する
                if (parentView.TargetNode is BehaviourCategory parentCategory) {

                    // 変更前にUndoできるように記録
                    Undo.RecordObject(parentCategory, "Update Utility AI Connections");

                    // リフレクションやシリアライズのために一度リストをクリア
                    // 実装した BehaviourCategory の childNodes フィールドにアクセス
                    var serializedObj = new SerializedObject(parentCategory);
                    var childNodesProperty = serializedObj.FindProperty("childNodes");
                    childNodesProperty.ClearArray();

                    // 出力ポートに繋がっているすべての線をスキャン
                    var connections = parentView.OutputPort.connections;
                    int index = 0;
                    foreach (var edge in connections) {
                        if (edge.input.node is UtilityNodeView childView) {
                            // 繋がっている子の実データ（SO）をリストに追加
                            childNodesProperty.InsertArrayElementAtIndex(index);
                            childNodesProperty.GetArrayElementAtIndex(index).objectReferenceValue = childView.TargetNode;
                            index++;
                        }
                    }

                    serializedObj.ApplyModifiedProperties();
                    EditorUtility.SetDirty(parentCategory); // 変更を通知
                }
            }

            AssetDatabase.SaveAssets(); // ディスクに保存
            Debug.Log("<color=green>【Utility AI】 グラフの保存が完了しました！</color>");
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
    }
}
