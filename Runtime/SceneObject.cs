using UnityEngine;

namespace IndustryCSE.Tool.ProductConfigurator
{
    [System.Serializable]
    public class SceneObject
    {
        [SerializeField]
        private Object sceneAsset;
        public string SceneName => sceneAsset.name;
    }
}
