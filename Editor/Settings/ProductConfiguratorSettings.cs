using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IndustryCSE.Tool.ProductConfigurator.Settings.Editor
{
    public enum RemoveBehaviour
    {
        AskEveryTime,
        RemoveWithoutAsking,
        KeepWithoutAsking
    }
    
    public class ProductConfiguratorSettings : ScriptableObject
    {
        public bool UseAdvancedSettings { get; private set; } = false;
        
#if UNITY_EDITOR_OSX
        public string VariantSetAssetPath { get; private set; } = "Assets/Product Configurator/Variant Set Asset";
        public string VariantAssetPath { get; private set; } = "Assets/Product Configurator/Variant Asset";
        public string VariantIconPath { get; private set; } = "Assets/Product Configurator/Icons";
#elif UNITY_EDITOR_WIN
        public string VariantSetAssetPath { get; private set; } = "Assets\\Product Configurator\\Variant Set Asset";
        public string VariantAssetPath { get; private set; } = "Assets\\Product Configurator\\Variant Asset";
        public string VariantIconPath { get; private set; } = "Assets\\Product Configurator\\Icons";
#endif

        
        public RemoveBehaviour RemoveBehaviour { get; private set; } = RemoveBehaviour.AskEveryTime;
        
        public void SetAdvancedSettings(bool value)
        {
            UseAdvancedSettings = value;
        }
        
        public void SetVariantSetAssetPath(string path)
        {
            VariantSetAssetPath = path;
        }
        
        public void SetVariantAssetPath(string path)
        {
            VariantAssetPath = path;
        }
        
        public void SetVariantIconPath(string path)
        {
            VariantIconPath = path;
        }
        
        public void SetRemoveBehaviour(int behaviourIndex)
        {
            RemoveBehaviour = (RemoveBehaviour) behaviourIndex;
        }
    }
}
