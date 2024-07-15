using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using IndustryCSE.Tool.ProductConfigurator.Runtime;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;
using IndustryCSE.Tool.ProductConfigurator.Settings.Editor;

namespace IndustryCSE.Tool.ProductConfigurator.Editor
{
    public abstract class CustomConfigurationEditorBase : UnityEditor.Editor
    {
        public VisualElement DefaultInspectorContainer;
        
        public VisualElement VariantSetContainer;
        public TextField VariantSetNameTextField;
        public Button CreateVariantSetButton;
        
        public VisualElement VariantContainer;
        public TextField VariantNameTextField;
        public Button CreateVariantButton;
        
        public VisualElement VariantSliderContainer;
        public Slider VariantSlider;
        
        public VisualElement CaptureImageContainer;
        public DropdownField CaptureSizeDropdown;
        public Button CaptureImageButton;
        public Button UsePreviousLocationButton;
        
        protected List<VariantBase> m_OriginalVariants;

        protected virtual void OnEnable()
        {
            var variantSetBase = target as VariantSetBase;
            m_OriginalVariants = new List<VariantBase>(variantSetBase.VariantBase);
        }

        public override VisualElement CreateInspectorGUI()
        {
            var myInspector = EditorCore.ReturnCommonInspectorElement(target, this);
            // Return the finished inspector UI
            return myInspector;
        }

        protected virtual void OnDestroy()
        {
            if (Application.isPlaying) return;
            var variantSetBase = target as VariantSetBase;
            SceneTracker.CheckVariantSet(variantSetBase);
        }

        protected virtual void OnDisable()
        {
            if (VariantSlider != null)
            {
                VariantSlider.UnregisterValueChangedCallback(OnVariantSliderChanged);
            }

            if (CaptureImageButton != null)
            {
                CaptureImageButton.clicked -= OnCaptureImageButtonClicked;
            }
            
            if (CreateVariantSetButton != null)
            {
                CreateVariantSetButton.clicked -= OnCreateNewVariantSetAsset;
            }

            if (VariantSetNameTextField != null)
            {
                VariantSetNameTextField.UnregisterValueChangedCallback(OnVariantSetTextFieldChange);
                VariantSetNameTextField.UnregisterCallback<KeyDownEvent>(OnVariantSetTextFieldKeyDown);
            }

            if (VariantNameTextField != null)
            {
                VariantNameTextField.UnregisterCallback<KeyDownEvent>(OnVariantTextFieldKeyDown);
            }

            if (CreateVariantButton != null)
            {
                CreateVariantButton.clicked -= OnCreateVariant;
            }

            if (UsePreviousLocationButton != null)
            {
                UsePreviousLocationButton.clicked -= OnUsePreviousLocationButtonClicked;
            }

            if (target != null) return;
            var variantSetBase = target as VariantSetBase;
            AssetRemoveConfirmation.RemoveAssetConfirmation(variantSetBase.VariantSetAsset, 
                variantSetBase.VariantBase.Select(x => x.variantAsset).ToList());
        }

        public void OnVariantTextFieldKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
            {
                OnCreateVariant();
            }
        }

