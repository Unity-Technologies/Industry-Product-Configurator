using System;
using System.Collections.Generic;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;
using UnityEngine;

namespace IndustryCSE.Tool.ProductConfigurator.Runtime
{
    [Serializable]
    public struct ConditionalVariantData
    {
        public VariantSetAsset variantSetAsset;
        public VariantAsset variantAsset;
    }
    
    [Serializable]
    public class VariantBase
    {
        public VariantAsset variantAsset;

        public List<ConditionalVariantData> conditionalVariants;
        
        #if UNITY_EDITOR
        [HideInInspector]
        public bool FoldoutInspector = true;
        #endif
    }
}

