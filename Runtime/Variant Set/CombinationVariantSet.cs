using System;
using System.Collections;
using System.Collections.Generic;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;
using UnityEngine;
using System.Linq;

namespace IndustryCSE.Tool.ProductConfigurator
{
    [Serializable]
    public class CombinationVariant : VariantBase {}
    
    public class CombinationVariantSet : VariantSetBase
    {
        public VariantSetBase[] VariantSets => variantSets;
    
        [SerializeField]
        protected VariantSetBase[] variantSets;
    
        public List<CombinationVariant> Variants => variants;
    
        [SerializeField]
        protected List<CombinationVariant> variants;
        
        public override List<VariantBase> VariantBase => Variants.Cast<VariantBase>().ToList();

        protected override void OnVariantChanged(VariantBase variantBase, bool triggerConditionalVariants)
        {
            if(!Variants.Contains(variantBase)) return;
            //find index
            var index = Variants.FindIndex(x => x == variantBase);
            foreach (var variantSet in variantSets)
            {
                variantSet.SetVariant(index, triggerConditionalVariants);
            }
            base.OnVariantChanged(variantBase, triggerConditionalVariants);
        }

        public override void SetVariant(int value, bool triggerConditionalVariants)
        {
            if(value < 0 || value >= Variants.Count) return;
            foreach (var variantSet in variantSets)
            {
                variantSet.SetVariant(value, triggerConditionalVariants);
            }
            base.SetVariant(value, triggerConditionalVariants);
        }

#if UNITY_EDITOR
        public override void AddVariant(VariantAsset variantAsset)
        {
            variants.Add(new CombinationVariant {variantAsset = variantAsset});
        }
#endif
    }
}