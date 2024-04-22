using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;

namespace IndustryCSE.Tool.ProductConfigurator
{
    [Serializable]
    public class GameObjectVariant : VariantBase
    {
        public GameObject VariantGameObject;
    }

    public class GameObjectVariantSet : VariantSetBase
    {
        public List<GameObjectVariant> Variants => variants;

        [SerializeField]
        protected List<GameObjectVariant> variants = new ();

        public override int CurrentSelectionIndex => Variants.FindIndex(x => x.VariantGameObject.activeSelf);
        
        public override string CurrentSelectionGuid => Variants[CurrentSelectionIndex].variantAsset.UniqueIdString;
    
        public override int CurrentSelectionCost => Variants[CurrentSelectionIndex].variantAsset.additionalCost;

        public override List<VariantBase> VariantBase => Variants.Cast<VariantBase>().ToList();

        protected override void OnVariantChanged(VariantBase variantBase, bool triggerConditionalVariants)
        {
            if (variantBase is not GameObjectVariant featureDetails) return;
            if(Variants.Contains(featureDetails))
            {
                Variants.ForEach(x => x.VariantGameObject.SetActive(featureDetails.VariantGameObject == x.VariantGameObject));
            }
            base.OnVariantChanged(variantBase, triggerConditionalVariants);
        }
    
        public override void SetVariant(int value, bool triggerConditionalVariants)
        {
            if(value < 0 || value >= Variants.Count) return;
            Variants.ForEach(x => x.VariantGameObject.SetActive(false));
            variants[value].VariantGameObject.SetActive(true);
            base.SetVariant(value, triggerConditionalVariants);
        }
        
#if UNITY_EDITOR
        public override void AddVariant(VariantAsset variantAsset)
        {
            variants.Add(new GameObjectVariant {variantAsset = variantAsset});
        }
#endif
    }
}

