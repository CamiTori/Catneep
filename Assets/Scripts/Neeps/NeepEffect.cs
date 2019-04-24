using System;
using UnityEngine;

using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using System.Collections.Generic;

namespace Catneep.Neeps
{

    using Neeps.Modules;

    [CreateAssetMenu(fileName = "New Effect", menuName = "Neep Effect")]
    public sealed class NeepEffect : ScriptableObject
    {

        public const string addModuleText = "Add Module...";

#if UNITY_EDITOR

        public List<EffectModule> GetModules { get { return effectModules; } }

        /*
        public void RemoveModule(EffectModule module)
        {
            DestroyImmediate(module, true);
        }

        public event Action OnAddModulePress;

        [ContextMenu(addModuleText, false)]
        private void OnAddModuleContext()
        {
            if (OnAddModulePress != null) OnAddModulePress();
        }
        */

#endif

        [Header("Fading")]

        [SerializeField]
        private float fadeTime = 1f;

        [SerializeField]
        private Ease fadeInEase = Ease.InOutQuad;
        [SerializeField]
        private Ease fadeOutEase = Ease.InOutQuad;

        [HideInInspector]
        [SerializeField]
        private List<EffectModule> effectModules = new List<EffectModule>();

        private GameObject containerObj;
        public GameObject ContainerGameObject { get { return containerObj; } }

        private NeepEffectManager manager;
        public NeepEffectManager Manager { get { return manager; } }


        private void OnEnable()
        {
#if UNITY_EDITOR
            effectModules.ForEach(e => e.SetOwner = this);
#endif
        }

        private void OnDestroyModule(EffectModule module)
        {
            effectModules.Remove(module);
        }

        internal void Initialize(NeepEffectManager manager)
        {
            this.manager = manager;

            containerObj = new GameObject("Effect - " + name)
            {
                layer = manager.gameObject.layer
            };
            containerObj.transform.parent = manager.transform;

            effectModules.ForEach(e => e.Initialize(this));
        }

        // Called after Start() is called on all behaviours
        internal void PostInitialize()
        {
            Hide();
        }


        public void StartEffect(float duration, float startTime, Func<float> getTimeFunc)
        {
            //manager.StartCoroutine(EffectCoroutine(duration, startTime, getTimeFunc));

            float fadeTime = Mathf.Min(this.fadeTime, duration * .5f);
            float fade = 0f;
            DOSetter<float> setFade = (f => { fade = f; Fade(f); });
            DOGetter<float> getFade = (() => fade);

            DOTween.Sequence()
                .OnStart(Show)
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


        private void Show()
        {
            Show(true);
        }
        private void Hide()
        {
            Show(false);
        }
        private void Show(bool show)
        {
            effectModules.ForEach(e => e.Show(show));
        }

        private void Fade(float fadeValue)
        {
            effectModules.ForEach(e => e.Fade(fadeValue));
        }

    }
}