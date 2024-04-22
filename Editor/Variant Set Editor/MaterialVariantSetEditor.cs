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
        private ObjectField _materialField;
        private ObjectField _targetParentField;
        private Button _captureButton;
        private Material _targetMaterial;
        private MaterialVariantSet _materialVariantSet;
        private int _setIndex;
        private int _prevIndex;
        private VisualElement _materialCaptureContainer;
        private GameObject _targetParent;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            _materialVariantSet = target as MaterialVariantSet;
        }
        
        public override VisualElement CreateInspectorGUI()
        {
            var myInspector = base.CreateInspectorGUI();

            var index = DefaultInspectorContainer.parent.IndexOf(DefaultInspectorContainer);

            _materialCaptureContainer = EditorCore.CreateContainer(EditorCore.TopMargin, 0f);

            Label tipsLabel = new Label("Select a material to capture all MeshRenderers in children from the target parent with this material.")
                {
                    style =
                    {
                        whiteSpace = WhiteSpace.Normal
                    }
                };

            _materialCaptureContainer.Add(tipsLabel);
            
            _targetParentField = new ObjectField("Target Parent")
            {
                objectType = typeof(GameObject),
                value =_targetParent
            };
            
            _materialCaptureContainer.Add(_targetParentField);

            _targetParentField.RegisterValueChangedCallback(OnTargetParentValueChanged);
            
            _materialField = new ObjectField("Ref. Material")
            {
                objectType = typeof(Material),
                value = _targetMaterial
            };

            _materialField.RegisterValueChangedCallback(OnMaterialValueChanged);

            _materialField.style.display = DisplayStyle.None;

            _materialCaptureContainer.Add(_materialField);

            _captureButton = new Button
            {
                text = "Capture MeshRenderers in children with this material"
            };
            _captureButton.clicked += CaptureButtonOnClicked;

            _captureButton.style.display = DisplayStyle.None;
            
            _materialCaptureContainer.Add(_captureButton);
            
            myInspector.Insert(index + 1, _materialCaptureContainer);
            
            _materialField.style.display = _targetParentField.value != null ? DisplayStyle.Flex : DisplayStyle.None;
            _captureButton.style.display = _materialField.value != null && _targetParentField.value != null ? DisplayStyle.Flex : DisplayStyle.None;
            
            // Return the finished inspector UI
            return myInspector;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _materialField.UnregisterValueChangedCallback(OnMaterialValueChanged);
            _targetParentField.UnregisterValueChangedCallback(OnTargetParentValueChanged);
            if (_captureButton != null)
            {
                _captureButton.clicked -= CaptureButtonOnClicked;
            }
            _targetMaterial = null;
            _targetParent = null;
        }

        private void OnTargetParentValueChanged(ChangeEvent<Object> evt)
        {
            _targetParent = evt.newValue as GameObject;
            _materialField.style.display = evt.newValue != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (_materialField.style.display == DisplayStyle.None)
            {
                _materialField.SetValueWithoutNotify(null);
            }
        }

        private void OnMaterialValueChanged(ChangeEvent<Object> evt)
        {
            _targetMaterial = evt.newValue as Material;
            _captureButton.style.display = evt.newValue != null ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void CaptureButtonOnClicked()
        {
            _materialVariantSet.RenderersDetails ??= new List<RendererDetail>();
            _materialVariantSet.RenderersDetails.AddRange(GetSameMaterialRenderers(_targetMaterial));
            _materialVariantSet.RenderersDetails = _materialVariantSet.RenderersDetails.Distinct(new RendererDetailComparer()).ToList();
        }

        private class RendererDetailComparer : IEqualityComparer<RendererDetail>
        {
            public bool Equals(RendererDetail x, RendererDetail y)
            {
                //Check whether the compared objects reference the same data.
                if (Object.ReferenceEquals(x, y)) return true;

                //Check whether any of the compared objects is null.
                if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                    return false;

                //Check whether the products' properties are equal.
                return x.materialsSlotIndex == y.materialsSlotIndex && x.renderer == y.renderer;
            }

            public int GetHashCode(RendererDetail obj)
            {
                //Check whether the object is null
                if (Object.ReferenceEquals(obj, null)) return 0;

                //Get hash code for the Name field if it is not null.
                int hashProductName = obj.renderer == null ? 0 : obj.renderer.GetHashCode();

                //Get hash code for the Code field.
                int hashProductCode = obj.materialsSlotIndex.GetHashCode();

                //Calculate the hash code for the product.
                return hashProductName ^ hashProductCode;
            }
        }
        
        private List<RendererDetail> GetSameMaterialRenderers(Material material )
        {
            List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
            List<RendererDetail> rendererDetails = new List<RendererDetail>();
            meshRenderers = new List<MeshRenderer>(_targetParent.GetComponentsInChildren<MeshRenderer>(true));
            for(int i = 0;i<meshRenderers.Count; i++)
            {
                if (meshRenderers[i].sharedMaterials.Contains(material))
                {
                    for (int j = 0; j < meshRenderers[i].sharedMaterials.Length; j++)
                    {
                        if (meshRenderers[i].sharedMaterials[j] == material)
                        {
                            RendererDetail rendererDetail = new RendererDetail
                            {
                                renderer = meshRenderers[i],
                                materialsSlotIndex = j
                            };
                            rendererDetails.Add(rendererDetail);
                        }
                    }
                    
                }
            }

            return rendererDetails;
        }
    }
}


