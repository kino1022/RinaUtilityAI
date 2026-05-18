using System.Collections.Generic;
using NUnit.Framework;
using RinaUtilityAI.Prioritization;

namespace RinaUtilityAI.Category {
	public class CategoryPrioritizationEntry : APrioritizationEntry<IBehaviourCategory, IBehaviourCategoryInstance> {

		protected override IBehaviourCategoryInstance CreateBehaviourInstance() {
			Assert.IsNotNull(item);
			return item.CreateInstance();
		}

	}

	public class CategoryPrioritizationInstance : PrioritizationInstance<IBehaviourCategoryInstance> {

		public CategoryPrioritizationInstance(List<IPrioritizationLogicInstance> logics, IBehaviourCategoryInstance item) : base(logics, item) { }

	}
}
