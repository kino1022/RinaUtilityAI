using R3;
using RinaUtilityAI.Prioritization;

namespace RinaUtilityAI.Category {

	public interface ICurrentCategoryHolder {

		ReadOnlyReactiveProperty<IBehaviourCategoryInstance> CurrentCategory { get; }

	}

	public class CategoryPrioritizationMachine : APrioritizationMachine<IBehaviourCategory, IBehaviourCategoryInstance>, ICurrentCategoryHolder {

		public virtual ReadOnlyReactiveProperty<IBehaviourCategoryInstance> CurrentCategory => currentInstance;

	}
}
