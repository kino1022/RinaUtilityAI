using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using RinaUtilityAI.Behaviour;
using RinaUtilityAI.Executor;
using RinaUtilityAI.Interface;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Assertions;
using VContainer;

namespace RinaUtilityAI {
	public class PrioritizationMachine : SerializedMonoBehaviour {

		[OdinSerialize]
		private List<IUtilityNode> nodes = new();

		[OdinSerialize]
		[ReadOnly]
		private List<IUtilityNodeInstance> nodeInstances = new();

		[TitleGroup("Config")]
		[SerializeField]
		[LabelText("評価の間隔(s)")]
		private float evaluationInterval = 1f;

		[TitleGroup("Runtime References")]
		[OdinSerialize]
		[ReadOnly]
		protected IUtilityBehaviourExecutor behaviourExecutor;

		[OdinSerialize]
		[ReadOnly]
		[TitleGroup("Runtime References")]
		protected IDefaultBehaviourHolder defaultBehaviour;

		[SerializeField]
		[ReadOnly]
		[TitleGroup("Runtime References")]
		protected UtilityOwnerReference ownerReference;

		private IObjectResolver _resolver;

		[Inject]
		public void Construct(IObjectResolver resolver) {
			Assert.IsNotNull(resolver);
			_resolver = resolver;
		}

		private void Start() {
			behaviourExecutor = _resolver.Resolve<IUtilityBehaviourExecutor>();
			behaviourExecutor ??= gameObject.transform.root.GetComponentInChildren<IUtilityBehaviourExecutor>();
			Assert.IsNotNull(behaviourExecutor);
			_resolver.TryResolve<IDefaultBehaviourHolder>(out defaultBehaviour);
			defaultBehaviour ??= gameObject.transform.root.GetComponentInChildren<IDefaultBehaviourHolder>();
			Assert.IsNotNull(defaultBehaviour);
			Func<IObjectResolver, UtilityOwnerReference> ownerRefFactory;
			_resolver.TryResolve<Func<IObjectResolver, UtilityOwnerReference>>(out ownerRefFactory);
			if (ownerRefFactory != null) {
				ownerReference = ownerRefFactory.Invoke(_resolver);
			}
			ownerReference ??= new UtilityOwnerReference(gameObject, _resolver);
			Assert.IsNotNull(ownerReference);
			StartupMachine_Async(this.GetCancellationTokenOnDestroy()).Forget();
		}

		private async UniTask StartupMachine_Async(CancellationToken token) {
			Assert.IsNotNull(behaviourExecutor);
			CreateNodeInstances();
			InitializeNodeInstances();
			try {
				await UniTask.WaitUntil(() => behaviourExecutor.CurrentBehaviour != null, cancellationToken: token);
			} catch (OperationCanceledException) {
				this.enabled = false;
			}
			EvaluateLoop_Async(this.GetCancellationTokenOnDestroy()).Forget();
		}

		protected virtual void CreateNodeInstances() {
			if (nodes.Count == 0) {
				return;
			}
			foreach (var node in nodes) {
				if (node != null) {
					nodeInstances.Add(node.CreateInstance());
				}
			}
		}

		protected virtual void InitializeNodeInstances() {
			Assert.IsNotNull(ownerReference);
			foreach (var node in nodeInstances) {
				if (node != null && !node.IsInitialized) {
					node.Initialize(ownerReference);
				}
			}
		}


		private async UniTask EvaluateLoop_Async(CancellationToken token) {
			try {
				while (!token.IsCancellationRequested) {
					if (nodeInstances.Count == 0) {
						behaviourExecutor.TryExecuteBehaviour(defaultBehaviour.DefaultBehaviour);
						await UniTask.Delay(TimeSpan.FromSeconds(evaluationInterval), cancellationToken: token);
						continue;
					}

					var score = new PrioritizationScore();
					var behaviour = defaultBehaviour.DefaultBehaviour;
					foreach (var node in nodeInstances) {
						var tempScore = node.Definition.Evaluate(node);
						if (tempScore > score) {
							score = tempScore;
							behaviour = node.Definition.GetBehaviour(node);
						}
					}
					Assert.IsNotNull(behaviourExecutor);
					behaviourExecutor.TryExecuteBehaviour(behaviour);
					await UniTask.Delay(TimeSpan.FromSeconds(evaluationInterval), cancellationToken: token);
				}
			} catch (OperationCanceledException) {

			}
		}
	}
}
