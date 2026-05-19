using UnityEngine;
using UnityEngine.Serialization;

namespace IndustryCSE.Tool.ProductConfigurator
{
    [System.Serializable]
    public class SceneObject
    {
        public Object SceneAsset => sceneAsset;
        
        [SerializeField]
        private Object sceneAsset;
        
        [SerializeField]
        public string SceneName;
    }
}
