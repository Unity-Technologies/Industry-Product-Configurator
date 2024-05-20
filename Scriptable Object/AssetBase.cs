using System;
using UnityEngine;
using IndustryCSE.Tool.ProductConfigurator.CustomAttribute;

namespace IndustryCSE.Tool.ProductConfigurator.ScriptableObjects
{
    public abstract class AssetBase : ScriptableObject
    {
        public string UniqueIdString => uniqueIdString;
        [ReadOnly, SerializeField] protected string uniqueIdString;
        
        public bool Hide => hide;
        [SerializeField] private bool hide;

        private Guid _uniqueId;
        
        private void OnValidate()
        {
            NewID();
        }
        
        public void NewID()
        {
            if(_uniqueId == Guid.Empty && string.IsNullOrEmpty(uniqueIdString))
            {
                if (!Guid.TryParse(name, out _uniqueId))
                {
                    _uniqueId = Guid.NewGuid();
                }
                uniqueIdString = _uniqueId.ToString();
                
            } else if (!string.Equals(uniqueIdString, name) && Guid.TryParse(name, out _uniqueId))
            {
                uniqueIdString = _uniqueId.ToString();
            } else if(_uniqueId == Guid.Empty && !string.IsNullOrEmpty(uniqueIdString))
            {
                _uniqueId = !Guid.TryParse(uniqueIdString, out _uniqueId) ? Guid.NewGuid() : new Guid(uniqueIdString);
            }
        }
        
#if UNITY_EDITOR
        public abstract void SetName(string value);
#endif
    }
}
