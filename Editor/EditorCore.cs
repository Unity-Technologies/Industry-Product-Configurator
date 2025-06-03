using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Object = UnityEngine.Object;
using IndustryCSE.Tool.ProductConfigurator.Runtime;
using IndustryCSE.Tool.ProductConfigurator.Settings.Editor;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;

namespace IndustryCSE.Tool.ProductConfigurator.Editor
{
    public class IconPreset
    {
        public string Name;
        public int Width;
        public int Height;
    }
    
    public static class EditorCore
    {
        public static readonly IconPreset[] IconPresets = {
            new() {Name = "128x128", Width = 128, Height = 128},
            new() {Name = "256x256", Width = 256, Height = 256},
            new() {Name = "512x512", Width = 512, Height = 512},
            new() {Name = "1024x1024", Width = 1024, Height = 1024},
            new() {Name = "2048x2048", Width = 2048, Height = 2048},
        };
        
        public const float TopMargin = 10f;
        public const float BottomMargin = 10f;
        
        #if UNITY_EDITOR_OSX
        public static string  VariantSetIconPath => PackageSettingsController.Settings.VariantIconPath.Replace("\\", "/");
        private static string VariantSetAssetsFolderPath => PackageSettingsController.Settings.VariantSetAssetPath.Replace("\\", "/");
        private static string VariantAssetsFolderPath => PackageSettingsController.Settings.VariantAssetPath.Replace("\\", "/");
        #elif UNITY_EDITOR_WIN
        public static string  VariantSetIconPath => PackageSettingsController.Settings.VariantIconPath.Replace("/", "\\");
        private static string VariantSetAssetsFolderPath => PackageSettingsController.Settings.VariantSetAssetPath.Replace("/", "\\");
        private static string VariantAssetsFolderPath => PackageSettingsController.Settings.VariantAssetPath.Replace("/", "\\");
        #endif
        
        public static AssetBase CreateReturnAsset<T>(string setName)
        {
            AssetBase newAsset = null;
            if (!Directory.Exists(VariantSetAssetsFolderPath))
            {
                Directory.CreateDirectory(VariantSetAssetsFolderPath);
            }
            
            if (typeof(T) == typeof(VariantSetAsset))
            {
                newAsset = ScriptableObject.CreateInstance<VariantSetAsset>();
            }
            else if (typeof(T) == typeof(VariantAsset))
            {
                newAsset = ScriptableObject.CreateInstance<VariantAsset>();
            }
            
            newAsset?.NewID();
            newAsset?.SetName(setName);
            if (typeof(T) == typeof(VariantSetAsset))
            {
                AssetDatabase.CreateAsset(newAsset, Path.Combine(VariantSetAssetsFolderPath, $"{newAsset.UniqueIdString}.asset"));
            }
            else if (typeof(T) == typeof(VariantAsset))
            {
                var path = Path.Combine(VariantAssetsFolderPath, $"{((VariantAsset) newAsset).UniqueIdString}.asset");
                if (!Directory.Exists(Path.GetDirectoryName(path)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                }
                AssetDatabase.CreateAsset(newAsset, path);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.SetDirty(newAsset);
            return newAsset;
        }

        public static VariantSetAsset CreateNewVariantSetAsset(VariantSetBase variantSetBase, string variantSetName)
        {
            var newVariantSet = CreateReturnAsset<VariantSetAsset>(variantSetName) as VariantSetAsset;
            variantSetBase.SetVariantSetAsset(newVariantSet);
            EditorUtility.SetDirty(variantSetBase);
            return newVariantSet;
        }
        
        public static VariantAsset CreateVariantAsset(VariantSetBase variantSetBase, string variantName)
        {
            var newVariant = CreateReturnAsset<VariantAsset>(variantName) as VariantAsset;
            variantSetBase.AddVariant(newVariant);
            EditorUtility.SetDirty(variantSetBase);
            return newVariant;
        }
        
        public static void AddVariantToVariantSet(VariantSetBase variantSetAsset, VariantAsset variantAsset)
        {
            variantSetAsset.AddVariant(variantAsset);
            EditorUtility.SetDirty(variantSetAsset);
        }

        /// <summary>
        /// Create Variant for MaterialVariantSet
        /// </summary>
        /// <param name="variantName">Variant Name</param>
        /// <param name="variantObject">Assign object</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static VariantAsset CreateVariantAsset<T>(VariantSetBase variantSetBase, string variantName,
            T variantObject)
        {
            var newVariant = CreateVariantAsset(variantSetBase, variantName);
            variantSetBase.AssignVariantObject(newVariant.UniqueIdString, variantObject);
            return newVariant;
        }
        
        public static void CaptureOptionImage(VariantSetBase variantSet, int width, int height)
        {
            //Use camera to capture 640 * 640 image and save it in asset folder
            List<VariantBase> options = variantSet.VariantBase;
            for (var i = 0; i < options.Count; i++)
            {
                variantSet.SetVariant(i, false);
                var option = options[i];
                string path = Path.Combine(VariantSetIconPath, $"{option.variantAsset.UniqueIdString}.png");
                //Debug.Log(path);
                if (!File.Exists(Path.GetDirectoryName(path)))
                {
                    // Create the directory it does not exist
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                }
                
                SceneView view = SceneView.lastActiveSceneView;
                Camera cam = view.camera;
                RenderTexture rt = new RenderTexture(width, height, 24);
                cam.targetTexture = rt;
                Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);
                cam.Render();
                RenderTexture.active = rt;
                screenShot.ReadPixels(new Rect(0,0, rt.width, rt.height), 0, 0);
                screenShot.Apply();
                RenderTexture.active = null;

                byte[] bytes = screenShot.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
                cam.targetTexture = null;
                
                variantSet.VariantSetAsset.storeCameraPosition = view.camera.transform.position;
                variantSet.VariantSetAsset.storeCameraRotation = view.camera.transform.rotation;
                variantSet.VariantSetAsset.storeCameraDistance =
                    Vector3.Distance(view.camera.transform.position, view.pivot);
                variantSet.VariantSetAsset.hasStoreCameraPositionAndRotation = true;
                
                EditorUtility.SetDirty(variantSet.VariantSetAsset);
                
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();
            }
            variantSet.SetVariant(0, false);
        }

