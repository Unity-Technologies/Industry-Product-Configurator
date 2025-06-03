using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.IO;
using System;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;

namespace IndustryCSE.Tool.ProductConfigurator.Settings.Editor
{
    public static class AssetRemoveConfirmation
    {
        private static Action DelayAction;
        
        private static void RemoveAssets(List<string> paths, List<string> outFailPaths)
        {
            AssetDatabase.MoveAssetsToTrash(paths.ToArray(), outFailPaths);
            AssetDatabase.Refresh();
        }

        private static (List<string> assets, List<string> failPath) ReturnVariantAssetsPath(List<VariantAsset> assets)
        {
            var paths = new List<string>();
            var outFailPaths = new List<string>();
            foreach (var variantAsset in assets)
            {
                if (variantAsset == null) continue;
                var path = AssetDatabase.GetAssetPath(variantAsset);
                if(string.IsNullOrEmpty(path)) continue;
                if (!outFailPaths.Contains(Directory.GetParent(path).FullName))
                {
                    outFailPaths.Add(Directory.GetParent(path).FullName);
                }
                paths.Add(path);
                    
                if (variantAsset.icon == null) continue;
                path = AssetDatabase.GetAssetPath(variantAsset.icon);
                if(string.IsNullOrEmpty(path)) continue;
                if (!outFailPaths.Contains(Directory.GetParent(path).FullName))
                {
                    outFailPaths.Add(Directory.GetParent(path).FullName);
                }
                paths.Add(path);
            }

            return (paths, outFailPaths);
        }
    }
    
    [InitializeOnLoad]
    public static class PackageSettingsController
    {
        const string settingsPath = "Assets/Product Configurator/Editor/Resources/Settings.asset";
        
        public static ProductConfiguratorSettings Settings => GetSettings(); 
        
        static PackageSettingsController()
        {
            CreateSettings();
        }
        
