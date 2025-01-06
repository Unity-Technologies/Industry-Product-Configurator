using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;
using IndustryCSE.Tool.ProductConfigurator.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using IndustryCSE.Tool.ProductConfigurator.Settings.Editor;

namespace IndustryCSE.Tool.ProductConfigurator.Editor
{
    [CustomPropertyDrawer(typeof(SceneObject))]
    public class SceneObjectCustomPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property == null) return null;
            
            var container = new VisualElement();
            var assetProperty = property.FindPropertyRelative("sceneAsset");
            var sceneAssetPropertyField = new PropertyField(assetProperty);
            
            var assetNameProperty = property.FindPropertyRelative("SceneName");
            
            assetNameProperty.stringValue = assetProperty.objectReferenceValue == null ? "" : assetProperty.objectReferenceValue.name;

            property.serializedObject.ApplyModifiedProperties();
            
            var sceneNameField = new TextField("Scene Name")
            {
                value = assetNameProperty.stringValue,
                name = "SceneNameField"
            };

            sceneNameField.SetEnabled(false);
            
            container.Add(sceneNameField);

            var sceneTips = new Label()
            {
                name = "Tips",
                style =
                {
                    color = new StyleColor(Color.red)
                }
            };
            
            sceneAssetPropertyField.TrackPropertyValue(assetProperty, serializedProperty =>
            {
                assetNameProperty.stringValue = assetProperty.objectReferenceValue == null ? "" : assetProperty.objectReferenceValue.name;
                property.serializedObject.ApplyModifiedProperties();
                ShowTips(assetProperty.objectReferenceValue);
            });
            
            container.Add(sceneAssetPropertyField);
            container.Add(sceneTips);

            ShowTips(assetProperty.objectReferenceValue);
            return container;

            void ShowTips(Object asset)
            {
                if (asset != null)
                {
                    var sceneObjectPath =
                        AssetDatabase.GetAssetPath(asset);
                    var isInBuildSettings = EditorBuildSettings.scenes.Any(scene => scene.path == sceneObjectPath);
                    if (!isInBuildSettings)
                    {
                        sceneTips.text = "Scene is not in build settings";
                        sceneTips.style.display = DisplayStyle.Flex;
                    } else {
                        var scene = EditorBuildSettings.scenes.First(scene => scene.path == sceneObjectPath);
                        if (scene.enabled)
                        {
                            sceneTips.style.display = DisplayStyle.None;
                            return;
                        }
                        sceneTips.text = "Scene is not enabled in build settings";
                        sceneTips.style.display = DisplayStyle.Flex;
                    }
                }
                else
                {
                    sceneTips.style.display = DisplayStyle.None;
                }
            }
        }
    }
    
    [CustomPropertyDrawer(typeof(VariantBase), true)]
    public class VariantBaseCustomPropertyDrawer : PropertyDrawer
    {
        protected Foldout Foldout;
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();
            var variantFoldout = property.FindPropertyRelative("FoldoutInspector");
            Foldout = new Foldout();
            
            var assetFieldProperty = property.FindPropertyRelative("variantAsset");
            if (variantFoldout != null)
            {
                Foldout.RegisterValueChangedCallback(evt =>
                {
                    variantFoldout.boolValue = evt.newValue;
                    variantFoldout.serializedObject.ApplyModifiedProperties();
                    property.serializedObject.ApplyModifiedProperties();
                });
            }
            
            if (assetFieldProperty == null) return null;
            
            var variantAssetObject = assetFieldProperty.objectReferenceValue;
            if (variantAssetObject != null)
            {
                //create a foldout for showing variant name, asset
                Foldout.text = ((VariantAsset) variantAssetObject).VariantName;
                Foldout.value = variantFoldout.boolValue;
                
                var variantNameProperty = new SerializedObject(variantAssetObject).FindProperty("variantName");
                if (variantNameProperty != null)
                {
                    TextField variantNameField = new TextField("Variant Name")
                    {
                        value = variantNameProperty.stringValue,
                        name = "VariantNameField"
                    };
                    variantNameField.RegisterValueChangedCallback(evt =>
                    {
                        variantNameProperty.stringValue = evt.newValue;
                        variantNameProperty.serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(variantAssetObject);
                        property.serializedObject.ApplyModifiedProperties();
                    });
                    Foldout.Add(variantNameField);
                }
                
                var variantIconProperty = new SerializedObject(variantAssetObject).FindProperty("icon");
                if(variantIconProperty != null){
                    var iconObjectField = new ObjectField
                    {
                        label = "Variant Icon",
                        objectType = typeof(Texture2D),
                        value = (Texture2D)variantIconProperty.objectReferenceValue,
                        allowSceneObjects = false
                    };
                    
                    iconObjectField.RegisterValueChangedCallback(evt =>
                    {
                        variantIconProperty.objectReferenceValue = (Texture2D)evt.newValue;
                        variantIconProperty.serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(variantAssetObject);
                    });

                    if (PackageSettingsController.Settings.UseAdvancedSettings)
                    {
                        iconObjectField.style.display = DisplayStyle.None;
                    }
                    
                    Foldout.Add(iconObjectField);
                }
                
                var variantDescriptionProperty = new SerializedObject(variantAssetObject).FindProperty("description");
                if (variantDescriptionProperty != null)
                {
                    TextField variantDescriptionField = new TextField("Variant Description")
                    {
                        value = variantDescriptionProperty.stringValue,
                        name = "VariantDescriptionField"
                    };
                    variantDescriptionField.RegisterValueChangedCallback(evt =>
                    {
                        variantDescriptionProperty.stringValue = evt.newValue;
                        variantDescriptionProperty.serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(variantAssetObject);
                        property.serializedObject.ApplyModifiedProperties();
                    });
                    Foldout.Add(variantDescriptionField);
                }
            } else if (variantAssetObject == null)
            {
                if (!PackageSettingsController.Settings.UseAdvancedSettings)
                {
                    var label = new Label("No Variant Asset found, you can turn on \"Advanced Mode\" in settings to assign Variant Asset.")
                    {
                        style =
                        {
                            whiteSpace = WhiteSpace.Normal,
                            color = Color.red,
                            marginBottom = new Length(10f, LengthUnit.Pixel),
                            marginTop = new Length(10f, LengthUnit.Pixel)
                        }
                    };
                    Foldout.Add(label);
                }
                
                var createAssetButton = new Button()
                {
                    text = "Create Variant Asset",
                    style =
                    {
                        marginBottom = new Length(10f, LengthUnit.Pixel)
                    }
                };
                createAssetButton.clicked += () =>
                {
                    var newVariantAsset = EditorCore.CreateReturnAsset<VariantAsset>("New Variant");
                    variantAssetObject = newVariantAsset;
                    assetFieldProperty.objectReferenceValue = newVariantAsset;
                    assetFieldProperty.serializedObject.ApplyModifiedProperties();
                    variantFoldout.boolValue = true;
                    var selectedObject = Selection.activeGameObject;
                    EditorUtility.SetDirty(selectedObject);
                    Selection.activeGameObject = null;
                    EditorApplication.delayCall += () =>
                    {
                        Selection.activeGameObject = selectedObject;
                    };
                };
                Foldout.Add(createAssetButton);
            }

            VisualElement variantAssetPropertyContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row
                }
            };

            var variantAssetPropertyField = new PropertyField(assetFieldProperty);
            
            variantAssetPropertyContainer.Add(variantAssetPropertyField);

            var selectButton = new Button
            {
                text = "Select"
            };
            selectButton.clicked += () =>
            {
                if(assetFieldProperty.objectReferenceValue == null) return;
                EditorGUIUtility.PingObject(assetFieldProperty.objectReferenceValue);
                Selection.activeObject = assetFieldProperty.objectReferenceValue;
            };
            
            variantAssetPropertyContainer.Add(selectButton);
            
            if (!PackageSettingsController.Settings.UseAdvancedSettings)
            {
                variantAssetPropertyContainer.style.display = DisplayStyle.None;
            }
            
            Foldout.Add(variantAssetPropertyContainer);
            container.Add(Foldout);
            
            return container;
        }

        protected VisualElement ConditionalVariantSetContainer(SerializedProperty property)
        {
            var boxContainer = EditorCore.CreateContainer(EditorCore.TopMargin, EditorCore.BottomMargin);
            var toolTip = new Label("It will trigger the following variant sets when this variant is selected:")
            {
                style =
                {
                    marginBottom = new Length(5f, LengthUnit.Pixel),
                    whiteSpace = WhiteSpace.Normal
                }
            };
            
            boxContainer.Add(toolTip);
            boxContainer.Add(new PropertyField(property.FindPropertyRelative("conditionalVariants")));

            if (!PackageSettingsController.Settings.UseAdvancedSettings)
            {
                boxContainer.style.display = DisplayStyle.None;
            }
            
            return boxContainer;
        }
    }
    
    [CustomPropertyDrawer(typeof(AnimationVariant))]
    public class AnimationVariantCustomPropertyDrawer : VariantBaseCustomPropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = base.CreatePropertyGUI(property);
            
            Foldout.Add(new PropertyField(property.FindPropertyRelative("VariantState")));
            Foldout.Add(ConditionalVariantSetContainer(property));
            
            return container;
        }
    }
    
    [CustomPropertyDrawer(typeof(GameObjectVariant))]
    public class GameObjectVariantCustomPropertyDrawer : VariantBaseCustomPropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = base.CreatePropertyGUI(property);
            
            Foldout.Add(new PropertyField(property.FindPropertyRelative("VariantGameObject")));
            Foldout.Add(ConditionalVariantSetContainer(property));
            
            return container;
        }
    }
    
    [CustomPropertyDrawer(typeof(MaterialVariant))]
    public class MaterialVariantCustomPropertyDrawer : VariantBaseCustomPropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = base.CreatePropertyGUI(property);
            Foldout.Add(new PropertyField(property.FindPropertyRelative("VariantMaterial")));
            Foldout.Add(ConditionalVariantSetContainer(property));
            return container;
        }
    }
    
    [CustomPropertyDrawer(typeof(TransformVariant))]
    public class TransformVariantCustomPropertyDrawer : VariantBaseCustomPropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = base.CreatePropertyGUI(property);
            Foldout.Add(new PropertyField(property.FindPropertyRelative("VariantTransform")));
            
            var instantChangeProperty = property.FindPropertyRelative("InstantChange");
            var toggleField = new Toggle("Instant Change")
            {
                value = instantChangeProperty.boolValue
            };
            
            Foldout.Add(toggleField);
            
            VisualElement animationContainer = new VisualElement();
            
            toggleField.RegisterValueChangedCallback(evt =>
            {
                // Update the value of the InstantChange property
                instantChangeProperty.boolValue = evt.newValue;
                animationContainer.style.display = evt.newValue ? DisplayStyle.None : DisplayStyle.Flex;
                instantChangeProperty.serializedObject.ApplyModifiedProperties();
            });

            var lerpTimeProperty = property.FindPropertyRelative("LerpTime");
            
            var lerpTimeField = new FloatField("Lerp Time")
            {
                value = lerpTimeProperty.floatValue
            };
            
            animationContainer.Add(lerpTimeField);
            
            lerpTimeField.RegisterValueChangedCallback(evt =>
            {
                // Update the value of the LerpTime property
                lerpTimeProperty.floatValue = evt.newValue;
                lerpTimeProperty.serializedObject.ApplyModifiedProperties();
            });
            Foldout.Add(animationContainer);
            Foldout.Add(ConditionalVariantSetContainer(property));
            return container;
        }
    }
    
    [CustomPropertyDrawer(typeof(CombinationVariant))]
    public class CombinationVariantCustomPropertyDrawer : VariantBaseCustomPropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = base.CreatePropertyGUI(property);

            var listProperty = property.FindPropertyRelative("CombinationList").FindPropertyRelative("KeyValuePairs");
            
            CombinationVariantSet combinationVariantSet = (CombinationVariantSet) property.serializedObject.targetObject;
            Dictionary<string, string> combinationList = new Dictionary<string, string>();
            
            foreach (var variantSet in combinationVariantSet.VariantSets)
            {
                var defaultVariant = ReturnDefaultValueOfDropDown(variantSet);
                
                DropdownField dropdownField = new DropdownField(variantSet.VariantSetAsset.VariantSetName)
                {
                    choices = variantSet.VariantBase.Select(x => x.variantAsset.VariantName).ToList(),
                    value = defaultVariant.VariantName
                };
                if (combinationList.ContainsKey(variantSet.VariantSetAsset.UniqueIdString))
                {
                    continue;
                }
                combinationList.Add(variantSet.VariantSetAsset.UniqueIdString, defaultVariant.UniqueIdString);
                dropdownField.RegisterValueChangedCallback(evt =>
                {
                    var variantBase = variantSet.VariantBase.FirstOrDefault(x =>
                        string.Equals(x.variantAsset.VariantName, evt.newValue));
                    
                    combinationList[variantSet.VariantSetAsset.UniqueIdString] = variantBase.variantAsset.UniqueIdString;
                    
                    while(listProperty.arraySize > 0)
                    {
                        listProperty.DeleteArrayElementAtIndex(0);
                    }
                    
                    // Update the listProperty with the new values from combinationList
                    foreach (KeyValuePair<string, string> pair in combinationList)
                    {
                        listProperty.arraySize++;
                        SerializedProperty entry = listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);
                        entry.FindPropertyRelative("Key").stringValue = pair.Key;
                        entry.FindPropertyRelative("Value").stringValue = pair.Value;
                    }
                    
                    property.serializedObject.ApplyModifiedProperties();
                });
                Foldout.Add(dropdownField);
            }
            
            while(listProperty.arraySize > 0)
            {
                listProperty.DeleteArrayElementAtIndex(0);
            }
                    
            // Update the listProperty with the new values from combinationList
            foreach (KeyValuePair<string, string> pair in combinationList)
            {
                listProperty.arraySize++;
                SerializedProperty entry = listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);
                entry.FindPropertyRelative("Key").stringValue = pair.Key;
                entry.FindPropertyRelative("Value").stringValue = pair.Value;
            }
            
            property.serializedObject.ApplyModifiedProperties();
            
            Foldout.Add(ConditionalVariantSetContainer(property));
            return container;

            VariantAsset ReturnDefaultValueOfDropDown(VariantSetBase variantSet)
            {
                for(var i = 0; i < listProperty.arraySize; i++)
                {
                    var entry = listProperty.GetArrayElementAtIndex(i);
                    if (entry.FindPropertyRelative("Key").stringValue == variantSet.VariantSetAsset.UniqueIdString)
                    {
                        foreach (var variantBase in variantSet.VariantBase)
                        {
                            if(string.Equals(variantBase.variantAsset.UniqueIdString, entry.FindPropertyRelative("Value").stringValue))
                            {
                                return variantBase.variantAsset;
                            }
                        }
                    }
                }
                return variantSet.VariantBase.FirstOrDefault().variantAsset;
            }
        }
    }
    
    [CustomPropertyDrawer(typeof(ConditionalVariantData))]
    public class ConditionalVariantDataPropertyDrawer: PropertyDrawer
    {
        private DropdownField _variantSetAssetDropdown, _variantAssetDropdown;
        private List<VariantSetBase> _allVariantSets;
        private SerializedProperty _variantSetAssetProperty;
        private SerializedProperty _variantAssetProperty;
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();
            
            VariantSetBase variantSetBase = (VariantSetBase) property.serializedObject.targetObject;
            _allVariantSets = Object.FindObjectsByType<VariantSetBase>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x != variantSetBase).ToList();

            if (!_allVariantSets.Any())
            {
                container.Add(new Label("No Variant Set found"));
                return container;
            }
            
            var allVariantSetNames = _allVariantSets.Select(x => x.VariantSetAsset.VariantSetName).ToList();
            
            _variantSetAssetDropdown = new DropdownField("Variant Set")
            {
                choices = allVariantSetNames
            };

            _variantSetAssetProperty = property.FindPropertyRelative("variantSetAsset");
            if (_variantSetAssetProperty.objectReferenceValue == null)
            {
                _variantSetAssetDropdown.index = 0;
                _variantSetAssetProperty.objectReferenceValue = _allVariantSets[0].VariantSetAsset;
                _variantSetAssetProperty.serializedObject.ApplyModifiedProperties();
            }
            else
            {
                var variantSet = (VariantSetAsset) _variantSetAssetProperty.objectReferenceValue;
                var index = _allVariantSets.FindIndex(x => x.VariantSetAsset == variantSet);
                _variantSetAssetDropdown.index = index == -1 ? 0 : index;
            }

            _variantSetAssetDropdown.RegisterValueChangedCallback(OnVariantSetDropdownValueChanged);
            
            container.Add(_variantSetAssetDropdown);

            _variantAssetProperty = property.FindPropertyRelative("variantAsset");
            var currentSelectedVariantSet = _allVariantSets[_variantSetAssetDropdown.index];
            var choices = currentSelectedVariantSet.VariantBase.Select(x => x.variantAsset.VariantName).ToList();
            
            if(choices.Count == 0)
            {
                container.Add(new Label("No Variant found"));
                return container;
            }
            
            _variantAssetDropdown = new DropdownField("Variant")
            {
                choices = choices
            };
            
            if(_variantAssetProperty.objectReferenceValue == null)
            {
                _variantAssetDropdown.index = 0;
                _variantAssetProperty.objectReferenceValue = currentSelectedVariantSet.VariantBase[0].variantAsset;
                _variantAssetProperty.serializedObject.ApplyModifiedProperties();
            }
            else
            {
                var variant = (VariantAsset) _variantAssetProperty.objectReferenceValue;
                var index = currentSelectedVariantSet.VariantBase.FindIndex(x => x.variantAsset == variant);
                _variantAssetDropdown.index = index == -1 ? 0 : index;
            }

            _variantAssetDropdown.RegisterValueChangedCallback(OnVariantDropdownValueChanged);
            
            container.Add(_variantAssetDropdown);
            
            return container;
        }

        private void OnVariantDropdownValueChanged(ChangeEvent<string> evt)
        {
            _variantAssetProperty.objectReferenceValue = _allVariantSets[_variantSetAssetDropdown.index].VariantBase[_variantAssetDropdown.index].variantAsset;
            _variantAssetProperty.serializedObject.ApplyModifiedProperties();
        }

        private void OnVariantSetDropdownValueChanged(ChangeEvent<string> evt)
        {
            _variantSetAssetProperty.objectReferenceValue = _allVariantSets[_variantSetAssetDropdown.index].VariantSetAsset;
            _variantSetAssetProperty.serializedObject.ApplyModifiedProperties();
            var currentSelectedVariantSet = _allVariantSets[_variantSetAssetDropdown.index];
            var choices = currentSelectedVariantSet.VariantBase.Select(x => x.variantAsset.VariantName).ToList();
            _variantAssetDropdown.choices = choices;
            _variantAssetDropdown.index = 0;
            _variantAssetProperty.objectReferenceValue = currentSelectedVariantSet.VariantBase[0].variantAsset;
            _variantAssetProperty.serializedObject.ApplyModifiedProperties();
        }
    }
}
