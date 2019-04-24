using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Catneep.Utils;


public class NoteIcon : MonoBehaviour
{

    [SerializeField]
    private RectTransform rectTransform;
    [SerializeField]
    private Image icon;
    [SerializeField]
    private GameObject glow;
    [SerializeField]
    private UILine longNoteBar;

    private NotesUI.NoteIndicator indicator;
    private int index;

    // 0 = No es una nota larga, 1+ = Es una nota larga, 2 = se está tocando ahora mismo la nota larga
    // -1 = dejar de actualizar
    private int longNoteState = 0;
    private float durationScale;

    private const float lineResolution = 20;
    private const float pointScale = 1f / lineResolution;

    private Action<int> Remove;

    // Posiciona la nota y la barra de nota larga en una interpolación t
    // 0 = posición del indicador de notas
    // 1 = posición de spawn
    private float lastT = Mathf.NegativeInfinity;
    private float releaseT = 0;

    internal void Initialize(NotesUI.NoteIndicator indicator, float durationScale, int index, Action<int> RemoveAction)
    {
        this.indicator = indicator;
        this.durationScale = durationScale;
        this.index = index;
        this.Remove = RemoveAction;

        SetColor(indicator.color);

        longNoteState = durationScale > 0 ? 1 : 0;
        if (longNoteState <= 0)
        {
            longNoteBar.gameObject.SetActive(false);
        }
    }
    private void SetColor(Color color)
    {
        icon.color = color;
        longNoteBar.color = color;
    }



    public void OnNextNote()
    {
        glow.SetActive(true);
    }

    public void OnHit()
    {
        // Si es una nota larga, cambiamos el estado para que la longitud se actualice
        if (longNoteState == 1)
        {
            icon.gameObject.SetActive(false);
            longNoteState = 2;
        }
    }

    public void OnRelease()
    {
        if (longNoteState == 2)
        {
            longNoteState = 1;
            releaseT = -lastT;
        }
    }

    public void OnMiss(Color missColor)
    {
        SetColor(missColor);
        glow.SetActive(false);
    }



    public void UpdatePosition(float t, float horizontal = 0, Wave wave = new Wave())
    {
        // Actualizamos cuando el valor de t es distinto al último cuando actualizamos
        if (t == lastT) return;
        lastT = t;
        // Indicamos desde donde y hasta donde llega la barra de notas
        float toT = Mathf.Min(durationScale + t, 1);
        t += releaseT;

        // Buscamos la posición incial de la barra de notas
        // y si estamos en el estado correcto, movemos el
        // icono que representa el inicio
        Vector2 startPos = Vector2.zero;
        switch (longNoteState)
        {
            default:
            case 0:
            case 1:
                startPos = indicator.GetLerp(t, horizontal);
                icon.rectTransform.anchoredPosition = startPos;
                break;
            case 2:
                if (toT < 0)
                {
                    Remove(index);
                    return;
                }
                startPos = indicator.GetLerp(t = 0);
                break;
        }

        // Si no es una nota larga no actualizamos la barra
        if (longNoteState < 1) return;

        float lineWaveDelta = longNoteState > 1 ? -lastT : 0;

        // Añadimos todos los puntos
        List<Vector2> linePoints = new List<Vector2>() { startPos };
        for (t += pointScale; t < toT; t += pointScale)
        {
            linePoints.Add(indicator.GetLerp(t, wave.GetY(t + lineWaveDelta)));
        }
        linePoints.Add(indicator.GetLerp(toT, wave.GetY(toT + lineWaveDelta)));

        // Asignamos los puntos a la linea para que se actualice
        longNoteBar.Points = linePoints.ToArray();
    }
	
}
