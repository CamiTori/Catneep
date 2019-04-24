using System;
using System.Collections.Generic;
using System.Linq;


/// <summary>
/// Enum que representa las subdivisiones posibles y válidas del beat.
/// </summary>
public enum ValidSubdivisions
{
    One = 1,
    Half = 2,
    Quarter = 4,
    Eighth = 8,
    Sixteenth = 16,
}
public static class Subdivision
{

    // Cuantos substeps hay dentro de un beat, tratar de que cumpla con los múltiplos de 2
    /// <summary>
    /// Cuantos substeps hay en un beat.
    /// </summary>
    public const int substepDivision = 16;
    /// <summary>
    /// Cuantos beats dura un substep.
    /// </summary>
    public const float subtepSize = 1f / substepDivision;


    // Un array con todos los divisores, ordenados del más pequeño al más grande
    // por si el enum está desordenado.
    static readonly int[] validDivisors = ((int[])Enum.GetValues(typeof(ValidSubdivisions))).OrderBy(d => d).ToArray();
    public static IEnumerable<int> GetDivisors()
    {
        return validDivisors;
    }

    /// <summary>
    /// Nos devuelve la cantidad de subpasos, dividiendo los subpasos que dura un beat, por la 
    /// la subdivisión válida que le pasemos.
    /// </summary>
    /// <param name="subdivision">La división del beat.</param>
    /// <returns>Cantidad de pasos.</returns>
    public static int GetSubsteps(int validDivisor)
    {
        if (validDivisors.Contains(validDivisor))
        {
            return (substepDivision / validDivisor);
        }
        else return 0;
    }
    /// <summary>
    /// Nos devuelve la cantidad de subpasos, dividiendo los subpasos que dura un beat, por la 
    /// la subdivisión válida que le pasemos, nultiplicada por la cantidad que queramos.
    /// Para poder hacer, por ejemplo 5/2 beats a subpasos.
    /// </summary>
    /// <param name="dividend">El dividendo de la fracción.</param>
    /// <param name="subdivision">La división del beat.</param>
    /// <returns>Cantidad de pasos.</returns>
    public static int GetSubsteps(int dividend, int validDivisor)
    {
        return GetSubsteps(validDivisor) * dividend;
    }

    /// <summary>
    /// Devuelve si un int es un divisor válido para el beat.
    /// </summary>
    /// <param name="divisor">El divisor a comprobar.</param>
    /// <returns>Si el divisor es válido</returns>
    public static bool DivisorIsValid(int divisor)
    {
        foreach (int d in validDivisors)
        {
            if (d == divisor) return true;
        }

        return false;
    }

    /// <summary>
    /// Transforma una cantidad de subpasos a una fracción de beats.
    /// Por ejemplo, si un beat equivale a 16 subpasos, y pasamos 24
    /// devuelve 3 en el dividendo y 2 en el divisor.
    /// </summary>
    /// <param name="substeps">La cantidad de subpasos.</param>
    /// <param name="dividend">Output del dividendo de la fracción.</param>
    /// <param name="divisor">Output del divisor, si es 0, no es válida.</param>
    public static void FromSubsteps(int substeps, out int dividend, out int divisor)
    {
        dividend = divisor = 0;

        foreach (int d in validDivisors)
        {
            int division = substepDivision / d;
            if (substeps % division == 0)
            {
                dividend = substeps / division;
                divisor = d;
                return;
            }
        }
    }

}