using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using RinaUtilityAI.Interface;

namespace RinaUtilityAI.Behaviour {

	public interface IUtilityBehaviour : IUtilityNode {

		int InterruptionPriority { get; }

		UniTask ExecuteBehaviour_Async(CancellationToken token, IUtilityBehaviourInstance instance);

	}

	public interface IUtilityBehaviourInstance : IUtilityNodeInstance, IEquatable<IUtilityBehaviourInstance> {

		new IUtilityBehaviour Definition { get; }

		bool IsActive { get; }

		bool IsInterruptible { get; }

		bool IEquatable<IUtilityBehaviourInstance>.Equals(IUtilityBehaviourInstance other) => other != null && Definition.Equals(other.Definition);

	}

	public abstract class AUtilityBehaviour : AUtilityNode {

		public abstract int InterruptionPriority { get; }

		public abstract UniTask ExecuteBehaviour_Async(CancellationToken token, IUtilityBehaviourInstance instance);

	}

	public abstract class AUtilityBehaviourInstance : AUtilityNodeInstance, IUtilityBehaviourInstance {

		protected AUtilityBehaviourInstance(AUtilityNode definition) : base(definition) {  }

		public abstract bool IsActive { get; }

		public abstract bool IsInterruptible { get; }

		public new IUtilityBehaviour Definition => (IUtilityBehaviour)base.Definition;

	}

}