        public static VisualElement ReturnCommonInspectorElement(Object target, UnityEditor.Editor editor)
        {
            VisualElement myInspector = new VisualElement();
            
            VariantSetBase variantSetBase = target as VariantSetBase;

            var configurationEditorBase = (editor as CustomConfigurationEditorBase);

            configurationEditorBase.VariantSetContainer = CreateContainer(0f, BottomMargin);
            configurationEditorBase.VariantSetContainer.name = "Variant Set Container";
            configurationEditorBase.VariantSetNameTextField = new TextField("Variant Set Name");
            configurationEditorBase.VariantSetNameTextField.value = variantSetBase.VariantSetAsset == null? "Name your variant set here" : variantSetBase.VariantSetAsset.VariantSetName;
            configurationEditorBase.VariantSetNameTextField.RegisterValueChangedCallback(configurationEditorBase.OnVariantSetTextFieldChange);
            configurationEditorBase.VariantSetNameTextField.RegisterCallback<KeyDownEvent>(configurationEditorBase.OnVariantSetTextFieldKeyDown);
            
            configurationEditorBase.VariantSetContainer.Add(configurationEditorBase.VariantSetNameTextField);
            
            myInspector.Add(configurationEditorBase.VariantSetContainer);
            
            configurationEditorBase.CreateVariantSetButton = new Button
            {
                text = "Create Variant Set Asset",
            };
            configurationEditorBase.CreateVariantSetButton.clicked += configurationEditorBase.OnCreateNewVariantSetAsset;
            configurationEditorBase.VariantSetContainer.Add(configurationEditorBase.CreateVariantSetButton);
            configurationEditorBase.ShowVariantSetAssetCreationBtn(variantSetBase.VariantSetAsset == null);
            
            var variantSetAssetProperty = new SerializedObject(variantSetBase).FindProperty("variantSetAsset");
            
            var so = new SerializedObject(target);
            
            configurationEditorBase.ShowVariantSetAssetCreationBtn(variantSetBase.VariantSetAsset == null);
            
            #region New Variant Button
            configurationEditorBase.VariantContainer = CreateContainer(0f, BottomMargin);
            myInspector.Add(configurationEditorBase.VariantContainer);
            configurationEditorBase.VariantNameTextField = new TextField("New Variant Name")
            {
                value = "Name your new variant here",
            };
            
            configurationEditorBase.VariantNameTextField.RegisterCallback<KeyDownEvent>(configurationEditorBase.OnVariantTextFieldKeyDown);

            configurationEditorBase.VariantContainer.Add(configurationEditorBase.VariantNameTextField);
            
            configurationEditorBase.CreateVariantButton = new Button
            {
                text = "Create New Variant"
            };
            configurationEditorBase.CreateVariantButton.clicked += configurationEditorBase.OnCreateVariant;
            configurationEditorBase.VariantContainer.Add(configurationEditorBase.CreateVariantButton);
            #endregion

            #region Draw Default Inspector

            configurationEditorBase.DefaultInspectorContainer = CreateContainer(0f, BottomMargin);
            InspectorElement.FillDefaultInspector(configurationEditorBase.DefaultInspectorContainer, so, editor);
            
            var useDefaultVariantIndexProperty = so.FindProperty("useDefaultVariantIndex");
            var useDefaultVariantIndexPropertyField = configurationEditorBase.DefaultInspectorContainer.Q<PropertyField>("PropertyField:useDefaultVariantIndex");
            
            var defaultVariantIndexProperty = so.FindProperty("defaultVariantIndex");
            var defaultVariantIndexPropertyField = configurationEditorBase.DefaultInspectorContainer.Q<PropertyField>("PropertyField:defaultVariantIndex");
            var defaultIndexSlider = new Slider(0, variantSetBase.VariantBase.Count - 1, SliderDirection.Horizontal, 1);
            defaultIndexSlider.SetValueWithoutNotify(variantSetBase.DefaultVariantIndex);
            
            var index = configurationEditorBase.DefaultInspectorContainer.IndexOf(defaultVariantIndexPropertyField);
            defaultVariantIndexPropertyField.style.display = DisplayStyle.None;
            configurationEditorBase.DefaultInspectorContainer.Insert( index, defaultIndexSlider);
            
            var defaultVariantIndexText = new Label(variantSetBase.VariantBase != null && variantSetBase.VariantBase.Count > 0 && variantSetBase.VariantBase.All(x => x.variantAsset != null) ? $"Default Variant Index: {variantSetBase.DefaultVariantIndex} - {variantSetBase.VariantBase[variantSetBase.DefaultVariantIndex].variantAsset.VariantName}" : string.Empty);
            configurationEditorBase.DefaultInspectorContainer.Insert(index + 2, defaultVariantIndexText);
            useDefaultVariantIndexPropertyField.TrackPropertyValue(useDefaultVariantIndexProperty, property =>
            {
                if (defaultVariantIndexPropertyField != null)
                {
                    defaultIndexSlider.style.display = variantSetBase.UseDefaultVariantIndex? DisplayStyle.Flex : DisplayStyle.None;
                    defaultVariantIndexText.style.display = variantSetBase.UseDefaultVariantIndex? DisplayStyle.Flex : DisplayStyle.None;
                }
            });
            
            defaultIndexSlider.RegisterValueChangedCallback(evt =>
            {
                defaultVariantIndexProperty.intValue = (int) evt.newValue; 
                so.ApplyModifiedProperties();
                so.Update();
                defaultVariantIndexText.text = $"Default Variant Index: {variantSetBase.DefaultVariantIndex} - {variantSetBase.VariantBase[variantSetBase.DefaultVariantIndex].variantAsset.VariantName}";
            });
            
            defaultIndexSlider.style.display = variantSetBase.UseDefaultVariantIndex? DisplayStyle.Flex : DisplayStyle.None;
            defaultVariantIndexText.style.display = variantSetBase.UseDefaultVariantIndex? DisplayStyle.Flex : DisplayStyle.None;
            
            myInspector.Add(configurationEditorBase.DefaultInspectorContainer);
            if (PackageSettingsController.Settings.UseAdvancedSettings)
            {
                configurationEditorBase.DefaultInspectorContainer.style.display = DisplayStyle.Flex;
            }
            else if(!PackageSettingsController.Settings.UseAdvancedSettings && variantSetBase.VariantSetAsset != null)
            {
                configurationEditorBase.DefaultInspectorContainer.style.display = DisplayStyle.Flex;
            }
            else
            {
                configurationEditorBase.DefaultInspectorContainer.style.display = DisplayStyle.None;
            }

            if (!PackageSettingsController.Settings.UseAdvancedSettings)
            {
                var variantSetAssetObjectField = configurationEditorBase.DefaultInspectorContainer.Q<PropertyField>("PropertyField:variantSetAsset");
                if (variantSetAssetObjectField != null)
                {
                    variantSetAssetObjectField.style.display = DisplayStyle.None;
                }
                
                var variantsObjectField = configurationEditorBase.DefaultInspectorContainer.Q<PropertyField>("PropertyField:variants");
                if (variantsObjectField != null)
                {
                    variantsObjectField.name = "VariantsList";
                    var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                        "Packages/com.unity.industrycse.productconfigurator/Editor/Resources/VariantBaseInspectorStyle.uss");
                    if (styleSheet != null)
                    {
                        myInspector.styleSheets.Add(styleSheet);
                    }
                }
            }

