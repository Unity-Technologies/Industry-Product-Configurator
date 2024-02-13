using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace IndustryCSE.Tool.ProductConfigurator.Editor
{
    [CustomEditor(typeof(MaterialConfiguration))]
    public class MaterialConfigurationEditor : UnityEditor.Editor
    {
        private ObjectField materialField;
        private Slider optionSlider;
        private Material targetMaterial;
        private MaterialConfiguration materialConfiguration;
        private int setIndex;
        private int prevIndex;

        private void OnEnable()
        {
            materialConfiguration = target as MaterialConfiguration;
        }

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement myInspector = new VisualElement();
            InspectorElement.FillDefaultInspector(myInspector, serializedObject, this);
            
            materialField = new ObjectField("Ref. Material")
            {
                objectType = typeof(Material)
            };

            materialField.RegisterValueChangedCallback(OnMaterialValueChanged);

            myInspector.Add(materialField);

            Button captureButton = new Button
            {
                text = "Capture MeshRenderers in children with this material"
            };
            captureButton.clicked += CaptureButtonOnClicked;
            
            myInspector.Add(captureButton);
            
            optionSlider = new Slider("Option Slider", 0, materialConfiguration.OptionDetails.Count - 1, SliderDirection.Horizontal, 1);

            optionSlider.RegisterValueChangedCallback(OnSliderOptionChanged);
            var prop = serializedObject.FindProperty("optionDetails.Array.size");
            optionSlider.TrackPropertyValue(prop, OnOptionCountChanged);
            myInspector.Add(optionSlider);
            
            // Return the finished inspector UI
            return myInspector;
        }

        private void OnDisable()
        {
            materialField.UnregisterValueChangedCallback(OnMaterialValueChanged);
            optionSlider.UnregisterValueChangedCallback(OnSliderOptionChanged);
        }

        private void OnOptionCountChanged(SerializedProperty obj)
        {
            optionSlider.highValue = obj.intValue - 1;
            optionSlider.value = Mathf.Min(optionSlider.value, optionSlider.highValue);
        }

        private void OnSliderOptionChanged(ChangeEvent<float> evt)
        {
            materialConfiguration.SetOption((int)evt.newValue);
        }

        private void OnMaterialValueChanged(ChangeEvent<Object> evt)
        {
            targetMaterial = evt.newValue as Material;
        }

        private void CaptureButtonOnClicked()
        {
            materialConfiguration.RenderersDetails = GetSameMaterialRenderers(targetMaterial);
        }

        private RendererDetail[] GetSameMaterialRenderers(Material material )
        {
            materialConfiguration.RenderersDetails = new RendererDetail[] { };
            List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
            List<RendererDetail> rendererDetails = new List<RendererDetail>();
            meshRenderers = new List<MeshRenderer>(materialConfiguration.GetComponentsInChildren<MeshRenderer>(true));
            for(int i = 0;i<meshRenderers.Count; i++)
            {
                if (meshRenderers[i].sharedMaterials.Contains(material))
                {
                    for (int j = 0; j < meshRenderers[i].sharedMaterials.Length; j++)
                    {
                        if (meshRenderers[i].sharedMaterials[j] == material)
                        {
                            RendererDetail rendererDetail = new RendererDetail();
                            rendererDetail.renderer = meshRenderers[i];
                            rendererDetail.materialsSlotIndex = j;
                            rendererDetails.Add(rendererDetail);
                        }
                    }
                    
                }
            }

            return rendererDetails.ToArray();
        }
    }
}


