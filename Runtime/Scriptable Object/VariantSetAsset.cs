using UnityEngine;

namespace IndustryCSE.Tool.ProductConfigurator.ScriptableObjects
{
    public class VariantSetAsset : AssetBase
    {
        public string VariantSetName => variantSetName;
        [SerializeField]
        private string variantSetName;
        
        public bool Hide => hide;
        [SerializeField] private bool hide;
        
#if UNITY_EDITOR
        public override void SetName(string value)
        {
            variantSetName = value;
        }
#endif
    }
}