            #endregion

            var objectField = configurationEditorBase.DefaultInspectorContainer.Q<PropertyField>("PropertyField:variantSetAsset");
            
            objectField.TrackPropertyValue(variantSetAssetProperty, configurationEditorBase.TrackVariantSetAssetProperty);
            
            var variantArraySizeProp = so.FindProperty("variants.Array.size");
            
            #region Slider
            configurationEditorBase.VariantSliderContainer = CreateContainer(0f, BottomMargin);
            configurationEditorBase.VariantSliderContainer.name = "Variant Slider Container";
            configurationEditorBase.VariantSliderContainer.Add(new Label("Drag this slider to quickly preview different variants"));
            configurationEditorBase.VariantSliderContainer.style.display = variantSetBase.VariantBase.Count <= 1 ? DisplayStyle.None : DisplayStyle.Flex;
            configurationEditorBase.VariantSlider =
                new Slider("Variant Slider", 0, variantArraySizeProp.intValue - 1, SliderDirection.Horizontal, 1);
            configurationEditorBase.VariantSlider.TrackPropertyValue(variantArraySizeProp, configurationEditorBase.OnVariantCountChanged);
            configurationEditorBase.VariantSlider.RegisterValueChangedCallback(configurationEditorBase.OnVariantSliderChanged);
            configurationEditorBase.VariantSliderContainer.Add(configurationEditorBase.VariantSlider);
            myInspector.Add(configurationEditorBase.VariantSliderContainer);
            #endregion
            
