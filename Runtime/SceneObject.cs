using UnityEngine;
using UnityEngine.Serialization;

namespace IndustryCSE.Tool.ProductConfigurator
{
    [System.Serializable]
    public class SceneObject
    {
        public Object ScentAsset => sceneAsset;
        
        [SerializeField]
        private Object sceneAsset;
        
        public string SceneName;
    }
}
