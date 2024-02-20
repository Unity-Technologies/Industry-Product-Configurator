using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;

namespace IndustryCSE.Tool.ProductConfigurator
{
    [Serializable]
    public class MaterialVariant : VariantBase
    {
        public Material VariantMaterial;
    }

    [Serializable]
    public class RendererDetail
    {
        public Renderer renderer;
        public int materialsSlotIndex;
    }

    public class MaterialVariantSet : VariantSetBase
    {
        public List<MaterialVariant> Variants => variants;

        [SerializeField]
        protected List<MaterialVariant> variants = new ();
    
        [SerializeField] public RendererDetail[] renderersDetails;
        
        public override List<VariantBase> VariantBase => Variants.Cast<VariantBase>().ToList();
    
        public RendererDetail[] RenderersDetails
        {
            get => renderersDetails;
            set => renderersDetails = value;
        }

        public string CurrentSelectionGuid => variants.FirstOrDefault(x => x.VariantMaterial == renderersDetails[0].renderer.sharedMaterials[renderersDetails[0].materialsSlotIndex])?.variantAsset.UniqueIdString;
    
        public int CurrentSelectionCost => variants.FirstOrDefault(x => x.VariantMaterial == renderersDetails[0].renderer.sharedMaterials[renderersDetails[0].materialsSlotIndex]).variantAsset.additionalCost;
        
        protected override void OnVariantChanged(VariantBase variantBase, bool triggerConditionalVariants)
        {
            if (variantBase is not MaterialVariant materialFeatureDetails) return;
            if (!Variants.Contains(materialFeatureDetails)) return;
            foreach (var renderer in renderersDetails)
            {
                var newMaterials = new Material[renderer.renderer.sharedMaterials.Length];
                Array.Copy(renderer.renderer.sharedMaterials, newMaterials, renderer.renderer.sharedMaterials.Length);
                newMaterials[renderer.materialsSlotIndex] = materialFeatureDetails.VariantMaterial;
                renderer.renderer.sharedMaterials = newMaterials;
            }
            base.OnVariantChanged(variantBase, triggerConditionalVariants);
        }
    
        public override void SetVariant(int value, bool triggerConditionalVariants)
        {
            if(value < 0 || value >= Variants.Count) return;
            foreach (var renderer in renderersDetails)
            {
                var newMaterials = new Material[renderer.renderer.sharedMaterials.Length];
                Array.Copy(renderer.renderer.sharedMaterials, newMaterials, renderer.renderer.sharedMaterials.Length);
                newMaterials[renderer.materialsSlotIndex] = variants[value].VariantMaterial;
                renderer.renderer.sharedMaterials = newMaterials;
            }
            base.SetVariant(value, triggerConditionalVariants);
        }
        
#if UNITY_EDITOR
        public override void AddVariant(VariantAsset variantAsset)
        {
            variants.Add(new MaterialVariant {variantAsset = variantAsset});
        }
#endif
    }
}