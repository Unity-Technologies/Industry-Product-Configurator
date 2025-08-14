using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if HAS_APPUI
using Unity.AppUI.UI;
#endif
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using IndustryCSE.Tool.ProductConfigurator.Runtime;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;

namespace IndustryCSE.Tool.ProductConfigurator.Shared.Runtime
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
        public CinemachineVirtualCameraBase VirtualCamera;
    }
    
    [RequireComponent(typeof(UIDocument))]
    public class MainUIController : MonoBehaviour
    {
#if HAS_APPUI
        private string k_CloseMenuButtonName = "Close-Menu-Button";
        private string k_ProductNameLabelName = "Product-Name";
        private string k_TotalCostLabelName = "Total-Cost";
        private string k_EnvironmentDropdownName = "Environment-Dropdown";
        private string k_CameraDropdownName = "Camera-Dropdown";
        private string k_ScrollViewName = "VariantSetList";
        private string k_CostContainerName = "Cost-Container";
        private string k_MenuPanelName = "Menu-Panel";
        private string k_CloseMenuPanelClass = "Close-Menu";
        private string k_OpenMenuPanelButtonName = "Open-Menu-Button";
        private string k_ArrowIconName = "ArrowIcon";
        private string k_ArrowCloseClassName = "Closed";
        private string k_ArrowOpenClassName = "Opened";
        
        private UIDocument m_document;

        private IconButton m_closeMenuButton;
        private IconButton m_openMenuButton;
        private Text m_productNameLabel;
        private Text m_totalCostLabel;
        private Dropdown m_environmentDropdown;
        private Dropdown m_cameraDropdown;

        
        private Coroutine m_costUpdateCoroutine;
        private VisualElement m_priceContainer;
        private ScrollView m_scrollView;
        private VisualElement m_variantsContainer;
        private VisualElement m_menuPanel;
        
        private VariantSetBase _currentVariantSet;
        private VariantSetBase[] _variantSets;

        [Header("Product Information")]
        [SerializeField] private string productName;
        [SerializeField] private string costCurrency;
        [SerializeField] private int defaultCost;
        
        [Header("Environment")]
        [SerializeField]
        private bool autoLoad;
        [SerializeField]
        private int defaultSceneIndex;
        [SerializeField]
        private SceneDetail[] scenes;
        private string _defaultSceneName;
        private SceneObject _nextLoadingScene;
        
        [Header("Camera")]
        [SerializeField]
        private CinemachineVirtualCameraBase defaultCamera;
        [SerializeField]
        private CameraData[] cameras;
        private CinemachineVirtualCameraBase _currentCamera;
        private CinemachineVirtualCameraBase _originalCamera;
        private CinemachineBrain _currentBrain;
        
        [Header("Variant Set")]
        [SerializeField]
        private VisualTreeAsset variantSetVisualTreeAsset;

        private void Awake()
        {
            m_document = GetComponent<UIDocument>();
            m_menuPanel = m_document.rootVisualElement.Q<VisualElement>(k_MenuPanelName);
            
            m_closeMenuButton = m_document.rootVisualElement.Q<IconButton>(k_CloseMenuButtonName);
            m_productNameLabel = m_document.rootVisualElement.Q<Text>(k_ProductNameLabelName);
            m_totalCostLabel = m_document.rootVisualElement.Q<Text>(k_TotalCostLabelName);
            m_priceContainer = m_document.rootVisualElement.Q<VisualElement>(k_CostContainerName);

            m_environmentDropdown = m_document.rootVisualElement.Q<Dropdown>(k_EnvironmentDropdownName);
            m_environmentDropdown.bindItem = EnvironmentDropdownBindItem;
            m_environmentDropdown.bindTitle = EnvironmentDropdownBindTitle;
            
            m_cameraDropdown = m_document.rootVisualElement.Q<Dropdown>(k_CameraDropdownName);
            m_cameraDropdown.bindItem = CameraDropdownBindItem;
            m_cameraDropdown.bindTitle = CameraDropdownBindTitle;
            m_openMenuButton = m_document.rootVisualElement.Q<IconButton>(k_OpenMenuPanelButtonName);
            
            m_scrollView = m_document.rootVisualElement.Q<ScrollView>(k_ScrollViewName);
            
            _defaultSceneName = SceneManager.GetActiveScene().name;
            
            _currentBrain = FindFirstObjectByType<CinemachineBrain>(FindObjectsInactive.Exclude);
            
            foreach (var virtualCamera in FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                virtualCamera.Priority = 0;
            }
            
            VariantSetBase.VariantTriggered += OnVariantTriggered;
        }

        private void Start()
        {
            m_menuPanel.RegisterCallback<TransitionEndEvent>(OnCloseTransitionEnd);
            m_openMenuButton.style.display = DisplayStyle.None;
            m_openMenuButton.clicked += OnOpenMenuButtonClicked;
#region Header
            m_productNameLabel.text = productName;
            m_totalCostLabel.text = $"{costCurrency} {defaultCost}";
            m_costUpdateCoroutine = StartCoroutine(UpdateCostCoroutine());
            m_closeMenuButton.clicked += CloseMenuButtonOnClicked;
            
#endregion

#region Environment
            m_environmentDropdown.SetEnabled(scenes.Length > 0);
            if (m_environmentDropdown.enabledSelf)
            {
                m_environmentDropdown.sourceItems = scenes;
                if (autoLoad)
                {
                    m_environmentDropdown.SetValueWithoutNotify(new [] {defaultSceneIndex});
                    Load(defaultSceneIndex);
                }
                else
                {
                    m_environmentDropdown.value = null;
                }
                m_environmentDropdown.RegisterValueChangedCallback(OnEnvironmentDropdownValueChanged);
                SceneManager.sceneLoaded += OnSceneLoaded;
                SceneManager.sceneUnloaded += OnSceneUnloaded;
                SceneManager.activeSceneChanged += OnActiveSceneChanged;
            }
#endregion

#region Camera
            m_cameraDropdown.sourceItems = cameras;
            var defaultCameraIndex = Array.FindIndex(cameras, x => x.VirtualCamera == defaultCamera);
            m_cameraDropdown.SetValueWithoutNotify(new int[] {defaultCameraIndex});
            
            m_cameraDropdown.RegisterValueChangedCallback(OnCameraDropdownValueChanged);
            
            if(defaultCamera != null)
            {
                SwitchCamera(defaultCamera);
            }
#endregion

#region Variant Set
            _variantSets = FindObjectsByType<VariantSetBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (_variantSets == null || _variantSets.Length == 0)
            {
                Debug.LogWarning("No Variant Sets found in the scene.");
                return;
            }
            foreach (var variantSet in _variantSets)
            {
                var variantSetButton = variantSetVisualTreeAsset.Instantiate().Children().First();
                m_scrollView.Add(variantSetButton);
                variantSetButton.userData = variantSet;
                var text = variantSetButton.Q<Text>();
                if (text != null)
                {
                    text.text = variantSet.VariantSetAsset.VariantSetName;
                }

                var icon = variantSetButton.Q<VisualElement>("VariantIcon");
                if (icon != null)
                {
                    icon.style.backgroundImage = new StyleBackground(variantSet.VariantBase[variantSet.CurrentSelectionIndex].variantAsset.icon);
                }
                variantSetButton.RegisterCallback<ClickEvent>(OnVariantSetClick);
            }
#endregion
        }

        private void OnDestroy()
        {
            m_environmentDropdown?.UnregisterValueChangedCallback(OnEnvironmentDropdownValueChanged);
            m_cameraDropdown?.UnregisterValueChangedCallback(OnCameraDropdownValueChanged);
            m_closeMenuButton.clicked -= CloseMenuButtonOnClicked;
            m_openMenuButton.clicked -= OnOpenMenuButtonClicked;
            m_menuPanel?.UnregisterCallback<TransitionEndEvent>(OnCloseTransitionEnd);
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            VariantSetBase.VariantTriggered -= OnVariantTriggered;
        }

#region Header

        private IEnumerator UpdateCostCoroutine()
        {
            if (defaultCost <= 0 && string.IsNullOrEmpty(costCurrency))
            {
                m_priceContainer.style.display = DisplayStyle.None;
                yield break;
            }
            m_priceContainer.style.display = DisplayStyle.Flex;
            yield return null;
            m_totalCostLabel.text = $"{costCurrency} {defaultCost.ToString("C0").Substring(1)}";
            int totalCost = defaultCost;
            foreach (var variantSetBase in _variantSets)
            {
                totalCost += variantSetBase.CurrentSelectionCost;
            }
            m_totalCostLabel.text = $"{costCurrency} {totalCost.ToString("C0".Substring(1))}";
        }
        
        private void CloseMenuButtonOnClicked()
        {
            if (!m_menuPanel.ClassListContains(k_CloseMenuPanelClass))
            {
                m_menuPanel.AddToClassList(k_CloseMenuPanelClass);
            }
        }
        
        private void OnCloseTransitionEnd(TransitionEndEvent evt)
        {
            //Show open menu button
            if (m_menuPanel.ClassListContains(k_CloseMenuPanelClass))
            {
                m_openMenuButton.style.display = DisplayStyle.Flex;
            }
        }
        
        private void OnOpenMenuButtonClicked()
        {
            m_openMenuButton.style.display = DisplayStyle.None;
            if (m_menuPanel.ClassListContains(k_CloseMenuPanelClass))
            {
                m_menuPanel.RemoveFromClassList(k_CloseMenuPanelClass);
            }
        }

#endregion
        

#region Environment
        
        private void OnEnvironmentDropdownValueChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            Load(evt.newValue.First());
        }

        private void EnvironmentDropdownBindTitle(DropdownItem arg1, IEnumerable<int> arg2)
        {
            if (arg2 == null || !arg2.Any())
            {
                arg1.label = m_environmentDropdown.defaultMessage;
                return;
            }
            EnvironmentDropdownBindItem(arg1, arg2.First());
        }

        private void EnvironmentDropdownBindItem(DropdownItem arg1, int arg2)
        {
            var sourceObjects = m_environmentDropdown.sourceItems as SceneDetail[];
            if(sourceObjects == null) return;
            arg1.label = sourceObjects[arg2].SceneDisplayName;
        }
        
        private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            _nextLoadingScene = null;
            SceneManager.SetActiveScene(arg0);
            if(scenes.All(x => x.Scene.SceneName != arg0.name)) return;
            var index = Array.FindIndex(scenes, x => x.Scene.SceneName == arg0.name);
            if (index < 0) return;
            m_environmentDropdown.SetValueWithoutNotify(new [] {index});
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
        
