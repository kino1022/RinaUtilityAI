using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using R3;
using RinaUtilityAI.Category;
using RinaUtilityAI.Prioritization;
using VContainer;

namespace RinaUtilityAI.Behaviour {

	public class BehaviourPrioritizationEntry : APrioritizationEntry<IUtilityBehaviour, IUtilityBehaviourInstance> {
		protected override IUtilityBehaviourInstance CreateBehaviourInstance() {
			return item.CreateInstance();
		}
	}

	public class BehaviourPrioritizationInstance : PrioritizationInstance<IUtilityBehaviourInstance>, IEquatable<BehaviourPrioritizationInstance> {

		public BehaviourPrioritizationInstance(List<IPrioritizationLogicInstance> logics, IUtilityBehaviourInstance item) : base(logics, item) { }

		protected override void BehaviourInitialize(UtilityOwnerReference reference) {
			base.BehaviourInitialize(reference);
			item.Initialize(reference);
		}

		public bool Equals(BehaviourPrioritizationInstance other) {
			if (other == null) return false;
			return item.BehaviourDefinition.Equals(other.Item.BehaviourDefinition);
		}

	}

	public class BehaviourPrioritizationMachine : APrioritizationMachine<IUtilityBehaviour, IUtilityBehaviourInstance> {

		private ICurrentCategoryHolder _categoryHolder;

		public CancellationTokenSource BehaviourCancellationTokenSource { get; protected set; }

		protected override void OnPostStart() {
			base.OnPostStart();
			Assert.IsNotNull(_resolver);
			_categoryHolder = _resolver.Resolve<ICurrentCategoryHolder>();
			Assert.IsNotNull(_categoryHolder);

			_categoryHolder
				.CurrentCategory
				.Subscribe(next => {
				})
				.AddTo(this);
		}

		protected virtual void OnCategoryChange(IBehaviourCategoryInstance nextInstance) {

		}

		protected UniTask SetNewBehaviour_async(CancellationToken token, BehaviourPrioritizationInstance instance) {
			try {
				if (instance == null) {
					return UniTask.CompletedTask;
				}
				currentInstance.Value = instance;
				var cts = new CancellationTokenSource();
				var cancelToken = cts.Token;
				instance.Item.BehaviourDefinition.BehaviourAction_Async(cancelToken, instance.Item).Forget();
				BehaviourCancellationTokenSource.Cancel();
				BehaviourCancellationTokenSource.Dispose();
				BehaviourCancellationTokenSource = cts;
				return UniTask.CompletedTask;
			} catch (OperationCanceledException) {

			}
			return UniTask.CompletedTask;
		}
		protected override async UniTask PrioritizationLoop_Async(CancellationToken token) {
			try {
				while (!token.IsCancellationRequested) {
					token.ThrowIfCancellationRequested();
					if (currentInstance.CurrentValue != null) {
						var instance = currentInstance.CurrentValue.Item;
						//割り込み不可であるなら次のフレームまで判定を待つ
						if (!instance.IsInterruptible) {
							await UniTask.Yield(cancellationToken: token);
							continue;
						}
						var next = GetMostPriorityItem();
						if (next == null) {
							//次の行動がnullなら次のフレームを待つ
							await UniTask.Yield(cancellationToken: token);
							continue;
						}
						if (next.Item != instance) {
							if (next.Item.InterruptPriority > instance.InterruptPriority) {
								await SetNewBehaviour_async(token, next);
								continue;
							}
						}
					}
					await UniTask.Delay(TimeSpan.FromSeconds(prioritizationInterval), cancellationToken: token);
				}
			} catch (OperationCanceledException) {

			}
		}
	}
}
