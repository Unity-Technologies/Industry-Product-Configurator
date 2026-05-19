using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;

namespace IndustryCSE.Tool.ProductConfigurator.Runtime
{
    [ExecuteInEditMode]
    public abstract class VariantSetBase : MonoBehaviour
    {
        public static Action<VariantSetAsset, VariantAsset, bool> VariantTriggered;
        private static bool _isApplyingVariant = false;
        
        public Action<VariantBase> VariantChanged;
        
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
        
#if CINEMACHINE_2
        public CinemachineVirtualCamera FocusCamera => focusCamera;
        [SerializeField]
        private CinemachineVirtualCamera focusCamera;
#else
        public CinemachineCamera FocusCamera => focusCamera;
        [SerializeField]
        private CinemachineCamera focusCamera;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            _isApplyingVariant = false;
        }

        protected virtual void OnEnable()
        {
            VariantTriggered += OnVariantTriggered;
        }

        protected virtual void Start()
        {
            if (useDefaultVariantIndex)
            {
                SetVariant(defaultVariantIndex, false);
            }
        }

        protected virtual void OnDisable()
        {
            VariantTriggered -= OnVariantTriggered;
        }

        private void OnVariantTriggered(VariantSetAsset variantSet, VariantAsset variantAsset, bool triggerConditionalVariants)
        {
            if(variantSet != variantSetAsset) return;
            var variant = VariantBase.Find(x => x.variantAsset == variantAsset);
            if(variant == null) return;
            if (triggerConditionalVariants && _isApplyingVariant)
                triggerConditionalVariants = false;

            var wasApplying = _isApplyingVariant;
            if (!wasApplying)
                _isApplyingVariant = true;

            OnVariantChanged(variant, triggerConditionalVariants);

            if (!wasApplying)
                _isApplyingVariant = false;
        }

        protected virtual void OnVariantChanged(VariantBase obj, bool triggerConditionalVariants)
        {
            VariantChanged?.Invoke(obj);
            if(!triggerConditionalVariants) return;
            foreach (var conditionalVariant in obj.conditionalVariants)
            {
                VariantTriggered?.Invoke(conditionalVariant.variantSetAsset, conditionalVariant.variantAsset, triggerConditionalVariants);
            }
        }

        public virtual void SetVariant(int value, bool triggerConditionalVariants)
        {
            VariantChanged?.Invoke(VariantBase[value]);
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
        
        public void SetVariantSetAsset(VariantSetAsset asset)
        {
            variantSetAsset = asset;
        }

        public abstract void AddVariant(VariantAsset variantAsset);
        
        public abstract void AddVariant<T>(VariantAsset variantAsset, T variantObject);
        
        public abstract void AssignVariantObject<T>(string variantGuid, T variantObject);
    }
}