using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
namespace RinaUtilityAI.Behaviour {

	public interface IUtilityBehaviour : IEquatable<IUtilityBehaviour>  {

		UniTask BehaviourAction_Async(CancellationToken token, IUtilityBehaviourInstance instance);

		IUtilityBehaviourInstance CreateInstance();

	}

	public interface IUtilityBehaviourInstance : IEquatable<IUtilityBehaviourInstance> {

		void Initialize(UtilityOwnerReference ownerReference);

		IUtilityBehaviour BehaviourDefinition { get; }

		bool IsInterruptible { get; }

		int InterruptPriority { get; }

	}

	public abstract class AUtilityBehaviour : SerializedScriptableObject, IUtilityBehaviour {

		public UniTask BehaviourAction_Async(CancellationToken token, IUtilityBehaviourInstance instance) {
			Assert.IsNotNull(instance);
			Assert.IsNotNull(instance.BehaviourDefinition);
			if (!Equals(instance.BehaviourDefinition)) {
				return UniTask.CompletedTask;
			}
			return BehaviourAction_Async_Implementation(token, instance);
		}

		public abstract IUtilityBehaviourInstance CreateInstance();

		public bool Equals(IUtilityBehaviour other) {
			return GetHashCode() == other.GetHashCode();
		}

		protected abstract UniTask BehaviourAction_Async_Implementation (CancellationToken token, IUtilityBehaviourInstance instance);
	}

	[Serializable]
	public abstract class AUtilityBehaviourInstance {

		[OdinSerialize]
		protected IUtilityBehaviour behaviourDefinition;

		protected bool isInterruptible = false;

		protected int interruptPriority = 0;

		protected UtilityOwnerReference reference;

		public virtual IUtilityBehaviour BehaviourDefinition => behaviourDefinition;

		public virtual bool IsInterruptible => isInterruptible;

		public virtual int InterruptPriority => interruptPriority;

		public void Initialize(UtilityOwnerReference ownerReference) {

		}
	}
}
