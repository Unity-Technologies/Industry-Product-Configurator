using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IndustryCSE.Tool.ProductConfigurator.CustomAttribute;

namespace IndustryCSE.Tool.ProductConfigurator.ScriptableObjects
{
    [CreateAssetMenu(fileName = "Configuration", menuName = "Product Configurator/Configuration", order = 1)]
    public class Configuration : ScriptableObject
    {
        public string ConfigurationName => configurationName;
    
        [SerializeField]
        protected string configurationName;

        public Guid uniqueId;

        public string UniqueIdString => uniqueIdString;
    
        [ReadOnly, SerializeField] private string uniqueIdString;

        public bool Hide => hide;
    
        [SerializeField] private bool hide;

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