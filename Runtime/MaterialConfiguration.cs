using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace IndustryCSE.Tool.ProductConfigurator
{
    [Serializable]
    public class MaterialOptionDetail : OptionDetailBase
    {
        public Material optionMaterial;
    }

    [Serializable]
    public class RendererDetail
    {
        public Renderer renderer;
        public int materialsSlotIndex;
    }

    public class MaterialConfiguration : ConfigurationBase
    {
        public List<MaterialOptionDetail> OptionDetails => optionDetails;

        [SerializeField]
        protected List<MaterialOptionDetail> optionDetails = new ();
    
        [SerializeField] public RendererDetail[] renderersDetails;
        
        public override List<OptionDetailBase> Options => OptionDetails.Cast<OptionDetailBase>().ToList();
    
        public RendererDetail[] RenderersDetails
        {
            get => renderersDetails;
            set => renderersDetails = value;
        }

        public string CurrentSelectionGuid => optionDetails.FirstOrDefault(x => x.optionMaterial == renderersDetails[0].renderer.sharedMaterials[renderersDetails[0].materialsSlotIndex])?.configurationOption.UniqueIdString;
    
        public int CurrentSelectionCost => optionDetails.FirstOrDefault(x => x.optionMaterial == renderersDetails[0].renderer.sharedMaterials[renderersDetails[0].materialsSlotIndex]).configurationOption.additionalCost;
        
        protected override void OnOptionChanged(OptionDetailBase optionDetailBase)
        {
            if (optionDetailBase is not MaterialOptionDetail materialFeatureDetails) return;
            if (!OptionDetails.Contains(materialFeatureDetails)) return;
            foreach (var renderer in renderersDetails)
            {
                var newMaterials = new Material[renderer.renderer.sharedMaterials.Length];
                Array.Copy(renderer.renderer.sharedMaterials, newMaterials, renderer.renderer.sharedMaterials.Length);
                newMaterials[renderer.materialsSlotIndex] = materialFeatureDetails.optionMaterial;
                renderer.renderer.sharedMaterials = newMaterials;
            }
        }
    
        public override void SetOption(int value)
        {
            foreach (var renderer in renderersDetails)
            {
                var newMaterials = new Material[renderer.renderer.sharedMaterials.Length];
                Array.Copy(renderer.renderer.sharedMaterials, newMaterials, renderer.renderer.sharedMaterials.Length);
                newMaterials[renderer.materialsSlotIndex] = optionDetails[value].optionMaterial;
                renderer.renderer.sharedMaterials = newMaterials;
            }
        }
    }
}