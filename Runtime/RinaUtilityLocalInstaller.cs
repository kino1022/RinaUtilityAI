using Sirenix.OdinInspector;
using UnityEngine.Assertions;
using VContainer;
using VContainer.Unity;

namespace RinaUtilityAI {
	public class RinaUtilityLocalInstaller : SerializedMonoBehaviour, IInstaller {

		public void Install(IContainerBuilder builder) {
			Assert.IsNotNull(builder);
			builder
				.RegisterFactory<IObjectResolver, UtilityOwnerReference>(resolver => {
					var ownerRef = new UtilityOwnerReference(gameObject, resolver);
					Assert.IsNotNull(ownerRef);
					return ownerRef;
				});
		}
	}
}
