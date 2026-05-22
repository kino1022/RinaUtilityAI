using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using RinaUtilityAI.Interface;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RinaUtilityAI.Behaviour {

	public interface IUtilityBehaviour : IUtilityNode {

		int InterruptionPriority { get; }

		UniTask ExecuteBehaviour_Async(CancellationToken token, IUtilityBehaviourInstance instance);

	}

	public interface IUtilityBehaviourInstance : IUtilityNodeInstance, IEquatable<IUtilityBehaviourInstance> {

		new IUtilityBehaviour Definition { get; }

		bool IsActive { get; set; }

		bool IsInterruptible { get; set; }

		bool IEquatable<IUtilityBehaviourInstance>.Equals(IUtilityBehaviourInstance other) => other != null && Definition.Equals(other.Definition);

	}

	public abstract class AUtilityBehaviour : AUtilityNode, IUtilityBehaviour {

		[SerializeField]
		[LabelText("割込の優先度")]
		protected int interruptionPriority = 0;

		public virtual int InterruptionPriority => interruptionPriority;

		public abstract UniTask ExecuteBehaviour_Async(CancellationToken token, IUtilityBehaviourInstance instance);

	}

	[Serializable]
	public abstract class AUtilityBehaviourInstance : AUtilityNodeInstance, IUtilityBehaviourInstance {

		[SerializeField]
		[ReadOnly]
		protected new AUtilityBehaviour definition;

		[SerializeField]
		[LabelText("行動がアクティブであるかどうか")]
		protected bool isActive = false;

		public virtual bool IsActive {
			get => isActive;
			set => isActive = value;
		}

		[SerializeField]
		[LabelText("割り込まれることを許容するかどうか")]
		private bool isInterruptible = false;

		public bool IsInterruptible {
			get => isInterruptible;
			set => isInterruptible = value;
		}

		public IUtilityBehaviour Definition => definition;

		protected AUtilityBehaviourInstance(AUtilityBehaviour definition) : base(definition) {
			this.definition = definition;
		}
	}

}
