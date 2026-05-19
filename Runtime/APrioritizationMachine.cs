using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using RinaUtilityAI.Behaviour;
using RinaUtilityAI.Executor;
using RinaUtilityAI.Interface;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
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
			defaultBehaviour = _resolver.Resolve<IDefaultBehaviourHolder>();
			defaultBehaviour ??= gameObject.transform.root.GetComponentInChildren<IDefaultBehaviourHolder>();
			Assert.IsNotNull(defaultBehaviour);
			CreateNodeInstances();
			InitializeNodeInstances();
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
						await UniTask.Yield(token);
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