        private static ProductConfiguratorSettings CreateSettings()
        {
            ProductConfiguratorSettings settings =
                AssetDatabase.LoadAssetAtPath<ProductConfiguratorSettings>(settingsPath);

            if (settings != null) return settings;
            
            string directoryPath = Path.GetDirectoryName(settingsPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            
            settings = ScriptableObject.CreateInstance<ProductConfiguratorSettings>();
            AssetDatabase.CreateAsset(settings, settingsPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        public static ProductConfiguratorSettings GetSettings()
        {
            ProductConfiguratorSettings settings =
                AssetDatabase.LoadAssetAtPath<ProductConfiguratorSettings>(settingsPath);

            return settings == null ? CreateSettings() : settings;
        }
    }

    static class ProductConfiguratorSettingsUIElementRegister
    {
        private static List<string> RemoveBehaviourOptions = new List<string>()
            {"Ask Every Time", "Remove Without Asking", "Keep Without Asking"};
        
        private static Label variantSetAssetPathLabel, variantAssetPathLabel, variantIconPathLabel;
        
        [SettingsProvider]
        public static SettingsProvider CreateProductConfiguratorSettingsProvider()
        {
            var provider = new SettingsProvider("Project/Product Configurator", SettingsScope.Project)
            {
                label = "Product Configurator",
                activateHandler = (searchContext, rootElement) =>
                {
                    var settings = PackageSettingsController.GetSettings();
                    
                    var container = new VisualElement
                    {
                        style =
                        {
                            marginTop = 1,
                            marginLeft = 9,
                            marginRight = 9,
                            marginBottom = 1
                        }
                    };

                    #region Title
                    var label = new Label("Product Configurator Settings")
                    {
                        style =
                        {
                            unityFontStyleAndWeight = FontStyle.Bold,
                            fontSize = 20
                        }
                    };
                    
                    container.Add(label);
                    #endregion
                    
                    #region Advance Mode Toggle
                    var toggle = new Toggle("Advanced Mode")
                    {
                        value = settings.UseAdvancedSettings,
                        tooltip = "Enable advanced mode for the product configurator.",
                        style =
                        {
                            marginTop = new Length(10, LengthUnit.Pixel)
                        }
                    };
                    toggle.RegisterValueChangedCallback(OnAdvancedSettingsToggleChange);
                    
                    container.Add(toggle);
                    #endregion
                    
                    #region Variant Set Asset Path

                    var settingVariantSetPathPerPlatform = settings.VariantSetAssetPath;
                    
                    #if UNITY_EDITOR_OSX
                    settingVariantSetPathPerPlatform = settingVariantSetPathPerPlatform.Replace("\\", "/");
                    #elif UNITY_EDITOR_WIN
                    settingVariantSetPathPerPlatform = settingVariantSetPathPerPlatform.Replace("/", "\\");
                    #endif
                    
                    var variantSetAssetPathField = ReturnPathField("Variant Set Asset Path",
                        out variantSetAssetPathLabel,
                        settingVariantSetPathPerPlatform,
                        "The path where the variant set asset will be saved.",
                        PickAssetSetPath);
                    container.Add(variantSetAssetPathField);
                    #endregion
                    
                    #region Variant Asset Path
                    
                    var settingVariantPathPerPlatform = settings.VariantAssetPath;
                    
                    #if UNITY_EDITOR_OSX
                    settingVariantPathPerPlatform = settingVariantPathPerPlatform.Replace("\\", "/");
                    #elif UNITY_EDITOR_WIN
                    settingVariantPathPerPlatform = settingVariantPathPerPlatform.Replace("/", "\\");
                    #endif
                    
                    var variantAssetPathField = ReturnPathField("Variant Asset Path",
                        out variantAssetPathLabel,
                        settingVariantPathPerPlatform,
                        "The path where the variant asset will be saved.",
                        PickAssetPath);
                    container.Add(variantAssetPathField);
                    #endregion
                    
                    #region Variant Icon Path
                    
                    var settingIconPathPerPlatform = settings.VariantIconPath;
                    
                    #if UNITY_EDITOR_OSX
                    settingIconPathPerPlatform = settingIconPathPerPlatform.Replace("\\", "/");
                    #elif UNITY_EDITOR_WIN
                    settingIconPathPerPlatform = settingIconPathPerPlatform.Replace("/", "\\");
                    #endif
                    
                    var variantIconPathField = ReturnPathField("Variant Icon Path",
                        out variantIconPathLabel, settingIconPathPerPlatform,
                        "The path where the variant icon will be saved.",
                        PickIconPath);
                    container.Add(variantIconPathField);
                    #endregion

                    #region Remove Behaviour

                    var removeBehaviourDropdownMenu = new DropdownField(RemoveBehaviourOptions, 
                        (int)settings.RemoveBehaviour)
                    {
                        tooltip = "Should the assets be removed without asking, ask every time or keep without asking.",
                        style =
                        {
                            marginTop = new Length(10, LengthUnit.Pixel)
                        },
                        label = "Variant Set / Variant Remove Behaviour"
                    };
                    removeBehaviourDropdownMenu.RegisterValueChangedCallback(RemoveBehaviourDropdownCallback);
                    container.Add(removeBehaviourDropdownMenu);

                    #endregion
                    
                    rootElement.Add(container);
                },
                keywords = new HashSet<string>(new []{"Product Configurator", "Product Configurator Settings", "Product Configurator Advanced Settings"})
            };

            return provider;
        }

        private static VisualElement ReturnPathField(string title, out Label valueLabel, string value, string toolTip, Action buttonAction)
        {
            var visualElement = new VisualElement
            {
                style =
                {
                    width = new Length(100, LengthUnit.Percent),
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.SpaceBetween,
                    flexGrow = 1,
                    marginTop = new Length(10, LengthUnit.Pixel),
                    paddingLeft = new Length(3, LengthUnit.Pixel)
                },
                tooltip = toolTip
            };
            var titleLabelWidth = new Length(20, LengthUnit.Percent);
            var titleLabel = new Label(title)
            {
                style =
                {
                    maxWidth = titleLabelWidth,
                    flexGrow = 1
                }
            };
            
            visualElement.Add(titleLabel);
            
            valueLabel = new Label(value)
            {
                style =
                {
                    flexGrow = 1
                }
            };
            visualElement.Add(valueLabel);
            
            var pathPicker = new Button
            {
                style =
                {
                    maxWidth = new Length(45, LengthUnit.Percent),
                },
                text = "Select Path"
            };
            pathPicker.clicked += buttonAction;

            visualElement.Add(pathPicker);
            
            return visualElement;
        }

        private static void PickAssetSetPath()
        {
            //Open File Panel
            var path = EditorUtility.OpenFolderPanel("Select Variant Set Asset Path", "Assets", "");
            if(string.IsNullOrEmpty(path)) return;
            var assetFolder = Directory.GetParent(Application.dataPath).FullName;
            
            path = path.Remove(0, assetFolder.Length + 1);
            
            #if UNITY_EDITOR_OSX
            path = path.Replace("\\", "/");
            #elif UNITY_EDITOR_WIN
            path = path.Replace("/", "\\");
            #endif
            
            variantSetAssetPathLabel.text = path;
            var settings = PackageSettingsController.GetSettings();
            settings.SetVariantSetAssetPath(path);
            EditorUtility.SetDirty(settings);
        }
        
        private static void PickAssetPath()
        {
            //Open File Panel
            var path = EditorUtility.OpenFolderPanel("Select Variant Asset Path", "Assets", "");
            if(string.IsNullOrEmpty(path)) return;
            var assetFolder = Directory.GetParent(Application.dataPath).FullName;
            
            path = path.Remove(0, assetFolder.Length + 1);
            
            #if UNITY_EDITOR_OSX
            path = path.Replace("\\", "/");
            #elif UNITY_EDITOR_WIN
            path = path.Replace("/", "\\");
            #endif
            
            variantAssetPathLabel.text = path;
            var settings = PackageSettingsController.GetSettings();
            settings.SetVariantAssetPath(path);
            EditorUtility.SetDirty(settings);
        }
        
        private static void PickIconPath()
        {
            //Open File Panel
            var path = EditorUtility.OpenFolderPanel("Select Variant Icon Path", "Assets", "");
            if(string.IsNullOrEmpty(path)) return;

            var assetFolder = Directory.GetParent(Application.dataPath).FullName;
            
            path = path.Remove(0, assetFolder.Length + 1);
            
            #if UNITY_EDITOR_OSX
            path = path.Replace("\\", "/");
            #elif UNITY_EDITOR_WIN
            path = path.Replace("/", "\\");
            #endif
            
            variantIconPathLabel.text = path;
            var settings = PackageSettingsController.GetSettings();
            settings.SetVariantIconPath(path);
            EditorUtility.SetDirty(settings);
        }

        private static void RemoveBehaviourDropdownCallback(ChangeEvent<string> arg)
        {
            var index = RemoveBehaviourOptions.IndexOf(arg.newValue);
            var settings = PackageSettingsController.GetSettings();
            settings.SetRemoveBehaviour(index);
        }
        
        private static void OnAdvancedSettingsToggleChange(ChangeEvent<bool> evt)
        {
            var settings = PackageSettingsController.GetSettings();
            settings.SetAdvancedSettings(evt.newValue);
            EditorUtility.SetDirty(settings);
            var selectedObject = Selection.activeGameObject;
            EditorUtility.SetDirty(selectedObject);
            Selection.activeGameObject = null;
            EditorApplication.delayCall += () => Selection.activeGameObject = selectedObject;
        }
    }
}
