using UnityEditor.Experimental.GraphView;
using UnityEngine;
using RinaUtilityAI.Category;
using RinaUtilityAI.Interface;
using UnityEngine.UIElements; // AUtilityNodeのネームスペース

namespace RinaUtilityAI.Editor {
	public class UtilityNodeView : Node {
		// ▼ このノードが表現している裏側の実データ（ScriptableObject）
		public AUtilityNode TargetNode { get; private set; }

		// 入力ポートと出力ポートの参照を保持
		public Port InputPort { get; private set; }

		public Port OutputPort { get; private set; }

		public static UtilityNodeView CreateNode(AUtilityNode targetNode, Vector2 position, IEdgeConnectorListener edgeConnectorListener) {
			var node = new UtilityNodeView {
				title = targetNode != null ? targetNode.name : "Unnamed Node",
				TargetNode = targetNode
			};

			// 入力ポート（左側）の作成
			node.InputPort = node.InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(bool));
			node.InputPort.portName = "Input";
			node.inputContainer.Add(node.InputPort);

			// 出力ポートは分岐ノード（カテゴリ）のみ持つ
			if (targetNode is IBehaviourCategory) {
				node.OutputPort = node.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
				node.OutputPort.portName = "Child Nodes";
				node.OutputPort.AddManipulator(new EdgeConnector<Edge>(edgeConnectorListener));
				node.outputContainer.Add(node.OutputPort);
			}

			node.SetPosition(new Rect(position, Vector2.zero));
			node.RefreshExpandedState();
			node.RefreshPorts();

			return node;
		}

		public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
			base.BuildContextualMenu(evt);

			if (TargetNode is BehaviourCategory parentCategory) {
				evt.menu.AppendAction("Add Existing AUtilityNode...", _ => {
					var graphView = this.GetFirstAncestorOfType<UtilityGraphView>();
					graphView?.BeginPickExistingChildNode(parentCategory);
				});
				evt.menu.AppendAction("Create New Category", _ => {
					var graphView = this.GetFirstAncestorOfType<UtilityGraphView>();
					graphView?.CreateChildCategory(parentCategory);
				});
			}
		}
	}
}
