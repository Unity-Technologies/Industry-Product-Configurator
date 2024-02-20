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
    [CustomEditor(typeof(MaterialVariantSet))]
    public class MaterialVariantSetEditor : CustomConfigurationEditorBase
    {
        private ObjectField materialField;
        private Material targetMaterial;
        private MaterialVariantSet _materialVariantSet;
        private int setIndex;
        private int prevIndex;
        private VisualElement materialCaptureContainer;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            _materialVariantSet = target as MaterialVariantSet;
        }
        
        public override VisualElement CreateInspectorGUI()
        {
            var myInspector = base.CreateInspectorGUI();
            
            //myInspector.Add(EditorCore.CreateContainer());

            materialCaptureContainer = EditorCore.CreateContainer(EditorCore.TopMargin, 0f);
            
            materialField = new ObjectField("Ref. Material")
            {
                objectType = typeof(Material),
            };

            materialField.RegisterValueChangedCallback(OnMaterialValueChanged);

            materialCaptureContainer.Add(materialField);

            Button captureButton = new Button
            {
                text = "Capture MeshRenderers in children with this material"
            };
            captureButton.clicked += CaptureButtonOnClicked;
            
            materialCaptureContainer.Add(captureButton);
            
            myInspector.Add(materialCaptureContainer);
            
            // Return the finished inspector UI
            return myInspector;
        }

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
            _materialVariantSet.RenderersDetails = GetSameMaterialRenderers(targetMaterial);
        }

        private RendererDetail[] GetSameMaterialRenderers(Material material )
        {
            _materialVariantSet.RenderersDetails = new RendererDetail[] { };
            List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
            List<RendererDetail> rendererDetails = new List<RendererDetail>();
            meshRenderers = new List<MeshRenderer>(_materialVariantSet.GetComponentsInChildren<MeshRenderer>(true));
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


