using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;

namespace IndustryCSE.Tool.ProductConfigurator.Runtime
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
    
        [SerializeField]
        private List<RendererDetail> renderersDetails;
        
        public override List<VariantBase> VariantBase => Variants.Cast<VariantBase>().ToList();
    
        public List<RendererDetail> RenderersDetails
        {
            get => renderersDetails;
            set => renderersDetails = value;
        }
        
        public override int CurrentSelectionIndex => Variants.All(x => x.VariantMaterial != null) && renderersDetails is
        {
            Count: > 0
        } && renderersDetails.All(x => x != null) ? Variants.FindIndex(x => x.VariantMaterial == renderersDetails[0].renderer.sharedMaterials[renderersDetails[0].materialsSlotIndex]) : -1;

        public override string CurrentSelectionGuid => CurrentSelectionIndex >= 0 && Variants[CurrentSelectionIndex].variantAsset != null ? Variants[CurrentSelectionIndex].variantAsset.UniqueIdString : string.Empty;

        public override int CurrentSelectionCost => CurrentSelectionIndex >= 0 ? Variants[CurrentSelectionIndex].variantAsset.additionalCost : 0;
        
        private void ApplyVariant(Material material)
        {
            foreach (var detail in renderersDetails)
            {
                var mats = detail.renderer.sharedMaterials;
                mats[detail.materialsSlotIndex] = material;
                detail.renderer.sharedMaterials = mats;
            }
        }

        protected override void OnVariantChanged(VariantBase variantBase, bool triggerConditionalVariants)
        {
            if (variantBase is not MaterialVariant materialFeatureDetails) return;
            if (!Variants.Contains(materialFeatureDetails)) return;
            ApplyVariant(materialFeatureDetails.VariantMaterial);
            base.OnVariantChanged(variantBase, triggerConditionalVariants);
        }

        public override void SetVariant(int value, bool triggerConditionalVariants)
        {
            if(value < 0 || value >= Variants.Count) return;
            ApplyVariant(variants[value].VariantMaterial);
            base.SetVariant(value, triggerConditionalVariants);
        }

        public override void AddVariant(VariantAsset variantAsset)
        {
            var newVariant = new MaterialVariant
            {
                variantAsset = variantAsset
            };
            Variants.Add(newVariant);
        }

        /// <summary>
        /// Add Variant for MaterialVariantSet
        /// </summary>
        /// <param name="variantName">Variant Name</param>
        /// <param name="variantObject">Assign Material</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public override void AddVariant<T>(VariantAsset variantAsset, T variantObject)
        {
            if (variantObject == null)
            {
                throw new ArgumentException("variantObject cannot be null");
            }
            
            if (!(variantObject is Material))
            {
                throw new ArgumentException("variantObject must be a Material");
            }
            
            AddVariant(variantAsset);
            AssignVariantObject(variantAsset.UniqueIdString, variantObject as Material);
        }
        
        public override void AssignVariantObject<T>(string id, T targetMaterial)
        {
            if (targetMaterial == null)
            {
                throw new ArgumentException("targetMaterial cannot be null");
            }
            
            if (!(targetMaterial is Material))
            {
                throw new ArgumentException("targetMaterial must be a Material");
            }
            var targetVariant = VariantBase.Find(x => string.Equals(x.variantAsset.UniqueIdString, id));
            if (targetVariant != null)
            {
                ((MaterialVariant)targetVariant).VariantMaterial = targetMaterial as Material;
            }
        }
    }
}