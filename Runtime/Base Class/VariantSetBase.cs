using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;

namespace IndustryCSE.Tool.ProductConfigurator
{
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
        public void SetVariantSetAsset(VariantSetAsset asset)
        {
            variantSetAsset = asset;
        }

        public abstract void AddVariant(VariantAsset variantAsset);
#endif
    }
}