using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Assertions;
using VContainer;

namespace RinaUtilityAI.Behaviour {

	public interface IDefaultBehaviourHolder {

		IUtilityBehaviourInstance DefaultBehaviour { get; }

	}

	public class DefaultBehaviourHolder : SerializedMonoBehaviour, IDefaultBehaviourHolder {

		[OdinSerialize]
		private IUtilityBehaviourInstance _defaultBehaviour;

		public virtual IUtilityBehaviourInstance DefaultBehaviour => _defaultBehaviour;

		private UtilityOwnerReference _ownerReference;

		private IObjectResolver _resolver;

		[Inject]
		public void Construct(IObjectResolver resolver) {
			Assert.IsNotNull(resolver);
			_resolver = resolver;
		}

		private void Start() {
			Assert.IsNotNull(_resolver);
			_ownerReference = _resolver.Resolve<Func<IObjectResolver,UtilityOwnerReference>>().Invoke(_resolver);
			_ownerReference ??= new UtilityOwnerReference(gameObject, _resolver);
			 Assert.IsNotNull(_ownerReference);
			 if (_defaultBehaviour != null) {
				 _defaultBehaviour.Initialize(_ownerReference);
			 }
		}
	}
}
