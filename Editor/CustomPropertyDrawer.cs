using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace IndustryCSE.Tool.ProductConfigurator.Editor
{
    [CustomPropertyDrawer(typeof(SceneObject))]
    public class SceneObjectCustomPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();
            container.Add(new PropertyField(property.FindPropertyRelative("sceneAsset")));
            return container;
        }
    }
    
    [CustomPropertyDrawer(typeof(VariantBase), true)]
    public class VariantBaseCustomPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();
            var assetFieldProperty = property.FindPropertyRelative("variantAsset");
            var variantAssetObject = assetFieldProperty.objectReferenceValue;
            if (variantAssetObject != null)
            {
                var variantNameProperty = new SerializedObject(variantAssetObject).FindProperty("variantName");
                if (variantNameProperty != null)
                {
                    var variantNameField = new TextField("Variant Name")
                    {
                        value = variantNameProperty.stringValue
                    };
                    variantNameField.RegisterValueChangedCallback(evt =>
                    {
                        variantNameProperty.stringValue = evt.newValue;
                        variantNameProperty.serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(variantAssetObject);
                    });
                    container.Add(variantNameField);
                }
            }
            
            container.Add(new PropertyField(property.FindPropertyRelative("variantAsset")));
            return container;
        }

        protected VisualElement ConditionalVariantSetContainer(SerializedProperty property)
        {
            var boxContainer = EditorCore.CreateContainer(EditorCore.TopMargin, EditorCore.BottomMargin);
            var toolTip = new Label("It will trigger the following variant sets when this variant is selected:")
            {
                style =
                {
                    marginBottom = new Length(5f, LengthUnit.Pixel)
                }
            };
            
            boxContainer.Add(toolTip);
            boxContainer.Add(new PropertyField(property.FindPropertyRelative("conditionalVariants")));
            
            return boxContainer;
        }
    }
    
    [CustomPropertyDrawer(typeof(GameObjectVariant))]
    public class GameObjectVariantCustomPropertyDrawer : VariantBaseCustomPropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = base.CreatePropertyGUI(property);
            container.Add(new PropertyField(property.FindPropertyRelative("VariantGameObject")));
            container.Add(ConditionalVariantSetContainer(property));
            return container;
        }
    }
    
    [CustomPropertyDrawer(typeof(MaterialVariant))]
    public class MaterialVariantCustomPropertyDrawer : VariantBaseCustomPropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = base.CreatePropertyGUI(property);
            container.Add(new PropertyField(property.FindPropertyRelative("VariantMaterial")));
            container.Add(ConditionalVariantSetContainer(property));
            return container;
        }
    }
    
    [CustomPropertyDrawer(typeof(TransformVariant))]
    public class TransformVariantCustomPropertyDrawer : VariantBaseCustomPropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = base.CreatePropertyGUI(property);
            container.Add(new PropertyField(property.FindPropertyRelative("VariantTransform")));
            container.Add(ConditionalVariantSetContainer(property));
            return container;
        }
    }
    
    [CustomPropertyDrawer(typeof(CombinationVariant))]
    public class CombinationVariantCustomPropertyDrawer : VariantBaseCustomPropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = base.CreatePropertyGUI(property);
            container.Add(ConditionalVariantSetContainer(property));
            return container;
        }
    }
}
