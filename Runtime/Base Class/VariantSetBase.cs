using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace IndustryCSE.Tool.ProductConfigurator
{
    [ExecuteInEditMode]
    public abstract class VariantSetBase : MonoBehaviour
    {
        public static Action<VariantSetAsset, VariantAsset, bool> VariantTriggered;
        private static int _triggerChangeCounter = 0;
        
        public VariantSetAsset VariantSetAsset => variantSetAsset;
        
        [SerializeField]
        private VariantSetAsset variantSetAsset;
        
        public bool UseDefaultVariantIndex => useDefaultVariantIndex;
        [SerializeField]
        protected bool useDefaultVariantIndex = false;
        
        [SerializeField]
        protected int defaultVariantIndex = 0;
        public int DefaultVariantIndex => defaultVariantIndex;
        
        public abstract List<VariantBase> VariantBase { get; }

        public bool Hide => hide;
        [SerializeField] protected bool hide = false;
        
        public abstract int CurrentSelectionIndex { get; }
        
        public abstract string CurrentSelectionGuid { get; }
        
        public abstract int CurrentSelectionCost { get; }

        public CinemachineVirtualCamera FocusCamera => focusCamera;
        [SerializeField]
        private CinemachineVirtualCamera focusCamera;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            _triggerChangeCounter = 0;   
        }
        
        protected void Awake()
        {
            _triggerChangeCounter = 0;
        }

        protected virtual void Start()
        {
            VariantTriggered += OnVariantTriggered;
            if (useDefaultVariantIndex)
            {
                SetVariant(defaultVariantIndex, false);
            }
        }

        protected virtual void OnDestroy()
        {
            VariantTriggered -= OnVariantTriggered;
        }

        private void OnVariantTriggered(VariantSetAsset variantSet, VariantAsset variantAsset, bool triggerConditionalVariants)
        {
            if(variantSet != variantSetAsset) return;
            var variant = VariantBase.Find(x => x.variantAsset == variantAsset);
            if(variant == null) return;
            if (triggerConditionalVariants)
            {
                if (_triggerChangeCounter != 0)
                {
                    triggerConditionalVariants = false;
                    _triggerChangeCounter = 0;
                }
                else
                {
                    _triggerChangeCounter++;
                }
            }
            OnVariantChanged(variant, triggerConditionalVariants);
        }

        protected virtual void OnVariantChanged(VariantBase obj, bool triggerConditionalVariants)
        {
            if(!triggerConditionalVariants) return;
            foreach (var conditionalVariant in obj.conditionalVariants)
            {
                VariantTriggered?.Invoke(conditionalVariant.variantSetAsset, conditionalVariant.variantAsset, triggerConditionalVariants);
            }
        }

        public virtual void SetVariant(int value, bool triggerConditionalVariants)
        {
            if(!triggerConditionalVariants) return;
            foreach (var conditionalVariant in VariantBase[value].conditionalVariants)
            {
                VariantTriggered?.Invoke(conditionalVariant.variantSetAsset, conditionalVariant.variantAsset, triggerConditionalVariants);
            }
        }
        
        public virtual void SetDefaultIndex(int index)
        {
            defaultVariantIndex = index;
        }
        
#if UNITY_EDITOR
        private static string VariantSetAssetsFolderPath => Path.Combine("Assets", "Data", "Variant Sets Assets");
        private static string VariantAssetsFolderPath => Path.Combine("Assets", "Data", "Variant Assets");

        private AssetBase CreateReturnAsset<T>(string setName)
        {
            AssetBase newAsset = null;
            if (!Directory.Exists(VariantSetAssetsFolderPath))
            {
                Directory.CreateDirectory(VariantSetAssetsFolderPath);
            }
            
            if (typeof(T) == typeof(VariantSetAsset))
            {
                newAsset = ScriptableObject.CreateInstance<VariantSetAsset>();
            }
            else if (typeof(T) == typeof(VariantAsset))
            {
                newAsset = ScriptableObject.CreateInstance<VariantAsset>();
            }
            
            newAsset?.NewID();
            newAsset?.SetName(setName);
            if (typeof(T) == typeof(VariantSetAsset))
            {
                AssetDatabase.CreateAsset(newAsset, Path.Combine(VariantSetAssetsFolderPath, $"{newAsset.UniqueIdString}.asset"));
            }
            else if (typeof(T) == typeof(VariantAsset))
            {
                var path = Path.Combine(VariantAssetsFolderPath, $"{((VariantAsset) newAsset).UniqueIdString}.asset");
                if (!Directory.Exists(Path.GetDirectoryName(path)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                }
                AssetDatabase.CreateAsset(newAsset, path);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.SetDirty(newAsset);
            return newAsset;
        }
        
        public VariantSetAsset CreateNewVariantSetAsset(string variantSetName)
        {
            var newVariantSet = CreateReturnAsset<VariantSetAsset>(variantSetName) as VariantSetAsset;
            variantSetAsset = newVariantSet;
            EditorUtility.SetDirty(this);
            return newVariantSet;
        }

        public VariantAsset CreateVariantAsset(string variantName)
        {
            var newVariant = CreateReturnAsset<VariantAsset>(variantName) as VariantAsset;
            AddVariant(newVariant);
            EditorUtility.SetDirty(this);
            return newVariant;
        }

        public abstract VariantAsset CreateVariantAsset<T>(string variantName, T variantObject);
        
        public abstract void AddVariant(VariantAsset variantAsset);
        
        public abstract void AddVariant<T>(VariantAsset variantAsset, T variantObject);
#endif
    }
}