#if UNITY_EDITOR
        
        private void OnValidate()
        {
            for(var i = 0; i < scenes.Length; i++)
            {
                if(scenes[i].Scene == null) continue;
                scenes[i].Scene.SceneName = scenes[i].Scene.ScentAsset.name;
            }
        }
        
#endif
        
#endregion

#region Camera

        private void SwitchCamera(CinemachineVirtualCameraBase toCamera)
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

            if (cameras.Any(x => x.VirtualCamera == _currentCamera))
            {
                var index = Array.FindIndex(cameras, x => x.VirtualCamera == _currentCamera);
                m_cameraDropdown.SetValueWithoutNotify(new [] {index});
            }
            
            if (cameras.All(x => x.VirtualCamera != toCamera))
            {
                m_cameraDropdown.value = null;
            }
        }
        
        private void CameraDropdownBindTitle(DropdownItem arg1, IEnumerable<int> arg2)
        {
            if (arg2 == null || !arg2.Any())
            {
                arg1.label = m_cameraDropdown.defaultMessage;
                return;
            }
            CameraDropdownBindItem(arg1, arg2.First());
        }

        private void CameraDropdownBindItem(DropdownItem arg1, int arg2)
        {
            var cameraData = m_cameraDropdown.sourceItems as CameraData[];
            if (cameraData == null || arg2 < 0 || arg2 >= cameraData.Length) return;
            arg1.label = cameraData[arg2].CameraName;
        }
        
        private void OnCameraDropdownValueChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            if (evt.newValue == null || !evt.newValue.Any())
            {
                return;
            }
            var index = evt.newValue.First();
            if (index < 0 || index >= cameras.Length) return;
            SwitchCamera(cameras[index].VirtualCamera);
        }

