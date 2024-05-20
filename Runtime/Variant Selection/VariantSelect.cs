using System.Collections;
using System.Collections.Generic;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;
using UnityEngine;

namespace IndustryCSE.Tool.ProductConfigurator.Runtime
{
    public class VariantSelect : MonoBehaviour
    {
        public VariantSetBase VariantSet;
        
        public VariantAsset VariantAsset;
        
        [SerializeField]
        private bool triggerConditionalVariants = true;
        
        public virtual void SelectVariant()
        {
            if (VariantSet == null || VariantAsset == null) return;
            VariantSetBase.VariantTriggered?.Invoke(VariantSet.VariantSetAsset, VariantAsset, triggerConditionalVariants);
        }

        public virtual void SelectVariant(bool selected)
        {
            if(!selected) return;
            SelectVariant();
        }
    }
}
