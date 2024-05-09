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
        protected List<CombinationVariant> variants = new();
        
        public override List<VariantBase> VariantBase => Variants.Cast<VariantBase>().ToList();

        public override int CurrentSelectionIndex
        {
            get
            {
                if(VariantSets.All(item => item.CurrentSelectionIndex == variantSets[0].CurrentSelectionIndex))
                {
                    return variantSets[0].CurrentSelectionIndex;
                }
                else
                {
                    return -1;
                }
            }
        }

        public override string CurrentSelectionGuid => CurrentSelectionIndex == -1 ? string.Empty : Variants[CurrentSelectionIndex].variantAsset.UniqueIdString;
        
        public override int CurrentSelectionCost => CurrentSelectionIndex == -1 ? 0 : Variants[CurrentSelectionIndex].variantAsset.additionalCost;

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
        public override VariantAsset CreateVariantAsset<T>(string variantName, T variantObject)
        {
            var variantAsset = CreateVariantAsset(variantName);
            return variantAsset;
        }

        public override void AddVariant(VariantAsset variantAsset)
        {
            variants.Add(new CombinationVariant {variantAsset = variantAsset});
        }

        public override void AddVariant<T>(VariantAsset variantAsset, T variantObject)
        {
            AddVariant(variantAsset);
        }
#endif
    }
}