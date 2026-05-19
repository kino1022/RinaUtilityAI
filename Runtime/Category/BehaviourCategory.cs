using System;
using System.Collections.Generic;
using NUnit.Framework;
using RinaUtilityAI.Behaviour;
using RinaUtilityAI.Interface;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
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
		protected IDefaultBehaviourHolder defaultBehaviour;

		[OdinSerialize]
		[ReadOnly]
		[TitleGroup("Runtime Reference")]
		protected List<IUtilityNodeInstance> childNodeInstances = new();

		public IDefaultBehaviourHolder DefaultBehaviour => defaultBehaviour;

		public virtual IReadOnlyList<IUtilityNodeInstance> ChildNodeInstances => childNodeInstances;

		public BehaviourCategoryInstance(BehaviourCategory definition) : base(definition) {
			InstanceChildNodes();
		}

		protected override void OnPostInitialize() {
			base.OnPostInitialize();
			Assert.IsNotNull(ownerRef);
			defaultBehaviour = ownerRef.Resolver.Resolve<IDefaultBehaviourHolder>();
			InitializeChildNodeInstances();
		}

		protected virtual void InitializeChildNodeInstances() {
			Assert.IsNotNull(definition);
			if (childNodeInstances.Count == 0) {
				return;
			}
			foreach (var node in childNodeInstances) {
				if (node != null && !node.IsInitialized) {
					node.Initialize(ownerRef);
				}
			}
		}

		protected void InstanceChildNodes() {
			Assert.IsNotNull(definition);
			Assert.IsFalse(definition is IBehaviourCategory == false, "definition must be of type BehaviourCategory");
			if (definition is IBehaviourCategory category) {
				if (category.ChildNodes.Count == 0) {
					return;
				}
				foreach (var node in category.ChildNodes) {
					if (node != null) {
						childNodeInstances.Add(node.CreateInstance());
					}
				}
			} else {
				throw new NullReferenceException();
			}
		}
	}
}
