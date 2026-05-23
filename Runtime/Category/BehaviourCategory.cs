using System;
using System.Collections.Generic;
using RinaUtilityAI.Behaviour;
using RinaUtilityAI.Interface;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Assertions;
using VContainer;

namespace RinaUtilityAI.Category {

	public interface IBehaviourCategory : IUtilityNode {

		IReadOnlyList<IUtilityNode> ChildNodes { get; }

	}

	public interface IBehaviourCategoryInstance : IUtilityNodeInstance {

		IDefaultBehaviourHolder DefaultBehaviour { get; }

		IReadOnlyList<IUtilityNodeInstance> ChildNodeInstances { get; }

	}

	[CreateAssetMenu(menuName = "RinaUtilityAI/Category")]
	public class BehaviourCategory : AUtilityNode, IBehaviourCategory {

		[SerializeField]
		[LabelText("子ノード")]
		protected List<AUtilityNode> childNodes = new();

		public virtual IReadOnlyList<IUtilityNode> ChildNodes => childNodes;

		public override IUtilityBehaviourInstance GetBehaviour(IUtilityNodeInstance nodeInstance) {
			Assert.IsNotNull(nodeInstance);
			Assert.IsFalse(nodeInstance is IBehaviourCategoryInstance == false, "nodeInstance must be of type BehaviourCategoryInstance");
			if (nodeInstance is IBehaviourCategoryInstance behaviour) {
				if (childNodes.Count == 0) {
					Assert.IsNotNull(behaviour.DefaultBehaviour);
					return behaviour.DefaultBehaviour.DefaultBehaviour;
				}

				var score = new PrioritizationScore();
				var mostBehaviour  = behaviour.DefaultBehaviour.DefaultBehaviour;
				foreach (var node in behaviour.ChildNodeInstances) {
					if (node != null && node.Definition != null) {
						var tempScore = node.Definition.Evaluate(node);
						if (tempScore > score) {
							score = tempScore;
							mostBehaviour = node.Definition.GetBehaviour(node);
						}
					}
				}
				return mostBehaviour;
			}
			throw new NullReferenceException();
		}

		public override IUtilityNodeInstance CreateInstance() {
			return new BehaviourCategoryInstance(this);
		}
	}

	public class BehaviourCategoryInstance : AUtilityNodeInstance, IBehaviourCategoryInstance {

		[OdinSerialize]
		[ReadOnly]
		[TitleGroup("Runtime Reference")]
		private IDefaultBehaviourHolder _defaultBehaviour;

		[OdinSerialize]
		[ReadOnly]
		[TitleGroup("Runtime Reference")]
		private List<IUtilityNodeInstance> _childNodeInstances = new();

		public IDefaultBehaviourHolder DefaultBehaviour => _defaultBehaviour;

		public virtual IReadOnlyList<IUtilityNodeInstance> ChildNodeInstances => _childNodeInstances;

		public BehaviourCategoryInstance(BehaviourCategory definition) : base(definition) {
			InstanceChildNodes();
		}

		protected override void OnPostInitialize() {
			base.OnPostInitialize();
			Assert.IsNotNull(OwnerRef);
			OwnerRef.Resolver.TryResolve(out _defaultBehaviour);
			_defaultBehaviour ??= OwnerRef.OwnerObject.GetComponentInParent<IDefaultBehaviourHolder>();
			InitializeChildNodeInstances();
		}

		protected virtual void InitializeChildNodeInstances() {
			Assert.IsNotNull(definition);
			if (_childNodeInstances.Count == 0) {
				return;
			}
			foreach (var node in _childNodeInstances) {
				if (node != null && !node.IsInitialized) {
					node.Initialize(OwnerRef);
				}
			}
		}

		private void InstanceChildNodes() {
			Assert.IsNotNull(definition);
			Assert.IsFalse(definition is IBehaviourCategory == false, "definition must be of type BehaviourCategory");
			if (definition is IBehaviourCategory category) {
				if (category.ChildNodes.Count == 0) {
					return;
				}
				foreach (var node in category.ChildNodes) {
					if (node != null) {
						_childNodeInstances.Add(node.CreateInstance());
					}
				}
			} else {
				throw new NullReferenceException();
			}
		}
	}
}
