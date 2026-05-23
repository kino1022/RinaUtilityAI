using RinaUtilityAI.Behaviour;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine.Assertions;
using VContainer;
using VContainer.Unity;

namespace RinaUtilityAI {
	public class RinaUtilityLocalInstaller : SerializedMonoBehaviour, IInstaller {

		[OdinSerialize]
		private IDefaultBehaviourHolder _defaultBehaviour;

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
				.As<IUtilityBehaviourInstance>();
			
		}
	}
}
