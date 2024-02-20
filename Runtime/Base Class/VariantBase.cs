using System;
using System.Collections.Generic;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;

namespace IndustryCSE.Tool.ProductConfigurator
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
    }
}

