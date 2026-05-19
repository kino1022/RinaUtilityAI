using System.Threading;
using Cysharp.Threading.Tasks;
using RinaUtilityAI.Behaviour;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace RinaUtilityAI.Executor {

	public interface IUtilityBehaviourExecutor {

		IUtilityBehaviourInstance CurrentBehaviour { get; }

		bool TryExecuteBehaviour(IUtilityBehaviourInstance instance);

	}

	public class UtilityBehaviourExecutor : SerializedMonoBehaviour {

		[OdinSerialize]
		[LabelText("現在の行動")]
		private IUtilityBehaviourInstance currentBehaviour;

		public IUtilityBehaviourInstance CurrentBehaviour => currentBehaviour;

		private CancellationTokenSource _behaviourCts = new();

		public bool TryExecuteBehaviour(IUtilityBehaviourInstance instance) {
			if (instance == null) {
				return false;
			}

			if (!instance.IsInitialized || !instance.IsActive) {
				return false;
			}

			if (currentBehaviour == null) {
				SwitchBehaviour_Async(instance).Forget();
				return true;
			}

			if (!currentBehaviour.IsInterruptible) {
				return false;
			}

			if (instance.Definition.InterruptionPriority >= currentBehaviour.Definition.InterruptionPriority) {
				SwitchBehaviour_Async(instance).Forget();
				return true;
			}
			return false;
		}

		protected UniTask SwitchBehaviour_Async(IUtilityBehaviourInstance nextBehaviour) {
			_behaviourCts.Cancel();
			_behaviourCts.Dispose();
			_behaviourCts = new CancellationTokenSource();
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(_behaviourCts.Token, _behaviourCts.Token);
			currentBehaviour = nextBehaviour;
			currentBehaviour.Definition.ExecuteBehaviour_Async(cts.Token, currentBehaviour).Forget();
			return UniTask.CompletedTask;
		}

	}
}
