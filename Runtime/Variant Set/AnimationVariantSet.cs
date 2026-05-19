using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;

namespace IndustryCSE.Tool.ProductConfigurator.Runtime
{
    [Serializable]
    public class AnimationVariant : VariantBase
    {
        public string VariantState;

        [NonSerialized] private bool _hashCached;
        [NonSerialized] private int _cachedHash;

        public int Hash
        {
            get
            {
                if (!_hashCached)
                {
                    _cachedHash = Animator.StringToHash(VariantState);
                    _hashCached = true;
                }
                return _cachedHash;
            }
        }
    }

    public class AnimationVariantSet : VariantSetBase
    {
        public List<AnimationVariant> Variants => variants;
        public Animator animator;

        [SerializeField] private int animatorLayerIndex = 0;

        [SerializeField]
        protected List<AnimationVariant> variants = new ();

        public override int CurrentSelectionIndex => animator != null ? Variants.FindIndex(x => x.Hash == animator.GetCurrentAnimatorStateInfo(animatorLayerIndex).fullPathHash) : -1;

        public override string CurrentSelectionGuid => CurrentSelectionIndex >= 0 ? Variants[CurrentSelectionIndex].variantAsset.UniqueIdString : string.Empty;

        public override int CurrentSelectionCost => CurrentSelectionIndex >= 0 ? Variants[CurrentSelectionIndex].variantAsset.additionalCost : 0;

        public override List<VariantBase> VariantBase => Variants.Cast<VariantBase>().ToList();

        private void ApplyVariant(AnimationVariant variant)
        {
            if (animator == null) return;
            animator.Play(variant.Hash, animatorLayerIndex);
        }

        protected override void OnVariantChanged(VariantBase variantBase, bool triggerConditionalVariants)
        {
            if (variantBase is not AnimationVariant featureDetails) return;
            ApplyVariant(featureDetails);
            base.OnVariantChanged(variantBase, triggerConditionalVariants);
        }

        public override void SetVariant(int value, bool triggerConditionalVariants)
        {
            if(value < 0 || value >= Variants.Count) return;
            ApplyVariant(variants[value]);
            base.SetVariant(value, triggerConditionalVariants);
        }

        public override void AddVariant(VariantAsset variantAsset)
        {
            var newVariant = new AnimationVariant
            {
                variantAsset = variantAsset,
                VariantState = string.Empty
            };
            variants.Add(newVariant);
        }

        public override void AddVariant<T>(VariantAsset variantAsset, T variantObject)
        {
            if (variantObject == null)
            {
                throw new ArgumentException("variantObject cannot be null");
            }
            
            if (!(variantObject is string))
            {
                throw new ArgumentException("variantObject must be a string");
            }
            
            AddVariant(variantAsset);
            
            AssignVariantObject(variantAsset.UniqueIdString, variantObject as string);
        }
        
        public override void AssignVariantObject<T>(string id, T targetState)
        {
            if (targetState == null)
            {
                throw new ArgumentException("targetState cannot be null");
            }
            
            if (!(targetState is string))
            {
                throw new ArgumentException("targetState must be a string");
            }
            
            var targetVariant = VariantBase.Find(x => string.Equals(x.variantAsset.UniqueIdString, id));
            if (targetVariant != null)
            {
                ((AnimationVariant)targetVariant).VariantState = targetState as string;
            }
        }
    }
}