#endregion

#region Variant Set

        private void OnVariantTriggered(VariantSetAsset arg1, VariantAsset arg2, bool arg3)
        {
            if(m_costUpdateCoroutine != null) StopCoroutine(m_costUpdateCoroutine);
            m_costUpdateCoroutine = StartCoroutine(UpdateCostCoroutine());
        }
        
        private void OnVariantSetClick(ClickEvent evt)
        {
            var ve = evt.target as VisualElement;
            if (ve == null) return;

            var allOpened = m_scrollView.contentContainer.Query<VisualElement>()
                .Where(x => x.ClassListContains(k_ArrowOpenClassName)).ToList();

            foreach (var openedVariantSet in allOpened)
            {
                openedVariantSet.RemoveFromClassList(k_ArrowOpenClassName);
                openedVariantSet.AddToClassList(k_ArrowCloseClassName);
            }
            
            m_variantsContainer ??= new VisualElement();
            m_variantsContainer.ClearClassList();
            m_variantsContainer.style.overflow = Overflow.Hidden;
            var existingVariantButtons = m_variantsContainer.Query<VisualElement>().ToList();
            foreach (var existingVariantButton in existingVariantButtons)
            {
                existingVariantButton.UnregisterCallback<ClickEvent>(OnVariantClick);
            }
            m_variantsContainer.Clear();
            m_variantsContainer.RemoveFromHierarchy();
            var variantSet = ve.userData as VariantSetBase;
            if (variantSet == null)
            {
                return;
            }
            if (m_variantsContainer.userData != null && m_variantsContainer.userData == variantSet)
            {
                m_variantsContainer.style.display = DisplayStyle.None;
                m_variantsContainer.userData = null;
                if (_originalCamera != null)
                {
                    SwitchCamera(_originalCamera);
                }
                return;
            }
            
            if (!m_variantsContainer.ClassListContains("ContainerClosed"))
            {
                m_variantsContainer.AddToClassList("ContainerClosed");
            }

            if (ve.ClassListContains(k_ArrowCloseClassName))
            {
                ve.RemoveFromClassList(k_ArrowCloseClassName);
            }
            ve.AddToClassList(k_ArrowOpenClassName);
            m_variantsContainer.userData = variantSet;
            foreach (var variantBase in variantSet.VariantBase)
            {
                var variantButton = variantSetVisualTreeAsset.Instantiate().Children().First();
                variantButton.userData = variantBase;
                var variantText = variantButton.Q<Text>();
                if (variantText != null)
                {
                    variantText.text = variantBase.variantAsset.VariantName;
                }
                var variantIcon = variantButton.Q<VisualElement>("VariantIcon");
                if (variantIcon != null)
                {
                    variantIcon.style.backgroundImage = new StyleBackground(variantBase.variantAsset.icon);
                }
                variantButton.RemoveFromClassList("variant-set--button");
                variantButton.AddToClassList("variant--button");
                variantButton.AddToClassList(k_ArrowCloseClassName);
                variantButton.RegisterCallback<ClickEvent>(OnVariantClick);
                m_variantsContainer.Add(variantButton);
            }
            m_variantsContainer.style.display = DisplayStyle.Flex;
            
            var index = m_scrollView.IndexOf(ve);
            m_scrollView.Insert(index + 1, m_variantsContainer);
            
            if (variantSet.FocusCamera != null)
            {
                var currentCamera = _currentBrain.ActiveVirtualCamera;
                if (cameras.Any(x => x.VirtualCamera == currentCamera))
                {
                    _originalCamera = currentCamera as CinemachineVirtualCameraBase;
                }
                SwitchCamera(variantSet.FocusCamera);
            }

            StartCoroutine(WaitForUIToUpdate());
            return;
            
            IEnumerator WaitForUIToUpdate()
            {
                yield return new WaitForEndOfFrame();
                m_variantsContainer.AddToClassList("ContainerOpened");
            }
        }

        private void OnVariantClick(ClickEvent evt)
        {
            var ve = evt.target as VisualElement;
            if (ve == null) return;
            var variantSetBase = m_variantsContainer.userData as VariantSetBase;
            var variantSet = ve.userData as VariantBase;
            VariantSetBase.VariantTriggered?.Invoke(variantSetBase.VariantSetAsset, variantSet.variantAsset, true);
        }

#endregion

#endif
    }
}
