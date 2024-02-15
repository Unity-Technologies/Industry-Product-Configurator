using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;
using UnityEngine;

namespace IndustryCSE.Tool.ProductConfigurator
{
    [Serializable]
    public class GameObjectOptionDetail : OptionDetailBase
    {
        public GameObject optionGameObject;
    }

    public class GameObjectConfiguration : ConfigurationBase
    {
        public List<GameObjectOptionDetail> OptionDetails => optionDetails;

        [SerializeField]
        protected List<GameObjectOptionDetail> optionDetails = new ();

        public string CurrentSelectionGuid => OptionDetails.First(x => x.optionGameObject.activeSelf).configurationOption.UniqueIdString;
    
        public int CurrentSelectionCost => OptionDetails.First(x => x.optionGameObject.activeSelf).configurationOption.additionalCost;

        public override List<OptionDetailBase> Options => OptionDetails.Cast<OptionDetailBase>().ToList();

        protected override void OnOptionChanged(OptionDetailBase optionDetailBase)
        {
            if (optionDetailBase is not GameObjectOptionDetail featureDetails) return;
            if(OptionDetails.Contains(featureDetails))
            {
                OptionDetails.ForEach(x => x.optionGameObject.SetActive(featureDetails.optionGameObject == x.optionGameObject));
            }
        }
    
        public override void SetOption(int value)
        {
            OptionDetails.ForEach(x => x.optionGameObject.SetActive(false));
            optionDetails[value].optionGameObject.SetActive(true);
        }
    }
}

