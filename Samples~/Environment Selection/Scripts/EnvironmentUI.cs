using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace IndustryCSE.Tool.ProductConfigurator.Sample.Environment
{
    [Serializable]
    public struct SceneDetail
    {
        public string SceneName;
        public SceneObject Scene;
    }
    
    public class EnvironmentUI : MonoBehaviour
    {
        [SerializeField]
        private UIDocument uiDocument;
        
        [SerializeField]
        private bool autoLoad;
        
        [SerializeField]
        private int defaultSceneIndex;
        
        [SerializeField]
        private SceneDetail[] scenes;

        private SceneObject nextLoadingScene;
        
        private DropdownField sceneDropdown;

        private void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private void Start()
        {
            var allScenes = scenes.Select(x => x.SceneName).ToList();
            sceneDropdown = new DropdownField(allScenes, autoLoad? defaultSceneIndex : -1);
            sceneDropdown.RegisterValueChangedCallback(OnDropdownValueChanged);
            uiDocument.rootVisualElement.Add(sceneDropdown);
            
            if (autoLoad)
            {
                Load(defaultSceneIndex);
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            sceneDropdown.UnregisterValueChangedCallback(OnDropdownValueChanged);
        }
        
        private void OnDropdownValueChanged(ChangeEvent<string> evt)
        {
            Load(sceneDropdown.index);
        }

        private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            nextLoadingScene = null;
            SceneManager.SetActiveScene(arg0);
            if(scenes.All(x => x.Scene.SceneName != arg0.name)) return;
            sceneDropdown.SetValueWithoutNotify(scenes.First(x => x.Scene.SceneName == arg0.name).SceneName);
        }
        
        private void OnSceneUnloaded(Scene arg0)
        {
            if (nextLoadingScene == null) return;
            SceneManager.LoadSceneAsync(nextLoadingScene.SceneName, LoadSceneMode.Additive);
        }
        
        private void OnActiveSceneChanged(Scene from, Scene current)
        {
            if (nextLoadingScene == null) return;
            SceneManager.UnloadSceneAsync(from, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
        }

        private void Load(int index)
        {
            index = Mathf.Clamp(index, 0, scenes.Length - 1);
            if(SceneManager.GetActiveScene().name == scenes[index].Scene.SceneName) return;
            nextLoadingScene = scenes[index].Scene;
            if (nextLoadingScene == null)
            {
                return;
            }
            if(SceneManager.sceneCount > 1 && SceneManager.GetActiveScene() != SceneManager.GetSceneByBuildIndex(0))
            {
                SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(0));
                return;
            }
            SceneManager.LoadSceneAsync(nextLoadingScene.SceneName, LoadSceneMode.Additive);
        }
    }
}

