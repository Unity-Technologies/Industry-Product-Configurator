using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;
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
        public static IconPreset[] IconPresets = {
            new() {Name = "128x128", Width = 128, Height = 128},
            new() {Name = "256x256", Width = 256, Height = 256},
            new() {Name = "512x512", Width = 512, Height = 512},
            new() {Name = "1024x1024", Width = 1024, Height = 1024},
            new() {Name = "2048x2048", Width = 2048, Height = 2048},
        };
        
        public static string ConfigurationIconPath => Path.Combine(Application.dataPath, "Configuration Data", "Icons");
        
        public static void CaptureOptionImage(ConfigurationBase configuration, int width, int height)
        {
            //Use camera to capture 640 * 640 image and save it in asset folder
            Configuration configurationSO = configuration.Configuration;
            List<OptionDetailBase> options = configuration.Options;
            for (var i = 0; i < options.Count; i++)
            {
                configuration.SetOption(i);
                var option = options[i];
                string path = Path.Combine(ConfigurationIconPath,
                    $"{configurationSO.ConfigurationName} - {configurationSO.uniqueId}", $"{option.configurationOption.name} - {option.configurationOption.UniqueIdString}.png");

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
                
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();
            }
            configuration.SetOption(0);
        }

        public static VisualElement ReturnCommonInspectorElement(Object target, ref Slider optionSlider, UnityEditor.Editor editor, 
            Action<SerializedProperty> onPropertyChanged, EventCallback<ChangeEvent<float>> sliderEventCallback,  
            ref Button captureImageBtn, Action onCaptureImageButtonClicked,
            ref DropdownField captureSizeDropdown)
        {
            VisualElement myInspector = new VisualElement();
            var so = new SerializedObject(target);
            InspectorElement.FillDefaultInspector(myInspector, so, editor);
            var prop = so.FindProperty("optionDetails.Array.size");
            
            #region Slider
            optionSlider = new Slider("Option Slider", 0,  prop.intValue - 1, SliderDirection.Horizontal, 1);
            optionSlider.TrackPropertyValue(prop, onPropertyChanged);
            optionSlider.RegisterValueChangedCallback(sliderEventCallback);
            myInspector.Add(optionSlider);
            #endregion

            #region Size Dropdown
            var choices = IconPresets.Select(x => x.Name).ToList();
            captureSizeDropdown = new DropdownField("Capture Size", choices, 0);
            myInspector.Add(captureSizeDropdown);
            #endregion
            
            #region Capture Button
            captureImageBtn = new Button
            {
                text = "Capture Option Icon"
            };
            captureImageBtn.clicked += onCaptureImageButtonClicked;
            myInspector.Add(captureImageBtn);
            #endregion
            
            return myInspector;
        }
    }
}
