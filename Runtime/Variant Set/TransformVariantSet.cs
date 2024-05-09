using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;

namespace IndustryCSE.Tool.ProductConfigurator
{
    [Serializable]
    public class TransformVariant : VariantBase
    {
        public Transform VariantTransform;
    }

    public class TransformVariantSet : VariantSetBase
    {
        public List<TransformVariant> Variants => variants;
        public GameObject gameObjectToMove;

        [SerializeField]
        protected List<TransformVariant> variants = new ();
        
        public override int CurrentSelectionIndex => Variants.FindIndex(x => x.VariantTransform.position==gameObjectToMove.transform.position && x.VariantTransform.rotation == gameObjectToMove.transform.rotation);

        public override string CurrentSelectionGuid => Variants[CurrentSelectionIndex].variantAsset.UniqueIdString;
    
        public override int CurrentSelectionCost => Variants[CurrentSelectionIndex].variantAsset.additionalCost;

        public override List<VariantBase> VariantBase => Variants.Cast<VariantBase>().ToList();

        protected override void OnVariantChanged(VariantBase variantBase, bool triggerConditionalVariants)
        {
            if (variantBase is not TransformVariant featureDetails) return;
            gameObjectToMove.transform.SetPositionAndRotation(featureDetails.VariantTransform.position, featureDetails.VariantTransform.rotation);
            base.OnVariantChanged(variantBase, triggerConditionalVariants);
        }
    
        public override void SetVariant(int value, bool triggerConditionalVariants)
        {
            if(value < 0 || value >= Variants.Count) return;
            gameObjectToMove.transform.SetPositionAndRotation(variants[value].VariantTransform.position, variants[value].VariantTransform.rotation);
            base.SetVariant(value, triggerConditionalVariants);
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// Create a new variant with a Transform object
        /// </summary>
        /// <param name="variantName">Variant Name</param>
        /// <param name="variantObject">Assign Transform</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public override VariantAsset CreateVariantAsset<T>(string variantName, T variantObject)
        {
            if (variantObject == null)
            {
                throw new ArgumentException("variantObject cannot be null");
            }
            
            if (!(variantObject is Transform))
            {
                throw new ArgumentException("variantObject must be a Transform");
            }
            
            var variantAsset = CreateVariantAsset(variantName);
            AssignVariantObject(variantAsset.UniqueIdString, variantObject as Transform);

            return variantAsset;
        }

        public override void AddVariant(VariantAsset variantAsset)
        {
            variants.Add(new TransformVariant {variantAsset = variantAsset});
        }

        /// <summary>
        /// Add a new variant with a Transform object
        /// </summary>
        /// <param name="variantName">Variant Name</param>
        /// <param name="variantObject">Assign Transform</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public override void AddVariant<T>(VariantAsset variantAsset, T variantObject)
        {
            if (variantObject == null)
            {
                throw new ArgumentException("variantObject cannot be null");
            }
            
            if (!(variantObject is Transform))
            {
                throw new ArgumentException("variantObject must be a Transform");
            }
            
            AddVariant(variantAsset);
            AssignVariantObject(variantAsset.UniqueIdString, variantObject as Transform);
        }

        private void AssignVariantObject(string id, Transform targetTransform)
        {
            var targetVariant = VariantBase.Find(x => string.Equals(x.variantAsset.UniqueIdString, id));
            if (targetVariant != null)
            {
                ((TransformVariant)targetVariant).VariantTransform = targetTransform;
            }
        }
#endif
    }
}

