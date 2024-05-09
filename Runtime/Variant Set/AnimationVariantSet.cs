using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;

namespace IndustryCSE.Tool.ProductConfigurator
{
    [Serializable]
    public class AnimationVariant : VariantBase
    {
        public string VariantState;
        
        public int Hash => Animator.StringToHash(VariantState);
    }
    
    public class AnimationVariantSet : VariantSetBase
    {
        public List<AnimationVariant> Variants => variants;
        public Animator animator;
        
        [SerializeField]
        protected List<AnimationVariant> variants = new ();
        
        public override int CurrentSelectionIndex => Variants.FindIndex(x => x.Hash == animator.GetCurrentAnimatorStateInfo(0).fullPathHash);
        
        public override string CurrentSelectionGuid => Variants[CurrentSelectionIndex].variantAsset.UniqueIdString;
    
        public override int CurrentSelectionCost => Variants[CurrentSelectionIndex].variantAsset.additionalCost;
        
        public override List<VariantBase> VariantBase => Variants.Cast<VariantBase>().ToList();
    
        protected override void OnVariantChanged(VariantBase variantBase, bool triggerConditionalVariants)
        {
            if (variantBase is not AnimationVariant featureDetails) return;
            animator.Play(featureDetails.Hash);
            base.OnVariantChanged(variantBase, triggerConditionalVariants);
        }
    
        public override void SetVariant(int value, bool triggerConditionalVariants)
        {
            if(value < 0 || value >= Variants.Count) return;
            animator.Play(variants[value].Hash);
            base.SetVariant(value, triggerConditionalVariants);
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// Create a new variant asset
        /// </summary>
        /// <param name="variantName">Variant Name</param>
        /// <param name="variantObject">Assign Animator State - must be a string</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public override VariantAsset CreateVariantAsset<T>(string variantName, T variantObject)
        {
            if (variantObject == null)
            {
                throw new ArgumentException("variantObject cannot be null");
            }
            
            if (!(variantObject is string))
            {
                throw new ArgumentException("variantObject must be a string");
            }
            
            var variantAsset = CreateVariantAsset(variantName);
            AssignVariantObject(variantAsset.UniqueIdString, variantObject as string);
            return variantAsset;
        }

        public override void AddVariant(VariantAsset variantAsset)
        {
            variants.Add(new AnimationVariant {variantAsset = variantAsset});
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
        
        /// <summary>
        /// Add a new variant asset
        /// </summary>
        /// <param name="variantName">Variant Name</param>
        /// <param name="variantObject">Assign Animator State - must be a string</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private void AssignVariantObject(string id, string targetState)
        {
            var targetVariant = VariantBase.Find(x => string.Equals(x.variantAsset.UniqueIdString, id));
            if (targetVariant != null)
            {
                ((AnimationVariant)targetVariant).VariantState = targetState;
            }
        }
#endif
    }
}
