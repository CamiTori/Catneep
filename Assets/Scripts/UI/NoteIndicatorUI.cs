using UnityEngine;
using UnityEngine.UI;

using DG.Tweening;

namespace Catneep.UI
{
    public class NoteIndicatorUI : MonoBehaviour
    {

        [SerializeField]
        private RectTransform rectTransform;

        [Header("Indicator")]

        [SerializeField]
        private Image border;
        [SerializeField]
        private Image center;

        [Header("Effects")]

        [SerializeField]
        private float centerLuminance = 0.6f;

        [SerializeField]
        private Image fireEffect;
        [SerializeField]
        private float fireScaleMultiplier = 1.2f;
        [SerializeField]
        private float fireEffectDuration = 0.1f;

        private Color centerColor;
        private string inputName;

        private Sequence fireEffectSequence;

        private void Start()
        {
            centerColor = center.color;
            fireEffect.enabled = false;

            fireEffectSequence = DOTween.Sequence()
                .OnStart(() => fireEffect.enabled = true)
                .Append(fireEffect.rectTransform.DOScale(fireScaleMultiplier, fireEffectDuration * 0.5f).SetEase(Ease.OutElastic))
                .Append(fireEffect.rectTransform.DOScale(1f, fireEffectDuration * 0.5f))
                .OnComplete(() => fireEffect.enabled = false)
                .SetAutoKill(false);
            fireEffectSequence.Pause();
        }

        private void LateUpdate()
        {
            SetIndicatorActive(Input.GetButton(inputName));
        }

        internal void Initialize(Vector2 position, Color color, int input)
        {
            rectTransform.anchoredPosition = position;
            border.color = color;
            inputName = "Note " + input;
            //Debug.Log(inputName);
        }

        public void SetIndicatorActive(bool state)
        {
            center.color = state ? border.color * centerLuminance : centerColor;
        }

        public void OnNoteHit()
        {
            fireEffect.enabled = true;
            fireEffectSequence.Restart();
        }

    }
}