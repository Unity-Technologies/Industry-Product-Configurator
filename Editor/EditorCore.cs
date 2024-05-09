using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Object = UnityEngine.Object;

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
        public static string  VariantSetIconPath => Path.Combine(Application.dataPath, "Data", "Icons");
        #elif UNITY_EDITOR_WIN
        public static string  VariantSetIconPath => Path.Combine(Application.dataPath.Replace("/", "\\"), "Data", "Icons");
        #endif
        
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
                
                variantSet.VariantSetAsset.storeCameraPosition = view.pivot;
                variantSet.VariantSetAsset.storeCameraRotation = view.rotation;
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
            
            configurationEditorBase.VariantSetNameTextField = new TextField("Variant Set Name");
            configurationEditorBase.VariantSetNameTextField.value = variantSetBase.VariantSetAsset == null? "Name your variant set here" : variantSetBase.VariantSetAsset.VariantSetName;
            configurationEditorBase.VariantSetNameTextField.RegisterValueChangedCallback(configurationEditorBase.OnVariantSetTextFieldChange);
            
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
            configurationEditorBase.DefaultInspectorContainer = CreateContainer(0f, 0f);
            InspectorElement.FillDefaultInspector(configurationEditorBase.DefaultInspectorContainer, so, editor);
            myInspector.Add(configurationEditorBase.DefaultInspectorContainer);

            var objectField = configurationEditorBase.DefaultInspectorContainer.Q<PropertyField>("PropertyField:variantSetAsset");
            
            objectField.TrackPropertyValue(variantSetAssetProperty, configurationEditorBase.TrackVariantSetAssetProperty);
            
            var variantArraySizeProp = so.FindProperty("variants.Array.size");
            
            #region New Variant Button
            configurationEditorBase.VariantContainer = CreateContainer(TopMargin, 0f);
            myInspector.Add(configurationEditorBase.VariantContainer);
            configurationEditorBase.VariantNameTextField = new TextField("New Variant Name")
            {
                value = "Name your new variant here",
            };

            configurationEditorBase.VariantContainer.Add(configurationEditorBase.VariantNameTextField);
            
            configurationEditorBase.CreateVariantButton = new Button
            {
                text = "Create New Variant"
            };
            configurationEditorBase.CreateVariantButton.clicked += configurationEditorBase.OnCreateVariant;
            configurationEditorBase.VariantContainer.Add(configurationEditorBase.CreateVariantButton);
            #endregion
            
            #region Slider
            configurationEditorBase.VariantSliderContainer = CreateContainer(TopMargin, 0f);
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
            
            #region Size Dropdown and Capture Button
            
            configurationEditorBase.CaptureImageContainer = CreateContainer(TopMargin, 0f);
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
                    borderBottomLeftRadius = 2.5f,
                    borderBottomRightRadius = 2.5f,
                    borderTopLeftRadius = 2.5f,
                    borderTopRightRadius = 2.5f,
                    marginTop = new Length(topMargin, LengthUnit.Pixel),
                    marginBottom = new Length(bottomMargin, LengthUnit.Pixel),
                    borderTopWidth = 1f,
                    borderTopColor = Color.grey,
                    borderBottomColor = Color.grey,
                    borderBottomWidth = 1f,
                    borderLeftColor = Color.grey,
                    borderLeftWidth = 1f,
                    borderRightColor = Color.grey,
                    borderRightWidth = 1f,
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
