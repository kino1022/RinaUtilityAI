using UnityEditor.Experimental.GraphView;
using UnityEngine;
using RinaUtilityAI.Interface; // AUtilityNodeのネームスペース

namespace RinaUtilityAI.Editor {
	public class UtilityNodeView : Node {
		// ▼ このノードが表現している裏側の実データ（ScriptableObject）
		public AUtilityNode TargetNode { get; private set; }

		// 入力ポートと出力ポートの参照を保持
		public Port InputPort { get; private set; }

		public Port OutputPort { get; private set; }

		public static UtilityNodeView CreateNode(AUtilityNode targetNode, Vector2 position) {
			var node = new UtilityNodeView {
				title = targetNode != null ? targetNode.name : "Unnamed Node",
				TargetNode = targetNode
			};

			// 入力ポート（左側）の作成
			node.InputPort = node.InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
			node.InputPort.portName = "Input";
			node.inputContainer.Add(node.InputPort);

			// 出力ポート（右側）の作成（カテゴリの場合のみ、または共通で持たせる）
			node.OutputPort = node.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
			node.OutputPort.portName = "Child Nodes";
			node.outputContainer.Add(node.OutputPort);

			node.SetPosition(new Rect(position, Vector2.zero));
			node.RefreshExpandedState();
			node.RefreshPorts();

			return node;
		}
	}
}
