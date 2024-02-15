using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IndustryCSE.Tool.ProductConfigurator
{
    public class SetConfiguration : ConfigurationBase
    {
        public ConfigurationBase[] Configurations => configurations;
    
        [SerializeField]
        protected ConfigurationBase[] configurations;
    
        public List<OptionDetailBase> OptionDetails => optionDetails;
    
        [SerializeField]
        protected List<OptionDetailBase> optionDetails;
        
        public override List<OptionDetailBase> Options => OptionDetails;

        protected override void OnOptionChanged(OptionDetailBase optionDetailBase)
        {
            if(!OptionDetails.Contains(optionDetailBase)) return;
            //find index
            var index = OptionDetails.FindIndex(x => x == optionDetailBase);
            foreach (var option in configurations)
            {
                option.SetOption(index);
            }
        }

        public override void SetOption(int value)
        {
            foreach (var option in configurations)
            {
                option.SetOption(value);
            }
        }
    }
}