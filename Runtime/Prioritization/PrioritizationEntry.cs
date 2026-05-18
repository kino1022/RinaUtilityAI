using System;
using System.Collections.Generic;
using NUnit.Framework;
using RinaUtilityAI.Behaviour;
using Sirenix.Serialization;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Serialization;

namespace RinaUtilityAI.Prioritization {

	/// <summary>
	/// 優先度をもとに実行を判定されるオブジェクトの定義を管理するクラスに対して約束するインターフェース
	/// </summary>
	/// <typeparam name="TItemType">実行する行動の定義</typeparam>
	/// <typeparam name="TInstanceType">実行する行動の実態の型</typeparam>
	public interface IPrioritizationEntry<out TItemType, out TInstanceType> {

		IReadOnlyList<IPrioritizationLogic<IPrioritizationLogicInstance>> Logics { get; }

		TItemType Item { get; }

		IPrioritizationInstance<TInstanceType> CreateInstance();

	}

	/// <summary>
	/// 優先度をもとに実行を判定されるオブジェクトの実体に対して約束するインターフェース
	/// </summary>
	/// <typeparam name="TBehaviourInstance"></typeparam>
	public interface IPrioritizationInstance<out TBehaviourInstance> {

		void Initialize(UtilityOwnerReference ownerReference);

		bool DidInitialized { get; }

		IReadOnlyList<IPrioritizationLogicInstance> Logics { get; }

		TBehaviourInstance Item { get; }

	}

	[Serializable]
	public abstract class APrioritizationEntry<TItemType, TInstanceType> : IPrioritizationEntry<TItemType, TInstanceType> {

		[SerializeField]
		protected List<APrioritizationLogic> logics;

		[FormerlySerializedAs("behaviour")]
		[SerializeField]
		protected TItemType item;

		public virtual IReadOnlyList<IPrioritizationLogic<IPrioritizationLogicInstance>> Logics => logics;

		public virtual TItemType Item => item;

		public virtual IPrioritizationInstance<TInstanceType> CreateInstance() {
			var logicInstances = new List<IPrioritizationLogicInstance>();
			foreach (var logic in logics) {
				if (logic == null) continue;
				logicInstances.Add(logic.CreateInstance());
			}
			var behaviourInstance = CreateBehaviourInstance();
			return new PrioritizationInstance<TInstanceType>(logicInstances, behaviourInstance);
		}

		protected abstract TInstanceType CreateBehaviourInstance();

	}

	[Serializable]
	public class PrioritizationInstance<TItemType> : IPrioritizationInstance<TItemType> {

		[OdinSerialize]
		protected readonly List<IPrioritizationLogicInstance> logics;

		[OdinSerialize]
		protected readonly TItemType item;

		[SerializeField]
		protected bool didInitialized = false;

		public virtual IReadOnlyList<IPrioritizationLogicInstance> Logics => logics;

		public virtual TItemType Item => item;

		public virtual bool DidInitialized => didInitialized;

		public PrioritizationInstance(List<IPrioritizationLogicInstance> logics, TItemType behaviour) {
			this.logics = logics;
			this.item = behaviour;
		}

		public void Initialize(UtilityOwnerReference ownerReference) {
			Assert.IsNotNull(ownerReference);
			foreach (var logic in logics) {
				if (logic == null) continue;
				logic.Initialize(ownerReference);
			}
			BehaviourInitialize(ownerReference);
			didInitialized = true;
		}

		protected virtual void BehaviourInitialize(UtilityOwnerReference reference) { }
	}
}
