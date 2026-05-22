using System;
using System.Collections.Generic;
using RinaUtilityAI.Behaviour;
using RinaUtilityAI.Logic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Assertions;

namespace RinaUtilityAI.Interface {
	/// <summary>
	/// UtilityAIの行動管理ロジックを構成する要素に対して約束するインターフェース
	/// </summary>
	public interface IUtilityNode {

		PrioritizationScore Evaluate(IUtilityNodeInstance nodeInstance);

		IUtilityBehaviourInstance GetBehaviour(IUtilityNodeInstance nodeInstance);

		IUtilityNodeInstance CreateInstance();

	}

	public interface IUtilityNodeInstance  {

		IUtilityNode Definition { get; }

		IReadOnlyList<IPrioritizationLogicInstance> EvaluateLogics { get; }

		void Initialize(UtilityOwnerReference ownerRef);

		bool IsInitialized { get; }

	}

	public abstract class AUtilityNode : SerializedScriptableObject, IUtilityNode {

		[SerializeField]
		[LabelText("評価ロジック")]
		protected List<APrioritizationLogic> prioritizationLogics = new();

		public virtual IReadOnlyList<APrioritizationLogic> PrioritizationLogics => prioritizationLogics;


		public virtual PrioritizationScore Evaluate(IUtilityNodeInstance nodeInstance) {
			if (nodeInstance == null) {
				return new PrioritizationScore();
			}
			PrioritizationScore result = new();
			if (nodeInstance.EvaluateLogics.Count != 0) {
				foreach (var logic in nodeInstance.EvaluateLogics) {
					if (logic != null && logic.Definition != null) {
						result += logic.Definition.Evaluate(logic);
					}
				}
			}
			return result;
		}

		public abstract IUtilityBehaviourInstance GetBehaviour(IUtilityNodeInstance nodeInstance);

		public abstract IUtilityNodeInstance CreateInstance();

	}

	[Serializable]
	public abstract class AUtilityNodeInstance : IUtilityNodeInstance {

		[SerializeField]
		[LabelText("ノードの定義")]
		[ReadOnly]
		protected AUtilityNode definition;

		[OdinSerialize]
		[LabelText("評価ロジックのインスタンス")]
		[ReadOnly]
		private List<IPrioritizationLogicInstance> _evaluateLogics = new();

		[SerializeField]
		[ReadOnly]
		private bool isInitialized;

		public virtual IReadOnlyList<IPrioritizationLogicInstance> EvaluateLogics => _evaluateLogics;

		public virtual IUtilityNode Definition => definition;

		public virtual bool IsInitialized => isInitialized;

		[SerializeField]
		[ReadOnly]
		private UtilityOwnerReference ownerRef;

		protected UtilityOwnerReference OwnerRef => ownerRef;

		protected AUtilityNodeInstance(AUtilityNode definition) {
			Assert.IsNotNull(definition);
			this.definition = definition;
			CreateLogicInstances();
		}

		public void Initialize(UtilityOwnerReference ownerReference) {
			Assert.IsNotNull(ownerReference);
			ownerRef = ownerReference;
			InitializeLogicInstances();
			OnPostInitialize();
			isInitialized = true;
		}

		protected virtual void OnPostInitialize() {}

		protected void CreateLogicInstances() {
			Assert.IsNotNull(definition);
			_evaluateLogics.Clear();
			if (definition.PrioritizationLogics.Count != 0) {
				foreach (var logic in definition.PrioritizationLogics) {
					if (logic != null) {
						var instance  = logic.CreateInstance();
						_evaluateLogics.Add(instance);
					}
				}
			}
		}

		protected virtual void InitializeLogicInstances() {
			Assert.IsNotNull(ownerRef);
			if (EvaluateLogics.Count != 0) {
				foreach (var logic in EvaluateLogics) {
					if (logic != null) {
						logic.Initialize(ownerRef);
					}
				}
			}
		}
	}
}
