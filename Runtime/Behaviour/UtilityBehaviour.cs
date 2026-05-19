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

		IUtilityBehaviour Definition { get; }

		bool IsActive { get; }

		bool IsInterruptible { get; }

		bool IEquatable<IUtilityBehaviourInstance>.Equals(IUtilityBehaviourInstance other) => other != null && Definition.Equals(other.Definition);

	}

}
