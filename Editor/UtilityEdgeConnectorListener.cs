using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace RinaUtilityAI.Editor {
	public sealed class UtilityEdgeConnectorListener : IEdgeConnectorListener {

		private readonly UtilityGraphView graphView;

		public UtilityEdgeConnectorListener(UtilityGraphView graphView) {
			this.graphView = graphView;
		}

		public void OnDropOutsidePort(Edge edge, Vector2 position) {
			if (edge?.output?.node is UtilityNodeView sourceNodeView) {
				graphView.ShowAddChildMenu(sourceNodeView, position);
			}
		}

		public void OnDrop(GraphView graphView, Edge edge) {
			this.graphView.RegisterEdge(edge);
		}
	}
}
