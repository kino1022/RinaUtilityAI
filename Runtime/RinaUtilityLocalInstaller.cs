using RinaUtilityAI.Behaviour;
using RinaUtilityAI.Executor;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine.Assertions;
using VContainer;
using VContainer.Unity;

namespace RinaUtilityAI {
	public class RinaUtilityLocalInstaller : SerializedMonoBehaviour, IInstaller {

		[OdinSerialize]
		private IDefaultBehaviourHolder _defaultBehaviour;

		[OdinSerialize]
		private IUtilityBehaviourExecutor _behaviourExecutor;

		public void Install(IContainerBuilder builder) {
			Assert.IsNotNull(builder);

			builder
				.RegisterFactory<IObjectResolver, UtilityOwnerReference>(resolver => {
					var ownerRef = new UtilityOwnerReference(gameObject, resolver);
					Assert.IsNotNull(ownerRef);
					return ownerRef;
				});

			_defaultBehaviour ??= gameObject.GetComponentInChildren<IDefaultBehaviourHolder>();
			Assert.IsNotNull(_defaultBehaviour);
			builder
				.RegisterInstance(_defaultBehaviour)
				.As<IDefaultBehaviourHolder>();

			_behaviourExecutor ??= gameObject.GetComponentInChildren<IUtilityBehaviourExecutor>();
			Assert.IsNotNull(_behaviourExecutor);
			builder
				.RegisterInstance(_behaviourExecutor)
				.As<IUtilityBehaviourExecutor>();
		}
	}
}
