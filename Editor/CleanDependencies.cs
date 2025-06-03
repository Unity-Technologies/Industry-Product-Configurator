using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;
using Object = UnityEngine.Object;

namespace IndustryCSE.Tool.ProductConfigurator.Editor
{
    public class CleanDependencies : EditorWindow
    {
        private static HashSet<string> _dependenciesVariantAssets;
        private static HashSet<string> _variantAssetsInProject;
        private static HashSet<string> _unusedVariantAssets;

        private static HashSet<string> _dependenciesVariantSetAssets;
        private static HashSet<string> _variantAssetSetsInProject;
        private static HashSet<string> _unusedVariantSetAssets;
        
        private static TabView _tabView;
        private static Tab _VariantAssetsTab;
        private static Tab _VariantSetAssetsTab;
        private static Button _searchUnusedAssetsButton;
        private static ListView _unuseAssetsListView;
        private static Button _selectAllButton;
        private static Button _deselectAllButton;
        private static Button _deleteButton;
        
        [MenuItem("Window/Product Configurator/Dependencies Cleaner")]
        public static void ShowWindow()
        {
            EditorWindow wnd = GetWindow<CleanDependencies>();
            wnd.titleContent = new GUIContent("Dependencies Cleaner");
        }

        public void CreateGUI()
        {
            _searchUnusedAssetsButton = new Button
            {
                text = "Search Unused Assets"
            };
            
            rootVisualElement.Add(_searchUnusedAssetsButton);
            _searchUnusedAssetsButton.clicked -= SearchUnusedAssetsButtonOnclicked;
            _searchUnusedAssetsButton.clicked += SearchUnusedAssetsButtonOnclicked;

            _tabView = new TabView();
            _VariantAssetsTab = new Tab("Variant Assets");
            _VariantSetAssetsTab = new Tab("Variant Set Assets");
            
            _tabView.Add(_VariantAssetsTab);
            _VariantAssetsTab.name = "Variant Assets";
            _tabView.Add(_VariantSetAssetsTab);
            _VariantSetAssetsTab.name = "Variant Set Assets";

            _tabView.activeTab = _VariantAssetsTab;
            
            rootVisualElement.Add(_tabView);
            
            _tabView.activeTabChanged -= TabViewOnActiveTabChanged;
            _tabView.activeTabChanged += TabViewOnActiveTabChanged;

            _unuseAssetsListView = new ListView
            {
                makeItem = MakeItem,
                bindItem = BindItem,
                unbindItem = UnBindItem,
            };

            rootVisualElement.Add(_unuseAssetsListView);

            var visualElement = new VisualElement()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                }
            };
            
            _selectAllButton = new Button()
            {
                text = "Select All"
            };
            
            visualElement.Add(_selectAllButton);
            
            _selectAllButton.clicked -= SelectAllButtonOnClicked;
            _selectAllButton.clicked += SelectAllButtonOnClicked;

            _deselectAllButton = new Button()
            {
                text = "Deselect All"
            };
            
            visualElement.Add(_deselectAllButton);
            
            _deselectAllButton.clicked -= DeselectAllButtonOnClicked;
            _deselectAllButton.clicked += DeselectAllButtonOnClicked;
            
            rootVisualElement.Add(visualElement);
            
            _selectAllButton.SetEnabled(false);
            _deselectAllButton.SetEnabled(false);
            
            _deleteButton = new Button()
            {
                text = "Delete Selected"
            };
            
            _deleteButton.SetEnabled(false);
            
