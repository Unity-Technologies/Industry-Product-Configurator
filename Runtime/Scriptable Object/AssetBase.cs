using System;
using UnityEngine;
using IndustryCSE.Tool.ProductConfigurator.CustomAttribute;

namespace IndustryCSE.Tool.ProductConfigurator.ScriptableObjects
{
    public abstract class AssetBase : ScriptableObject
    {
        public string UniqueIdString => uniqueIdString;
        [ReadOnly, SerializeField] protected string uniqueIdString;

        public Guid uniqueId;
        
        private void OnValidate()
        {
            NewID();
        }
        
        public void NewID()
        {
            if(uniqueId == Guid.Empty)
            {
                uniqueId = Guid.NewGuid();
            }
            uniqueIdString = uniqueId.ToString();
        }
        
#if UNITY_EDITOR
        public abstract void SetName(string value);
#endif
    }
}
