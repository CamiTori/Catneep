using System;

namespace Catneep.Utils
{

    [Serializable]
    public struct Wave
    {

        public const float PI2 = (float)Math.PI * 2f;
        public const float PI2Inverted = 1f / PI2;

        public float intensity;

        public float adjustedFreq;
        public float Frequency
        {
            get
            {
                return adjustedFreq * PI2Inverted;
            }
            set
            {
                adjustedFreq = value * PI2;
            }
        }

        /// <summary>
        /// Creates a wave with a amplitude and a frequency.
        /// </summary>
        /// <param name="intensity">Max intensity for the wave.</param>
        /// <param name="frequency">How many cycles are in one X unit.</param>
        public Wave(float intensity, float frequency)
        {
            this.intensity = intensity;
            this.adjustedFreq = frequency * PI2Inverted;
        }

        /// <summary>
        /// Returns the T position with the sine function
        /// </summary>
        /// <param name="x">The X position in the wave.</param>
        /// <returns>The Y position in the wave.</returns>
        public float GetY(float x)
        {
            return GetYSin(x);
        }
        public float GetYSin(float x)
        {
            return GetY(x, Math.Sin);
        }
        public float GetYCos(float x)
        {
            return GetY(x, Math.Cos);
        }
        private float GetY(float x, Func<double, double> function)
        {
            if (intensity == 0f) return 0f;

            return (float)function(x * adjustedFreq) * intensity;
        }

    }

}