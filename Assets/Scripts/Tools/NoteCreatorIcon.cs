using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Catneep.NoteCreation
{

    public class NoteCreatorIcon : MonoBehaviour, IPointerClickHandler, IDragHandler
    {

        [SerializeField]
        RectTransform rectTransform;

        public event Action OnRightClick;
        public event Action<float> OnDragVertical;

        public void SetVerticalPosition(float position)
        {
            rectTransform.anchoredPosition = new Vector2(0, position);
        }

        public void SetVerticalScale(float scale)
        {
            Vector2 rectScale = rectTransform.sizeDelta;
            rectScale.y = scale;
            rectTransform.sizeDelta = rectScale;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            //Debug.Log(eventData.button);
            if (eventData.button == PointerEventData.InputButton.Right && OnRightClick != null)
            {
                OnRightClick();
            }
        }


        public void OnDrag(PointerEventData eventData)
        {
            float dragPos = rectTransform.anchoredPosition.y + eventData.delta.y;
            //Debug.Log(dragPos);

            if (OnDragVertical != null) OnDragVertical(dragPos);
        }

    }

}
