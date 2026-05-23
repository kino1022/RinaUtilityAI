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
		private IUtilityBehaviour _defaultBehaviourDefinition;

		[OdinSerialize]
		[ReadOnly]
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
			Func<IObjectResolver, UtilityOwnerReference> refFactory;
			_resolver.TryResolve(out refFactory);
			if (refFactory != null) {
				_ownerReference = refFactory.Invoke(_resolver);
			}
			_ownerReference ??= new UtilityOwnerReference(gameObject, _resolver);
			Assert.IsNotNull(_ownerReference);
			Assert.IsNotNull(_defaultBehaviourDefinition);
			_defaultBehaviour = _defaultBehaviourDefinition.CreateInstance() as IUtilityBehaviourInstance;
			Assert.IsNotNull(_defaultBehaviour);
			_defaultBehaviour.Initialize(_ownerReference);
		}
	}
}