        public void OnVariantSetTextFieldKeyDown(KeyDownEvent evt)
        {
            if(evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
            {
                OnCreateNewVariantSetAsset();
            }
        }

        public void OnUsePreviousLocationButtonClicked()
        {
            SceneView view = SceneView.lastActiveSceneView;
            var variantSetBase = target as VariantSetBase;
            if(!variantSetBase.VariantSetAsset.hasStoreCameraPositionAndRotation) return;
            // Set the SceneView camera position and rotation
            view.LookAt(variantSetBase.VariantSetAsset.storeCameraPosition, variantSetBase.VariantSetAsset.storeCameraRotation, variantSetBase.VariantSetAsset.storeCameraDistance * 0.01f);
        }

        public void ShowVariantSetAssetCreationBtn(bool show)
        {
            CreateVariantSetButton.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        public void ShowVariantContainer(bool show)
        {
            VariantContainer.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            var variantSetBase = target as VariantSetBase;
            if (variantSetBase.VariantBase.Count <= 1)
            {
                VariantSliderContainer.style.display = DisplayStyle.None;
                CaptureImageContainer.style.display = DisplayStyle.None;
                return;
            }
            CaptureImageContainer.style.display = DisplayStyle.Flex;
            VariantSliderContainer.style.display = DisplayStyle.Flex;
        }
        
        public void TrackVariantSetAssetProperty(SerializedProperty obj)
        {
            ShowVariantSetAssetCreationBtn(obj.objectReferenceValue == null);
            ShowVariantContainer(obj.objectReferenceValue != null);
            var variantSetBase = target as VariantSetBase;
            VariantSetNameTextField.SetValueWithoutNotify(variantSetBase.VariantSetAsset == null?
                "Name your variant set here" : variantSetBase.VariantSetAsset.VariantSetName);
        }
        
        public void OnCaptureImageButtonClicked()
        {
            var variantSetBase = target as VariantSetBase;
            var size = EditorCore.IconPresets[CaptureSizeDropdown.index];
            UsePreviousLocationButton.style.display = DisplayStyle.Flex;
            EditorCore.CaptureOptionImage(variantSetBase, size.Width, size.Height);
            if (Selection.activeGameObject != variantSetBase.gameObject) return;
            Selection.activeGameObject = null;
            EditorUtility.SetDirty(variantSetBase.gameObject);
            EditorApplication.delayCall += () => Selection.activeGameObject = variantSetBase.gameObject;
        }

        public void OnVariantCountChanged(SerializedProperty obj)
        {
            var variantSetBase = target as VariantSetBase;
            
            if (m_OriginalVariants.Count != obj.intValue)
            {
                if (m_OriginalVariants.Count > obj.intValue)
                {
                    //A variant has removed find the removed variant
                    List<VariantAsset> removedVariant = new List<VariantAsset>();
                    foreach (var variantBase in m_OriginalVariants)
                    {
                        if(variantSetBase.VariantBase.Any(x => string.Equals(x.variantAsset.UniqueIdString, variantBase.variantAsset.UniqueIdString))) continue;
                        removedVariant.Add(variantBase.variantAsset);
                    }

                    if (removedVariant.Count > 0)
                    {
                        AssetRemoveConfirmation.RemoveAssetConfirmation(removedVariant);
                    }
                }
                else
                {
                    foreach (var variantBase in variantSetBase.VariantBase)
                    {
                        if (variantBase.variantAsset == null)
                        {
                            EditorApplication.delayCall += () =>
                            {
                                // Show a dialog box asking the user if they want to remove the assets
                                var result = EditorUtility.DisplayDialog("Adding Variant Asset",
                                    "There is no Variant Asset found in your variant, do you want to create one?", "Yes", "No");
                                if (result)
                                {
                                    var newVariant = EditorCore.CreateReturnAsset<VariantAsset>("Variant");
                                    variantBase.variantAsset = newVariant as VariantAsset;
                                    variantBase.FoldoutInspector = true;
                                    EditorUtility.SetDirty(variantSetBase);
                                    var selectedObject = Selection.activeGameObject;
                                    Selection.activeGameObject = null;
                                    EditorApplication.delayCall += () => Selection.activeGameObject = selectedObject;
                                }
                            };
                        }
                    }
                }
                m_OriginalVariants = new List<VariantBase>(variantSetBase.VariantBase);
            }
            
            
            SceneTracker.CheckVariantSet(variantSetBase);
            VariantSlider.highValue = obj.intValue - 1;
            VariantSlider.value = Mathf.Min(VariantSlider.value, VariantSlider.highValue);
            
            if (VariantSlider.highValue < 1)
            {
                VariantSliderContainer.style.display = DisplayStyle.None;
                CaptureImageContainer.style.display = DisplayStyle.None;
                return;
            }
            CaptureImageContainer.style.display = DisplayStyle.Flex;
            VariantSliderContainer.style.display = DisplayStyle.Flex;
        }

        public void OnVariantSliderChanged(ChangeEvent<float> evt)
        {
            (target as VariantSetBase).SetVariant((int)evt.newValue, false);
        }
        
        public void OnVariantSetTextFieldChange(ChangeEvent<string> evt)
        {
            var variantSetBase = target as VariantSetBase;
            if(variantSetBase.VariantSetAsset == null) return;
            variantSetBase.VariantSetAsset.SetName(evt.newValue);
            EditorUtility.SetDirty(variantSetBase.VariantSetAsset);
        }

        public virtual void OnCreateNewVariantSetAsset()
        {
            // Create a new VariantSetAsset
            if (string.IsNullOrEmpty(VariantSetNameTextField.value))
            {
                EditorUtility.DisplayDialog("Warning", "You must enter a name for the variant set.", "OK");
                return;
            }
            var variantSetBase = target as VariantSetBase;
            EditorCore.CreateNewVariantSetAsset(variantSetBase, VariantSetNameTextField.value);
            DefaultInspectorContainer.style.display = DisplayStyle.Flex;
        }
        
        public void OnCreateVariant()
        {
            if (string.IsNullOrEmpty(VariantNameTextField.value))
            {
                EditorUtility.DisplayDialog("Warning", "You must enter a name for the variant.", "OK");
                return;
            }
            
            var variantSetBase = target as VariantSetBase;
            EditorCore.CreateVariantAsset(variantSetBase, VariantNameTextField.value);
            VariantNameTextField.SetValueWithoutNotify("Name your variant here");
            
            var variantsObjectField = DefaultInspectorContainer.Q("VariantsList");
            if (variantsObjectField != null)
            {
                var foldOut = variantsObjectField.Q<Foldout>("unity-list-view__foldout-header");
                if (foldOut != null)
                {
                    foldOut.value = true;
                }
            }
        }
    }
}
