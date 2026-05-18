using System;
using Sirenix.OdinInspector;

namespace RinaUtilityAI.Prioritization {

	public interface IPrioritizationLogic<TInstanceType> : IEquatable<IPrioritizationLogic<TInstanceType>> where TInstanceType : IPrioritizationLogicInstance {

		PrioritizationScore Prioritization(TInstanceType instance);

		TInstanceType CreateInstance();

	}

	public interface IPrioritizationLogicInstance {

		void Initialize(UtilityOwnerReference ownerReference);

		IPrioritizationLogic<IPrioritizationLogicInstance> LogicDefinition { get; }

	}

	public abstract class APrioritizationLogic : SerializedScriptableObject, IPrioritizationLogic<IPrioritizationLogicInstance> {

		public abstract PrioritizationScore Prioritization(IPrioritizationLogicInstance instance);

		public abstract IPrioritizationLogicInstance CreateInstance();

		public bool Equals(IPrioritizationLogic<IPrioritizationLogicInstance> other) {
			if (other == null) return false;
			return GetHashCode() == other.GetHashCode();
		}
	}
}
