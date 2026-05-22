using System;
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

	public class UtilityBehaviourExecutor : SerializedMonoBehaviour, IUtilityBehaviourExecutor {

		[OdinSerialize]
		[LabelText("現在の行動")]
		private IUtilityBehaviourInstance _currentBehaviour;

		public IUtilityBehaviourInstance CurrentBehaviour => _currentBehaviour;

		private CancellationTokenSource _behaviourCts = new();

		public bool TryExecuteBehaviour(IUtilityBehaviourInstance instance) {
			if (instance == null) {
				return false;
			}

			if (!instance.IsInitialized || instance.IsActive) {
				return false;
			}

			if (_currentBehaviour == null) {
				SwitchBehaviour_Async(instance).Forget();
				return true;
			}

			if (!_currentBehaviour.IsInterruptible) {
				return false;
			}

			if (instance.Definition.InterruptionPriority >= _currentBehaviour.Definition.InterruptionPriority) {
				SwitchBehaviour_Async(instance).Forget();
				return true;
			}
			return false;
		}

		protected virtual UniTask SwitchBehaviour_Async(IUtilityBehaviourInstance nextBehaviour) {
			_behaviourCts.Cancel();
			_behaviourCts.Dispose();
			_behaviourCts = new CancellationTokenSource();
			_currentBehaviour = nextBehaviour;
			ExecuteCurrentBehaviour_Async(nextBehaviour).Forget();
			return UniTask.CompletedTask;
		}

		protected virtual async UniTask ExecuteCurrentBehaviour_Async(IUtilityBehaviourInstance nextBehaviour) {
			try {
				nextBehaviour.IsActive = true;
				await nextBehaviour.Definition.ExecuteBehaviour_Async(_behaviourCts.Token, nextBehaviour);
			} catch (OperationCanceledException) {

			} finally {
				nextBehaviour.IsActive = false;
				_currentBehaviour = null;
			}
		}

	}
}
