using UnityEditor.Experimental.GraphView;
using UnityEngine;
using RinaUtilityAI.Category;

namespace RinaUtilityAI.Editor {
	public sealed class UtilityEdgeConnectorListener : IEdgeConnectorListener {

		private readonly UtilityGraphView _graphView;

		public UtilityEdgeConnectorListener(UtilityGraphView graphView) {
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

