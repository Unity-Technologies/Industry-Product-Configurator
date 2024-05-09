using System;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Cinemachine;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;

namespace IndustryCSE.Tool.ProductConfigurator.Sample.StandardConfigurator
{
    [Serializable]
    public struct SceneDetail
    {
        public string SceneDisplayName;
        public SceneObject Scene;
    }

    [Serializable]
    public struct CameraData
    {
        public string CameraName;
        public CinemachineVirtualCamera VirtualCamera;
    }
    
    public class StandardUIController : MonoBehaviour
    {
        [SerializeField]
        private UIDocument uiDocument;
        
        private VisualElement _menuContainer;
        private Button _closeMenuButton;
        private Button _openMenuButton;
        
        [Header("Cost")]
        [SerializeField]
        private string currencySymbol;
        [SerializeField]
        private int defaultCost;
        private Label _costLabel;
        private VisualElement _priceContainer;
        private Coroutine _costUpdateCoroutine;
        
        [Header("Variant Set")]
        [SerializeField]
        private VisualTreeAsset variantSetVisualTreeAsset;
        [SerializeField]
        private VisualTreeAsset variantButtonVisualTreeAsset;
        private ScrollView _scrollView;
        private VisualElement _variantContainer;
        private VariantSetBase _currentVariantSet;
        private VariantSetBase[] _variantSets;
        
        [Header("Environment")]
        private string _defaultSceneName;
        [SerializeField]
        private bool autoLoad;
        [SerializeField]
        private int defaultSceneIndex;
        [SerializeField]
        private SceneDetail[] scenes;
        private SceneObject _nextLoadingScene;
        private DropdownField _environmentDropdown;
        
        [Header("Camera")]
        [SerializeField]
        private CameraData[] cameras;
        private CinemachineVirtualCamera _currentCamera;
        private DropdownField _cameraDropdown;
        [SerializeField]
        private CinemachineVirtualCamera defaultCamera;
        
        #if UNITY_EDITOR
        
        private void OnValidate()
        {
            for(var i = 0; i < scenes.Length; i++)
            {
                scenes[i].Scene.SceneName = scenes[i].Scene.ScentAsset.name;
            }
        }
        
        #endif
        

        private void Awake()
        {
            _defaultSceneName = SceneManager.GetActiveScene().name;
            foreach (var virtualCamera in FindObjectsByType<CinemachineVirtualCamera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                virtualCamera.Priority = 0;
            }
            VariantSetBase.VariantTriggered += OnVariantTriggered;
        }

        private void Start()
        {
            _variantContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    paddingRight = new Length(10f, LengthUnit.Pixel),
                    paddingLeft = new Length(10f, LengthUnit.Pixel),
                    paddingBottom = new Length(10f, LengthUnit.Pixel),
                    paddingTop = new Length(10f, LengthUnit.Pixel),
                }
            };
            
            _menuContainer = uiDocument.rootVisualElement.Q<VisualElement>("ConfigurationMenu");
            _closeMenuButton = uiDocument.rootVisualElement.Q<Button>("CloseBtn");
            _openMenuButton = uiDocument.rootVisualElement.Q<Button>("MenuBtn");
            
            _menuContainer.style.display = DisplayStyle.Flex;
            _closeMenuButton.clicked += OnCloseMenuButtonClicked;
            _openMenuButton.style.display = DisplayStyle.None;
            _openMenuButton.clicked += OnOpenMenuButtonClicked;
            
            _costLabel = uiDocument.rootVisualElement.Q<Label>("PriceLabel");
            _priceContainer = uiDocument.rootVisualElement.Q<VisualElement>("PriceContainer");

            _scrollView = uiDocument.rootVisualElement.Q<ScrollView>("VariantSetScrollView");
            PopulateAllVariantSets();
            
            _environmentDropdown = uiDocument.rootVisualElement.Q<DropdownField>("EnvironmentDropDown");
            _environmentDropdown.RegisterValueChangedCallback(OnEnvironmentDropdownValueChanged);
            PopulateEnvironments();
            
            _cameraDropdown = uiDocument.rootVisualElement.Q<DropdownField>("CameraDropDown");
            _cameraDropdown.RegisterValueChangedCallback(OnCameraDropDownValueChange);
            if(defaultCamera != null)
            {
                SwitchCamera(defaultCamera);
            }

