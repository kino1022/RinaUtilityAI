using Sirenix.OdinInspector;

namespace RinaUtilityAI.Logic {
	/// <summary>
	/// 評価値算出のロジックを持つクラスに対して約束するインターフェース
	/// </summary>
	public interface IPrioritizationLogic {

		PrioritizationScore Evaluate(IPrioritizationLogicInstance instance);

		IPrioritizationLogicInstance CreateInstance();

	}

	public interface IPrioritizationLogicInstance {

		IPrioritizationLogic Definition { get; }

		void Initialize(UtilityOwnerReference ownerRef);

		bool IsInitialized { get; }

	}

	public abstract class APrioritizationLogic : SerializedScriptableObject, IPrioritizationLogic {

		public abstract PrioritizationScore Evaluate(IPrioritizationLogicInstance instance);

		public abstract IPrioritizationLogicInstance CreateInstance();

	}
}
