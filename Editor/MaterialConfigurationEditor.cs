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
    public class MaterialConfigurationEditor : CustomConfigurationEditorBase
    {
        private ObjectField materialField;
        private Material targetMaterial;
        private MaterialConfiguration materialConfiguration;
        private int setIndex;
        private int prevIndex;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            materialConfiguration = target as MaterialConfiguration;
        }
        
        public override VisualElement CreateInspectorGUI()
        {
            var myInspector = base.CreateInspectorGUI();
            
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
            
            // Return the finished inspector UI
            return myInspector;
        }

        /*public override VisualElement CreateInspectorGUI()
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
        }*/

        protected override void OnDisable()
        {
            base.OnDisable();
            materialField.UnregisterValueChangedCallback(OnMaterialValueChanged);
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


