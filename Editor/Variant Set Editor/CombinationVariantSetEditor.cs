using UnityEditor;
using IndustryCSE.Tool.ProductConfigurator.Runtime;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using System.Linq;

namespace IndustryCSE.Tool.ProductConfigurator.Editor
{
    [CustomEditor(typeof(CombinationVariantSet))]
    public class CombinationVariantSetEditor : CustomConfigurationEditorBase
    {
        private int originalVariantSetsCount;
        
        private SerializedProperty variantSetsProperty;
        
        private VisualElement defaultInspector;
        
        private List<string> originalList = new();

        protected override void OnEnable()
        {
            base.OnEnable();
            variantSetsProperty ??= serializedObject.FindProperty("VariantSets.Array");
            originalVariantSetsCount = variantSetsProperty.arraySize;
        }
        
        public override VisualElement CreateInspectorGUI()
        {
            CombinationVariantSet combinationVariantSet = (CombinationVariantSet)target;
            combinationVariantSet.VariantSets.ForEach(x => originalList.Add(x.VariantSetAsset.UniqueIdString));
            defaultInspector = base.CreateInspectorGUI();
            
            var so = new SerializedObject(target);
            
            variantSetsProperty ??= so.FindProperty("VariantSets.Array");
            
            var objectField = defaultInspector.Q<PropertyField>("PropertyField:VariantSets");
            
            objectField.TrackPropertyValue(variantSetsProperty, OnVariantSetsListChanged);
            
            return defaultInspector;
        }

        private void OnVariantSetsListChanged(SerializedProperty listProperty)
        {
            CombinationVariantSet combinationVariantSet = (CombinationVariantSet)target;
            if (listProperty.arraySize == originalVariantSetsCount)
            {
                var currentList = new List<string>();
                combinationVariantSet.VariantSets.ToList().ForEach(x => currentList.Add(x.VariantSetAsset.UniqueIdString));
                currentList.Sort();
                originalList.Sort();
                if (!currentList.SequenceEqual(originalList))
                {
                    RefreshSelection();
                    return;
                }
                return;
            }
            originalList.Clear();
            combinationVariantSet.Variants.ForEach(x => originalList.Add(x.variantAsset.UniqueIdString));
            if (originalVariantSetsCount > listProperty.arraySize)
            {
                //Remove unused elements
                
                var currentList = new List<string>();
                combinationVariantSet.VariantSets.ToList().ForEach(x => currentList.Add(x.VariantSetAsset.UniqueIdString));
                
                foreach (var variant in combinationVariantSet.Variants)
                {
                    variant.CombinationList.KeyValuePairs.RemoveAll(x => !currentList.Contains(x.Key));
                }
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            originalVariantSetsCount = listProperty.arraySize;

            RefreshSelection();
        }

        private void RefreshSelection()
        {
            var selectedObject = Selection.activeGameObject;
            if(selectedObject == null) return;
            if (selectedObject != ((CombinationVariantSet) target).gameObject) return;
            Selection.activeGameObject = null;
            EditorApplication.delayCall += () => Selection.activeGameObject = selectedObject;
        }
    }
}
