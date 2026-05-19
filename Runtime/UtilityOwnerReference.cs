using System;
using UnityEngine;
using VContainer;

namespace RinaUtilityAI {
	[Serializable]
	public class UtilityOwnerReference {

		public GameObject OwnerObject;

		public IObjectResolver Resolver;

	}
}