            //Wait for one frame to ensure all the variant sets are set
            _costUpdateCoroutine = StartCoroutine(UpdateCostCoroutine());
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            _environmentDropdown.UnregisterValueChangedCallback(OnEnvironmentDropdownValueChanged);
            _cameraDropdown.UnregisterValueChangedCallback(OnCameraDropDownValueChange);
            _closeMenuButton.clicked -= OnCloseMenuButtonClicked;
            _openMenuButton.clicked -= OnOpenMenuButtonClicked;
            VariantSetBase.VariantTriggered -= OnVariantTriggered;
        }

        private void OnVariantTriggered(VariantSetAsset arg1, VariantAsset arg2, bool arg3)
        {
            foreach (var child in _variantContainer.Children())
            {
                if(child.userData == null) continue;
                Button button = child.Q<Button>();
                bool selected = string.Equals(child.userData.ToString(), arg2.UniqueIdString);
                if (selected)
                {
                    if (!button.ClassListContains("Selected"))
                    {
                        button.AddToClassList("Selected");
                    }
                }
                else
                {
                    if(button.ClassListContains("Selected"))
                    {
                        button.RemoveFromClassList("Selected");
                    }
                }
            }
            
            if(_costUpdateCoroutine != null) StopCoroutine(_costUpdateCoroutine);
            _costUpdateCoroutine = StartCoroutine(UpdateCostCoroutine());
        }

        private void OnOpenMenuButtonClicked()
        {
            _menuContainer.style.display = DisplayStyle.Flex;
            _openMenuButton.style.display = DisplayStyle.None;
        }

        private void OnCloseMenuButtonClicked()
        {
            _menuContainer.style.display = DisplayStyle.None;
            _openMenuButton.style.display = DisplayStyle.Flex;
        }

        #region Variant Set
        private void PopulateAllVariantSets()
        {
            _variantSets = FindObjectsByType<VariantSetBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            
            foreach (var variantSetBase in _variantSets)
            {
                if(variantSetBase.Hide) continue;
                var variantSetContainer = variantSetVisualTreeAsset.Instantiate();
                
                var variantSetLabel = variantSetContainer.Q<Label>("VariantSetLabel");
                variantSetLabel.text = variantSetBase.VariantSetAsset.VariantSetName;
                variantSetContainer.userData = variantSetBase.VariantSetAsset.UniqueIdString;
                if (variantSetBase.CurrentSelectionIndex != -1)
                {
                    if(variantSetBase.VariantBase[variantSetBase.CurrentSelectionIndex].variantAsset.icon != null)
                    {
                        var variantSetIcon = variantSetContainer.Q<VisualElement>("Icon");
                        variantSetIcon.style.backgroundImage = new StyleBackground(variantSetBase.VariantBase[variantSetBase.CurrentSelectionIndex].variantAsset.icon);
                    }
                }
                
                var btn = variantSetContainer.Q<Button>();
                btn.clicked += () =>
                {
                    foreach (var child in _scrollView.Children())
                    {
                        Button button = child.Q<Button>();
                        if(button.ClassListContains("Selected"))
                        {
                            button.RemoveFromClassList("Selected");
                        }
                    }

                    if (_currentVariantSet != null && _currentVariantSet == variantSetBase)
                    {
                        RemoveScrollViewContent();
                        _currentVariantSet = null;
                        return;
                    }
                    else
                    {
                        if (_variantContainer != null && _variantContainer.style.display == DisplayStyle.Flex)
                        {
                            RemoveScrollViewContent();
                        }
                    }
                    _currentVariantSet = variantSetBase;
                    foreach (var child in _scrollView.Children())
                    {
                        if (child.userData == null)
                        {
                            continue;
                        }
                        if (!string.Equals(child.userData.ToString(), _currentVariantSet.VariantSetAsset.UniqueIdString)) continue;
                        Button button = child.Q<Button>();
                        button.AddToClassList("Selected");
                    }
                    
                    _variantContainer?.Clear();
                    
                    foreach (var variantBase in variantSetBase.VariantBase)
                    {
                        var variant = variantButtonVisualTreeAsset.Instantiate();
                        var variantBtn = variant.Q<Button>();
                        var buttonLabel = variant.Q<Label>();
                        variant.userData = variantBase.variantAsset.UniqueIdString;

                        if (!string.IsNullOrEmpty(variantSetBase.CurrentSelectionGuid))
                        {
                            if(string.Equals(variantBase.variantAsset.UniqueIdString, variantSetBase.CurrentSelectionGuid))
                            {
                                variantBtn.AddToClassList("Selected");
                            }
                        }
                        
                        if(variantBase.variantAsset.icon != null)
                        {
                            var variantIcon = variant.Q<VisualElement>("Icon");
                            variantIcon.style.backgroundImage = new StyleBackground(variantBase.variantAsset.icon);
                        }
                        
                        buttonLabel.text = variantBase.variantAsset.VariantName;
                        variantBtn.clicked += () =>
                        {
                            if (variantSetBase.FocusCamera != null)
                            {
                                SwitchCamera(variantSetBase.FocusCamera);
                            }
                            VariantSetBase.VariantTriggered?.Invoke(variantSetBase.VariantSetAsset, variantBase.variantAsset, true);
                        };
                        _variantContainer.Add(variant);
                    }
                    
                    var index = _scrollView.IndexOf(variantSetContainer);
                    _scrollView.Insert(index + 1, _variantContainer);
                    _variantContainer.style.display = DisplayStyle.Flex;
                    
                    if (variantSetBase.FocusCamera != null)
                    {
                        SwitchCamera(variantSetBase.FocusCamera);
                    }

                    void RemoveScrollViewContent()
                    {
                        _scrollView.Remove(_variantContainer);
                        _variantContainer.Clear();
                        _variantContainer.style.display = DisplayStyle.None;
                    }
                };
                
                _scrollView.Add(variantSetContainer);
            }
        }

        #endregion
        
        #region Environment
        private void PopulateEnvironments()
        {
            if(_environmentDropdown == null) return;
            _environmentDropdown.style.display = scenes.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (scenes.Length <= 0) return;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            
            for(var i = 0; i < scenes.Length; i++)
            {
                if (string.IsNullOrEmpty(scenes[i].SceneDisplayName))
                {
                    scenes[i].SceneDisplayName = $"Sample Scene {i + 1}";
                }
            }
            
            var allScenes = scenes.Select(x => x.SceneDisplayName).ToList();
            _environmentDropdown.choices = allScenes;
            
            if (autoLoad)
            {
                Load(defaultSceneIndex);
            }
        }
        
        private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            _nextLoadingScene = null;
            SceneManager.SetActiveScene(arg0);
            if(scenes.All(x => x.Scene.SceneName != arg0.name)) return;
            _environmentDropdown.SetValueWithoutNotify(scenes.First(x => x.Scene.SceneName == arg0.name).SceneDisplayName);
        }
        
        private void OnSceneUnloaded(Scene arg0)
        {
            if (_nextLoadingScene == null) return;
            SceneManager.LoadSceneAsync(_nextLoadingScene.SceneName, LoadSceneMode.Additive);
        }
        
        private void OnActiveSceneChanged(Scene from, Scene current)
        {
            if (_nextLoadingScene == null) return;
            SceneManager.UnloadSceneAsync(from, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
        }
        
        private void OnEnvironmentDropdownValueChanged(ChangeEvent<string> evt)
        {
            Load(_environmentDropdown.index);
        }
        
        private void Load(int index)
        {
            index = Mathf.Clamp(index, 0, scenes.Length - 1);
            if(SceneManager.GetActiveScene().name == scenes[index].Scene.SceneName) return;
            _nextLoadingScene = scenes[index].Scene;
            if (_nextLoadingScene == null)
            {
                return;
            }
            if(SceneManager.sceneCount > 1 && SceneManager.GetActiveScene() != SceneManager.GetSceneByName(_defaultSceneName))
            {
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(_defaultSceneName));
                return;
            }
            SceneManager.LoadSceneAsync(_nextLoadingScene.SceneName, LoadSceneMode.Additive);
        }
        
        #endregion
        
        #region Camera
        
        private void SwitchCamera(CinemachineVirtualCamera toCamera)
        {
            if(toCamera == null) return;
            if(_currentCamera != null && _currentCamera == toCamera) return;
            if (_currentCamera != null)
            {
                _currentCamera.Priority = 0;
            }
            
            foreach (var optionalCamera in cameras.Where(x => x.VirtualCamera != toCamera))
            {
                optionalCamera.VirtualCamera.Priority = 0;
            }
            toCamera.Priority = 10;
            _currentCamera = toCamera;
            UpdateCameraDropDown();
        }

        private void UpdateCameraDropDown()
        {
            if(_cameraDropdown == null) return;
            if (cameras.Length <= 0)
            {
                _cameraDropdown.style.display = DisplayStyle.None;
                return;
            }
            var cameraChoices = cameras.Select(x => x.CameraName).ToList();
            _cameraDropdown.style.display = cameraChoices.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _cameraDropdown.choices = cameraChoices;
            if (cameras.Any(x => x.VirtualCamera == _currentCamera))
            {
                _cameraDropdown.SetValueWithoutNotify(cameras.First(x => x.VirtualCamera == _currentCamera).CameraName);
            }
            else
            {
                _cameraDropdown.index = -1;
            }
        }
        
        private void OnCameraDropDownValueChange(ChangeEvent<string> evt)
        {
            if(_cameraDropdown.index < 0) return;
            SwitchCamera(cameras[_cameraDropdown.index].VirtualCamera);
        }
        
        #endregion
        
        #region Cost

        private IEnumerator UpdateCostCoroutine()
        {
            if (defaultCost <= 0 && string.IsNullOrEmpty(currencySymbol))
            {
                _priceContainer.style.display = DisplayStyle.None;
                yield break;
            }
            _priceContainer.style.display = DisplayStyle.Flex;
            yield return null;
            _costLabel.text = $"{currencySymbol} {defaultCost.ToString("C0").Substring(1)}";
            int totalCost = defaultCost;
            foreach (var variantSetBase in _variantSets)
            {
                totalCost += variantSetBase.CurrentSelectionCost;
            }
            _costLabel.text = $"{currencySymbol} {totalCost.ToString("C0".Substring(1))}";
        }
        
        #endregion
    }
}

