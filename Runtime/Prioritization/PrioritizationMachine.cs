using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using R3;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using VContainer;

namespace RinaUtilityAI.Prioritization {
	public abstract class APrioritizationMachine<TItemType, TInstanceType> : SerializedMonoBehaviour where TInstanceType : IEquatable<TInstanceType> {

		[OdinSerialize]
		protected List<IPrioritizationEntry<TItemType, TInstanceType>> prioritizationEntries = new();

		[OdinSerialize]
		[ReadOnly]
		protected List<IPrioritizationInstance<TInstanceType>> instances = new();

		protected ReactiveProperty<TInstanceType> currentInstance = new();

		public float prioritizationInterval = 3.0f;

		public bool categoryCancelable = true;

		private UtilityOwnerReference _ownerReference;

		protected IObjectResolver _resolver;

		[Inject]
		public void Construct(IObjectResolver resolver) {
			Assert.IsNotNull(resolver);
			_resolver = resolver;
		}

		private void Start() {
			Assert.IsNotNull(_resolver);
			_ownerReference = _resolver.Resolve<UtilityOwnerReference>();
			Assert.IsNotNull(_ownerReference);
			OnPreStart();
			instances.Clear();
			for (int i = 0; i < instances.Count; i++) {
				var instance = prioritizationEntries[i];
				if (instance == null) {
					prioritizationEntries.RemoveAt(i);
					continue;
				}
				instances.Add(instance.CreateInstance());
			}
			if (instances.Count != 0) {
				foreach (var instance in instances) {
					if (instance == null) {
						continue;
					}
					instance.Initialize(_ownerReference);
				}
			}
			PrioritizationLoop_Async(this.GetCancellationTokenOnDestroy()).Forget();
			OnPostStart();
		}

		protected virtual async UniTask PrioritizationLoop_Async(CancellationToken token) {
			try {
				while (!token.IsCancellationRequested) {
					token.ThrowIfCancellationRequested();
					var item = GetMostPriorityItem();
					if (item == null) {
						Debug.LogWarning("優先度計算で判定するアイテムが存在しませんでした");
						await UniTask.Delay(TimeSpan.FromSeconds(prioritizationInterval), cancellationToken: token);
						continue;
					}
					if (currentInstance != null) {
						if (!currentInstance.CurrentValue.Equals(item)) {
							if (categoryCancelable) {
								await UniTask.Delay(TimeSpan.FromSeconds(prioritizationInterval), cancellationToken: token);
								continue;
							}
						}
					}
					currentInstance.Value = item;
					await UniTask.Delay(TimeSpan.FromSeconds(prioritizationInterval), cancellationToken: token);
				}
			} catch (OperationCanceledException) {

			}
		}

		protected virtual TInstanceType GetMostPriorityItem() {
			if (instances.Count == 0) {
				return default;
			}
			var result = instances[0].Item;
			var mostScore = new PrioritizationScore();
			foreach (var instance in instances) {
				if (instance == null) {
					continue;
				}
				var score = new PrioritizationScore();
				foreach (var logic in instance.Logics) {
					if (logic == null) {
						continue;
					}
					score += logic.LogicDefinition.Prioritization(logic);
				}
				if (score > mostScore) {
					result = instance.Item;
					mostScore = score;
				}
			}
			return result;
		}

		protected virtual void OnPreStart () {}

		protected virtual void OnPostStart () {}
	}
}
