using System;
using System.Collections.Generic;
using RinaUtilityAI.Behaviour;
using RinaUtilityAI.Prioritization;

namespace RinaUtilityAI.Category {

	public interface IBehaviourCategory {

		IReadOnlyList<IPrioritizationEntry<IUtilityBehaviour, IUtilityBehaviourInstance>> CategoryBehaviours { get; }

		IBehaviourCategoryInstance CreateInstance();

	}

	public interface IBehaviourCategoryInstance : IEquatable<IBehaviourCategoryInstance> {

		IBehaviourCategory CategoryDefinition { get; }
		
	}

}
