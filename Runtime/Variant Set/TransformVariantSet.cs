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

        public string CurrentSelectionGuid => Variants.First(x => x.VariantTransform.position==gameObjectToMove.transform.position
                                                                  && x.VariantTransform.rotation == gameObjectToMove.transform.rotation ).variantAsset.UniqueIdString;
    
        public int CurrentSelectionCost => Variants.First(x => x.VariantTransform.position==gameObjectToMove.transform.position && x.VariantTransform.rotation == gameObjectToMove.transform.rotation).variantAsset.additionalCost;

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
        public override void AddVariant(VariantAsset variantAsset)
        {
            variants.Add(new TransformVariant {variantAsset = variantAsset});
        }
#endif
    }
}

