using UnityEngine;

namespace IndustryCSE.Tool.ProductConfigurator.ScriptableObjects
{
    public class VariantAsset : AssetBase
    {
        public string VariantName => variantName;
        [SerializeField]
        private string variantName;
        
        public Texture2D icon;
        public int additionalCost;
        public string description;

#if UNITY_EDITOR
        public override void SetName(string value)
        {
            variantName = value;
        }
#endif
    }
}