            #region Variant Drop-down
            if (variantSetBase.VariantBase.All(x => x.variantAsset != null))
            {
                var variantChoice = variantSetBase.VariantBase.Select(x => x.variantAsset.VariantName).ToList();
                configurationEditorBase.VariantDropdown = new DropdownField("Variant Dropdown", variantChoice, variantSetBase.CurrentSelectionIndex);
                configurationEditorBase.VariantDropdown.RegisterValueChangedCallback(configurationEditorBase.OnVariantDropdownChanged);
                configurationEditorBase.VariantSliderContainer.Add(configurationEditorBase.VariantDropdown);
            }
            #endregion
            
            #region Size Dropdown and Capture Button
            
            configurationEditorBase.CaptureImageContainer = CreateContainer(0f, BottomMargin);
            configurationEditorBase.CaptureImageContainer.name = "Capture Image Container";
            configurationEditorBase.CaptureImageContainer.Add(new Label("Capture the icon for each variant"));
            configurationEditorBase.CaptureImageContainer.style.display = variantSetBase.VariantBase.Count <= 1 ? DisplayStyle.None : DisplayStyle.Flex;
            var choices = IconPresets.Select(x => x.Name).ToList();
            configurationEditorBase.CaptureSizeDropdown = new DropdownField("Icon Size", choices, 0);
            configurationEditorBase.CaptureImageContainer.Add(configurationEditorBase.CaptureSizeDropdown);
            
            
            configurationEditorBase.UsePreviousLocationButton = new Button
            {
                text = "Move camera to previous location"
            };
                
            configurationEditorBase.UsePreviousLocationButton.clicked += configurationEditorBase.OnUsePreviousLocationButtonClicked;
                
            configurationEditorBase.CaptureImageContainer.Add(configurationEditorBase.UsePreviousLocationButton);
            
            if(variantSetBase.VariantSetAsset != null && variantSetBase.VariantSetAsset.hasStoreCameraPositionAndRotation)
            {
                configurationEditorBase.UsePreviousLocationButton.style.display = DisplayStyle.Flex;
            }
            else
            {
                configurationEditorBase.UsePreviousLocationButton.style.display = DisplayStyle.None;
            }
            
            configurationEditorBase.CaptureImageButton = new Button
            {
                text = "Capture Variant Icon"
            };
            
            configurationEditorBase.CaptureImageButton.clicked += configurationEditorBase.OnCaptureImageButtonClicked;
            configurationEditorBase.CaptureImageContainer.Add(configurationEditorBase.CaptureImageButton);
            myInspector.Add(configurationEditorBase.CaptureImageContainer);
            #endregion
            
            configurationEditorBase.ShowVariantContainer(variantSetBase.VariantSetAsset != null);
            
            return myInspector;
        }

        public static VisualElement CreateContainer(float topMargin, float bottomMargin)
        {
            VisualElement emptySpace = new VisualElement
            {
                style =
                {
                    borderBottomLeftRadius = 5f,
                    borderBottomRightRadius = 5f,
                    borderTopLeftRadius = 5f,
                    borderTopRightRadius = 5f,
                    marginTop = new Length(topMargin, LengthUnit.Pixel),
                    marginBottom = new Length(bottomMargin, LengthUnit.Pixel),
                    backgroundColor = Color.black,
                    paddingLeft = new Length(5f, LengthUnit.Pixel),
                    paddingRight = new Length(5f, LengthUnit.Pixel),
                    paddingTop = new Length(5f, LengthUnit.Pixel),
                    paddingBottom = new Length(5f, LengthUnit.Pixel)
                }
            };
            return emptySpace;
        }
    }
}
