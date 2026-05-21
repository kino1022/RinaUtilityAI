using RinaUtilityAI.Category;
using RinaUtilityAI.Interface;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace RinaUtilityAI.Editor {
	public sealed class UtilityNodeView : Node {

		public AUtilityNode TargetNode { get; private set; }

		public bool IsGraphRoot { get; private set; }

		public Port InputPort { get; private set; }

		public Port OutputPort { get; private set; }

		public bool CanHaveChildren => IsGraphRoot || TargetNode is BehaviourCategory;

		public static UtilityNodeView CreateRootNode(Object rootObject, Vector2 position, IEdgeConnectorListener listener) {
			var node = new UtilityNodeView {
				IsGraphRoot = true,
				title = rootObject == null ? "Utility AI Root" : rootObject.name
			};

			node.AddToClassList("utility-ai-root-node");
			node.BuildRootContent(rootObject);
			node.CreateOutputPort("Root Nodes", listener);
			node.SetPosition(new Rect(position, new Vector2(220f, 110f)));
			node.RefreshExpandedState();
			node.RefreshPorts();
			return node;
		}

		public static UtilityNodeView CreateUtilityNode(AUtilityNode targetNode, Vector2 position, IEdgeConnectorListener listener) {
			var node = new UtilityNodeView {
				TargetNode = targetNode,
				title = targetNode == null ? "Missing Node" : targetNode.name
			};

			node.AddToClassList("utility-ai-node");
			node.CreateInputPort();
			node.BuildUtilityContent(targetNode);

			if (targetNode is BehaviourCategory) {
				node.CreateOutputPort("Child Nodes", listener);
			}

			node.SetPosition(new Rect(position, new Vector2(240f, 130f)));
			node.RefreshExpandedState();
			node.RefreshPorts();
			return node;
		}

		private void CreateInputPort() {
			InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(AUtilityNode));
			InputPort.portName = string.Empty;
			inputContainer.Add(InputPort);
		}

		private void CreateOutputPort(string portName, IEdgeConnectorListener listener) {
			OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(AUtilityNode));
			OutputPort.portName = portName;
			OutputPort.AddManipulator(new EdgeConnector<Edge>(listener));
			outputContainer.Add(OutputPort);
		}

		private void BuildRootContent(Object rootObject) {
			var typeLabel = new Label(rootObject is PrioritizationMachine ? "PrioritizationMachine" : "BehaviourCategory");
			typeLabel.AddToClassList("utility-ai-node-type");
			mainContainer.Add(typeLabel);
		}

		private void BuildUtilityContent(AUtilityNode targetNode) {
			if (targetNode == null) {
				var missingLabel = new Label("Missing reference");
				missingLabel.AddToClassList("utility-ai-node-warning");
				mainContainer.Add(missingLabel);
				return;
			}

			var typeLabel = new Label(targetNode.GetType().Name);
			typeLabel.AddToClassList("utility-ai-node-type");
			mainContainer.Add(typeLabel);

			var logicCount = targetNode.PrioritizationLogics == null ? 0 : targetNode.PrioritizationLogics.Count;
			var logicLabel = new Label($"Logics: {logicCount}");
			logicLabel.AddToClassList("utility-ai-node-subtle");
			mainContainer.Add(logicLabel);

			if (targetNode is IBehaviourCategory category) {
				var childCount = category.ChildNodes == null ? 0 : category.ChildNodes.Count;
				var childLabel = new Label($"Children: {childCount}");
				childLabel.AddToClassList("utility-ai-node-subtle");
				mainContainer.Add(childLabel);
			}
		}

		public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
			base.BuildContextualMenu(evt);

			var graphView = this.GetFirstAncestorOfType<UtilityGraphView>();
			if (graphView == null) {
				return;
			}

			evt.menu.AppendAction("Select Asset", _ => {
				if (TargetNode != null) {
					Selection.activeObject = TargetNode;
					EditorGUIUtility.PingObject(TargetNode);
				}
			}, TargetNode == null ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

			if (CanHaveChildren) {
				evt.menu.AppendSeparator();
				evt.menu.AppendAction("Add Existing Node...", _ => graphView.ShowExistingNodeSelectionMenu(this, worldBound.position));
				evt.menu.AppendAction("Create Category", _ => graphView.CreateCategoryChild(this, worldBound.position));
			}
		}
	}
}
