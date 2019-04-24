using System;
using UnityEngine;


public class PitchShifterFilter : MonoBehaviour
{

    public int shift = 0;


    void OnAudioFilterRead(float[] data, int channels)
    {
        if (shift < 0) shift = 0;

        int length = data.Length;
        float[] shiftedData = new float[length];

        int from = 0;
        int to = shift;
        for (; to < length; from++, to++)
        {
            shiftedData[to] = data[from];
        }
        //Debug.Log(from + " - " + to);

        Array.Copy(shiftedData, data, length);
    }

}
