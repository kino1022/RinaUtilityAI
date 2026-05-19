namespace RinaUtilityAI.Interface {
	/// <summary>
	/// インスタンスに対してその評価を行えるクラスのインターフェース
	/// </summary>
	/// <typeparam name="TInstanceType"></typeparam>
	public interface IEvaluatable<out TInstanceType> {

		void Initialize(UtilityOwnerReference ownerRef);

		PrioritizationScore Evaluate();

		TInstanceType GetInstance();

	}
}