            _deleteButton.clicked -= DeleteSelectedAssets;
            _deleteButton.clicked += DeleteSelectedAssets;
            rootVisualElement.Add(_deleteButton);
        }

        private void DeleteSelectedAssets()
        {
            int option = EditorUtility.DisplayDialogComplex(
                "Delete Confirmation",
                "Do you want to delete the selected assets?",
                "Delete Selected & their dependencies",    // Option 1
                "Delete Selected", // Option 2
                "Cancel"         // Option 3
            );
            
            switch (option)
            {
                case 0: // "Delete All"
                    DeleteSelectedAssetsInternal(true);
                    break;
                case 1: // "Delete Selected"
                    DeleteSelectedAssetsInternal(false);
                    break;
                case 2: // "Cancel"
                    Debug.Log("Operation canceled");
                    break;
            }
        }
        
        private void DeleteSelectedAssetsInternal(bool deleteDependencyAssetsToo)
        {
            // Save all assets and open scenes to ensure references are up-to-date
            AssetDatabase.SaveAssets();
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            
            var toggles = _unuseAssetsListView.Query<Toggle>().ToList();
            var guidsToDelete = toggles.Where(x => x.value).Select(x => x.userData as string).ToHashSet();
            HashSet<string> additionalGuidsToDelete = null;
            foreach (var assetToBeDeleted in guidsToDelete)
            {
                var assetToDelete = AssetDatabase.GUIDToAssetPath(assetToBeDeleted);
                if (deleteDependencyAssetsToo)
                {
                    var dependencies = AssetDatabase.GetDependencies(assetToDelete, false);
                    foreach (var dependencyPath in dependencies)
                    {
                        if(AssetDatabase.GetMainAssetTypeAtPath(dependencyPath) == typeof(MonoScript)) continue;
                        if(assetToDelete == dependencyPath) continue;
                        
                        var dependencyGUID = AssetDatabase.AssetPathToGUID(dependencyPath);
                        additionalGuidsToDelete ??= new HashSet<string>();
                        additionalGuidsToDelete.Add(dependencyGUID);
                    }
                }
            }
            
            if (additionalGuidsToDelete != null)
            {
                guidsToDelete.UnionWith(additionalGuidsToDelete);
            }
            
            foreach (var guid in guidsToDelete)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath)) continue;
                
                AssetDatabase.DeleteAsset(assetPath);
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            SearchUnusedAssetsButtonOnclicked();
        }

        private void DeselectAllButtonOnClicked()
        {
            ToggleSelections(false);
        }

        private void SelectAllButtonOnClicked()
        {
            ToggleSelections(true);
        }
        
        private void ToggleSelections(bool isSelected)
        {
            if(_unuseAssetsListView == null) return;
            foreach (var toggle in _unuseAssetsListView.Query<Toggle>().ToList())
            {
                toggle.value = isSelected;
            }
        }

        private void UnBindItem(VisualElement arg1, int arg2)
        {
            Button button = arg1.Q<Button>();
            button.UnregisterCallback<ClickEvent>(OnViewButtonClicked);
            Toggle toggle = arg1.Q<Toggle>();
            toggle.UnregisterValueChangedCallback(OnToggleChanged);
        }

        private void BindItem(VisualElement arg1, int arg2)
        {
            var GUID = (_unuseAssetsListView.itemsSource as List<string>)[arg2];
            Toggle toggle = arg1.Q<Toggle>();
            toggle.userData = GUID;
            if (AssetDatabase.GetMainAssetTypeFromGUID(new GUID(GUID)) == typeof(VariantAsset))
            {
                var variantAsset = GetAssetByGUID<VariantAsset>(GUID);
                toggle.text = variantAsset.VariantName;
            }
            else if(AssetDatabase.GetMainAssetTypeFromGUID(new GUID(GUID)) == typeof(VariantSetAsset))
            {
                var variantSetAsset = GetAssetByGUID<VariantSetAsset>(GUID);
                toggle.text = variantSetAsset.VariantSetName;
            }
            Button button = arg1.Q<Button>();
            button.userData = GUID;
            button.RegisterCallback<ClickEvent>(OnViewButtonClicked);
            toggle.RegisterValueChangedCallback(OnToggleChanged);
            toggle.SetValueWithoutNotify(false);
        }

        private void OnToggleChanged(ChangeEvent<bool> evt)
        {
            var allToggles = _unuseAssetsListView.Query<Toggle>().ToList();
            bool anySelected = allToggles.Any(x => x.value);
            _deleteButton.SetEnabled(anySelected);
        }

        private void OnViewButtonClicked(ClickEvent evt)
        {
            Button button = evt.currentTarget as Button;
            if (button == null) return;
            string guid = button.userData as string;
            if (string.IsNullOrEmpty(guid)) return;

            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath)) return;

            Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (asset != null)
            {
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
            }
        }

        private VisualElement MakeItem()
        {
            var visualElement = new VisualElement()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.SpaceBetween,
                }
            };
            visualElement.Add(new Toggle());
            visualElement.Add(new Button()
            {
                text = "View"
            });
            return visualElement;
        }

        private void SearchUnusedAssetsButtonOnclicked()
        {
            string[] allSceneGUIDs = AssetDatabase.FindAssets("t:Scene");
            
            _dependenciesVariantAssets?.Clear();
            _dependenciesVariantSetAssets?.Clear();
            
            foreach (var guid in allSceneGUIDs)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                string[] sceneDependencies = AssetDatabase.GetDependencies(scenePath, true);
                
                foreach (var sceneDependency in sceneDependencies)
                {
                    if (AssetDatabase.GetMainAssetTypeAtPath(sceneDependency) == typeof(VariantAsset))
                    {
                        _dependenciesVariantAssets ??= new HashSet<string>();
                        _dependenciesVariantAssets.Add(AssetDatabase.AssetPathToGUID(sceneDependency));
                    }
                    
                    if (AssetDatabase.GetMainAssetTypeAtPath(sceneDependency) == typeof(VariantSetAsset))
                    {
                        _dependenciesVariantSetAssets ??= new HashSet<string>();
                        _dependenciesVariantSetAssets.Add(AssetDatabase.AssetPathToGUID(sceneDependency));
                    }
                }
            }
            
            string[] allPrefabsGUIDs = AssetDatabase.FindAssets("t:Prefab");
            
            foreach (var guid in allPrefabsGUIDs)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                string[] prefabDependencies = AssetDatabase.GetDependencies(prefabPath, true);
                
                foreach (var prefabDependency in prefabDependencies)
                {
                    if (AssetDatabase.GetMainAssetTypeAtPath(prefabDependency) == typeof(VariantAsset))
                    {
                        _dependenciesVariantAssets ??= new HashSet<string>();
                        _dependenciesVariantAssets.Add(AssetDatabase.AssetPathToGUID(prefabDependency));
                    }
                    
                    if (AssetDatabase.GetMainAssetTypeAtPath(prefabDependency) == typeof(VariantSetAsset))
                    {
                        _dependenciesVariantSetAssets ??= new HashSet<string>();
                        _dependenciesVariantSetAssets.Add(AssetDatabase.AssetPathToGUID(prefabDependency));
                    }
                }
            }
            
            _variantAssetsInProject?.Clear();
            _unusedVariantAssets?.Clear();
            
            _variantAssetsInProject ??= new HashSet<string>();
            
            _variantAssetsInProject = AssetDatabase.FindAssets("t:VariantAsset").ToHashSet();
            foreach (var asset in _variantAssetsInProject)
            {
                if(_dependenciesVariantAssets.Contains(asset)) continue;
                _unusedVariantAssets ??= new HashSet<string>();
                _unusedVariantAssets.Add(asset);
            }
            
            _variantAssetSetsInProject?.Clear();
            _unusedVariantSetAssets?.Clear();
            
            _variantAssetSetsInProject = AssetDatabase.FindAssets("t:VariantSetAsset").ToHashSet();
            foreach (var asset in _variantAssetSetsInProject)
            {
                if(_dependenciesVariantSetAssets.Contains(asset)) continue;
                _unusedVariantSetAssets ??= new HashSet<string>();
                _unusedVariantSetAssets.Add(asset);
            }

            DrawLists();
        }

        private static void DrawLists()
        {
            if(_tabView == null) return;
            bool enable = false;
            if (_tabView.activeTab == _VariantAssetsTab && _unusedVariantAssets != null)
            {
                enable = _unusedVariantAssets.Count > 0;
                _unuseAssetsListView.itemsSource = _unusedVariantAssets.ToList();
            }
            else if (_tabView.activeTab == _VariantSetAssetsTab && _unusedVariantSetAssets != null)
            {
                enable = _unusedVariantSetAssets.Count > 0;
                _unuseAssetsListView.itemsSource = _unusedVariantSetAssets.ToList();
            }
            
            _selectAllButton?.SetEnabled(enable);
            _deselectAllButton?.SetEnabled(enable);
            _deleteButton?.SetEnabled(false);
        }

        private void TabViewOnActiveTabChanged(Tab prevTab, Tab newTab)
        {
            DrawLists();
        }

        private void OnDestroy()
        {
            if (_tabView != null)
            {
                _tabView.activeTabChanged -= TabViewOnActiveTabChanged;
            }

            if (_searchUnusedAssetsButton != null)
            {
                _searchUnusedAssetsButton.clicked -= SearchUnusedAssetsButtonOnclicked;
            }
            
            if (_selectAllButton != null)
            {
                _selectAllButton.clicked -= SelectAllButtonOnClicked;
            }
            
            if (_deselectAllButton != null)
            {
                _deselectAllButton.clicked -= DeselectAllButtonOnClicked;
            }
            
            if (_deleteButton != null)
            {
                _deleteButton.clicked -= DeleteSelectedAssets;
            }
        }

        public static T GetAssetByGUID<T>(string guid) where T : Object
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError($"No asset found for GUID: {guid}");
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }
    }
}
