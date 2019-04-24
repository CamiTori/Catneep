using UnityEngine;


namespace Catneep.Neeps.Modules
{

    public abstract class EffectModuleAudio<AudioFilterT> : EffectModule where AudioFilterT : Behaviour
    {

        public const float minFrequency = 10f;
        public const float maxFrequency = 22000f;

        private AudioFilterT filter;
        public AudioFilterT AudioFilter { get { return filter; } }

        protected override void OnInitialize(NeepEffect owner)
        {
            filter = Owner.Manager.AudioListener.gameObject.AddComponent<AudioFilterT>();
        }

        protected internal override void Show(bool show)
        {
            filter.enabled = show;
        }

    }

    public abstract class EffectModulePassFilter<AudioFilterT> : EffectModuleAudio<AudioFilterT> where AudioFilterT : Behaviour
    {

        [SerializeField]
        private float cutoffFrequency = 5000f;

        public abstract float LerpFrom { get; }

        public abstract float CutoffSetProperty { set; }

        protected internal override void Fade(float fade)
        {
            CutoffSetProperty = Mathf.LerpUnclamped(LerpFrom, cutoffFrequency, fade);
        }

    }

}
