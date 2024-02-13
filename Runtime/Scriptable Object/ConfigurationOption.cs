using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using IndustryCSE.Tool.ProductConfigurator.CustomAttribute;

namespace IndustryCSE.Tool.ProductConfigurator.ScriptableObjects
{
    [CreateAssetMenu(fileName = "Configuration Option", menuName = "Product Configurator/Configuration Option", order = 1)]
    public class ConfigurationOption : ScriptableObject
    {
        public Texture2D icon;
        public string name;
        public Guid uniqueId;
        public int additionalCost;
    
        public string UniqueIdString => uniqueIdString;
    
        [ReadOnly, SerializeField] private string uniqueIdString;
    
        private void OnValidate()
        {
            if(uniqueId == Guid.Empty)
            {
                uniqueId = Guid.NewGuid();
            }
            uniqueIdString = uniqueId.ToString();
        }
    }
}