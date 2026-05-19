using UnityEngine;

namespace IndustryCSE.Tool.ProductConfigurator.ScriptableObjects
{
    public class VariantSetAsset : AssetBase
    {
        public string VariantSetName => variantSetName;
        [SerializeField]
        private string variantSetName;

        #if UNITY_EDITOR

        [HideInInspector, SerializeField]
        public bool hasStoreCameraPositionAndRotation;
        [HideInInspector, SerializeField]
        public Vector3 storeCameraPosition;
        [HideInInspector, SerializeField]
        public Quaternion storeCameraRotation;

        [HideInInspector, SerializeField]
        public float storeCameraDistance;

        #endif
        
#if UNITY_EDITOR
        public override void SetName(string value)
        {
            variantSetName = value;
        }
#endif
    }
}