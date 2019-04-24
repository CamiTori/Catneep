using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;


namespace Catneep.Neeps.Effects
{

    [CreateAssetMenu(fileName = "New Effect", menuName = "Neep Effect")]
    public class NeepEffect : ScriptableObject
    {

        [SerializeField]
        private float fadeTime = 1f;

        [SerializeField]
        private Ease fadeInEase = Ease.InOutQuad;
        [SerializeField]
        private Ease fadeOutEase = Ease.InOutQuad;

        [SerializeField]
        private PostProcessProfile postProcess;
        public PostProcessProfile GetPostProcessProfile { get { return postProcess; } }

        //private List<EffectModuleBase> effectModules = new List<EffectModuleBase>();

        private GameObject effectObj;
        public GameObject EffectGameObject { get { return effectObj; } }

        private PostProcessVolume postProcessVolume;

        private NeepEffectManager manager;
        public NeepEffectManager Manager { get { return manager; } }

        public void Initialize(NeepEffectManager manager)
        {
            this.manager = manager;

            effectObj = new GameObject("Effect - " + name);
            effectObj.layer = manager.gameObject.layer;

            if (postProcess != null)
            {
                postProcessVolume = effectObj.AddComponent<PostProcessVolume>();
                postProcessVolume.transform.parent = manager.transform;

                postProcessVolume.isGlobal = true;
                postProcessVolume.sharedProfile = postProcess;
                postProcessVolume.weight = 0f;
            }

            OnInit();
        }
        protected virtual void OnInit()
        {

        }

        public void StartEffect(float duration, float startTime, Func<float> getTimeFunc)
        {
            //manager.StartCoroutine(EffectCoroutine(duration, startTime, getTimeFunc));

            float fadeTime = Mathf.Min(this.fadeTime, duration * .5f);
            float fade = 0f;
            DOSetter<float> setFade = (f => { fade = f; OnFade(f); });
            DOGetter<float> getFade = (() => fade);

            DOTween.Sequence()
                .AppendCallback(Show)
                // Tiempo de espera de la duración
                .AppendInterval(duration)
                // Fade In
                .Insert(0f, FadeIn(getFade, setFade, fadeTime))
                // Fade Out
                .Insert(duration - fadeTime, FadeOut(getFade, setFade, fadeTime))
                // Para que DOTween pueda reciclar este objeto y no lo tenga que borrar
                .SetRecyclable()
                // Para que se actualize de acuerdo al tiempo de la canción
                .SetUpdate(UpdateType.Manual)
                .OnComplete(Hide);
        }

        TweenerCore<float, float, FloatOptions> FadeIn(DOGetter<float> getter, DOSetter<float> setter, 
            float duration)
        {
            return Fade(getter, setter, duration, 1f).SetEase(fadeInEase);
        }
        TweenerCore<float, float, FloatOptions> FadeOut(DOGetter<float> getter, DOSetter<float> setter, 
            float duration)
        {
            return Fade(getter, setter, duration, 0f).SetEase(fadeOutEase);
        }
        TweenerCore<float, float, FloatOptions> Fade(DOGetter<float> getter, DOSetter<float> setter,
            float duration, float to)
        {
            return DOTween.To(getter, setter, to, duration);
        }
        

        protected void Show()
        {
            Show(true);
        }
        protected void Hide()
        {
            Show(false);
        }
        protected virtual void Show(bool show)
        {
            
        }

        protected virtual void OnFade(float fadeValue)
        {
            if (postProcessVolume != null) postProcessVolume.weight = fadeValue;
        }

    }
}
