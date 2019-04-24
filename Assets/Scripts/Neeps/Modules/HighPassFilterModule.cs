using UnityEngine;


namespace Catneep.Neeps.Modules
{

    [CustomEffectModule(audioMenu + "High Pass Filter")]
    public class HighPassFilterModule : EffectModulePassFilter<AudioHighPassFilter>
    {

        public override float LerpFrom { get { return minFrequency; } }

        public override float CutoffSetProperty { set { AudioFilter.cutoffFrequency = value; } }

    }
}
