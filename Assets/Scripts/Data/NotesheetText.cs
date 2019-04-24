using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;


public static class NotesheetText
{

    public const string version = "0.4a";

    // Carácteres especiales
    public const char charComment = '#';

    public const char charTime = '@';
    public const char charDuration = '*';

    public const char charFraction = '/';

    public const char charNoteTrue = '1';
    public const char charNoteFalse = '0';



    public static void Export(Song song, string path, Difficulty difficulty)
    {
        if (string.IsNullOrEmpty(path)) return;

        Path.ChangeExtension(path, "txt");
        File.WriteAllLines(path, NotesheetToStrings(song, song.GetNotesheet(difficulty).GetNoteGroups, difficulty)
            .ToArray());
    }
    static IEnumerable<string> NotesheetToStrings(Song song, IEnumerable<NoteGroup> groups, Difficulty difficulty)
    {
        // Encabezado
        yield return null;
        yield return charComment + " Notesheet text importer/exporter v" + version;
        yield return charComment + " Song: " + (song ? song.Title : "missingsong.");
        yield return charComment + " Dificultad: " + difficulty;
        yield return charComment + " Advertencia: Asegurarse de que el editor tenga " +
            "\"Ajuste de línea\" desactivado para visualizar el texto correctamente.";
        yield return null;
        yield return charComment + string.Format(" {0} = Tiempo relativo en beats", charTime);
        yield return charComment + string.Format(" {0} = Duración del grupo en beats (0 por defecto)", charDuration);
        yield return charComment + string.Format(" {0}{1}{0} = Notas en ese grupo ({1} = nota, {0} = sin nota)", charNoteFalse, charNoteTrue);
        yield return null;

        // Informar de las subdivisiones válidas:
        yield return charComment + string.Format(" Los tiempos en beats se pueden fraccionar. Ej: 1{0}2", charFraction);
        string subdivisions = string.Join(" - ", Subdivision.GetDivisors().Select(d => d.ToString()).ToArray());
        yield return charComment + " Divisores válidos: " + subdivisions;
        yield return null;

        int inputLength = (int)difficulty;
        // Escribimos las líneas necesarias para 
        foreach (var group in groups)
        {
            // Dejamos un espacio al principio de cada grupo
            yield return null;


            // Tiempo
            // Devolvemos la línea de texto con el tiempo
            yield return charTime + GetBeatTimeFraction(group.GetRelativeTime);


            // Duración:
            int duration = group.GetDuration;
            if (duration > 0) yield return charDuration + GetBeatTimeFraction(duration);


            // Notas:

            // Devolvemos un línea con el array de notas de este grupo
            // Por ejemplo: 101 = Notas 0 y 2
            string notesText = string.Empty;
            for (int i = 0; i < inputLength; i++)
            {
                notesText += ((group.NoteBits & 1 << i) != 0) ? charNoteTrue : charNoteFalse;
            }
            yield return notesText;
        }
    }
    static string GetBeatTimeFraction(int substeps)
    {
        // Transformar el tiempo de subpasos a una fracción en beats y en texto
        int dividend, divisor;
        Subdivision.FromSubsteps(substeps, out dividend, out divisor);
        string timeText = dividend.ToString();

        // Si el divisor es mayor que 1, le agregamos la fracción ya que para el
        // importador el divisor por defecto es 1.
        if (divisor > 1) timeText += charFraction + divisor.ToString();

        // Devolvemos como número fraccionado y en texto la cantidad de beats
        return timeText;
    }

    /* Versión vieja del exportador
    /// <summary>
    /// Exporta a un archivo de texto las notas de una canción, en un formato legible
    /// para una persona y que puede ser editado para volver a ser importado.
    /// </summary>
    /// <param name="song">La canción para exportar.</param>
    /// <param name="path">A donde exportamos el archivo.</param>
    [Obsolete]
    public static void ExportOld(Song song, string path)
    {
        if (string.IsNullOrEmpty(path)) return;

        Path.ChangeExtension(path, "txt");
        File.WriteAllLines(path, NotesheetToStrings(song).ToArray());
    }
    // Devuelve una colección de líneas de texto, que representan las notas de
    // la canción, de una forma separada por gr
    [Obsolete]
    static IEnumerable<string> NotesheetToStringsOld(Song song)
    {
        // Encabezado
        yield return null;
        yield return charComment + " Notesheet text importer/exporter v" + version;
        yield return charComment + " Song: " + song.title;
        yield return charComment + " Advertencia: Asegurarse de que el editor tenga " +
            "\"Ajuste de línea\" desactivado para visualizar el texto correctamente.";
        yield return null;
        yield return string.Format("{0} {1} = Tiempo relativo en beats", charComment, charTime);
        yield return string.Format("{0} {1}{2}{1} = Notas en ese grupo ({1} = nota, {0} = sin nota)", charComment, charNoteTrue, charNoteFalse);
        yield return null;

        // Si es un array vacío, no hacemos nada de lo de abajo
        Note[] noteArray = song.notes;
        if (noteArray.Length <= 0) yield break;
        
        // El array de bits que representa las notas del grupo
        BitArray inputArray = new BitArray(SongManagerNoteInput.maxNoteInputs);

        // El start time del último grupo que se agregó
        uint lastTime = 0;
        for (int i = 0; i < noteArray.Length; i++)
        {
            // La nota actual y la que le sigue
            Note currentNote = noteArray[i];
            Note nextNote = (i + 1 < noteArray.Length) ? noteArray[i + 1] : null;

            // Añadir el input que pertenece esta nota
            inputArray.Set(currentNote.Input, true);

            // Si no hay una siguiente nota o esa nota está separada de esta,
            // creamos un grupo con las notas que tengamos ahora
            if (nextNote == null || nextNote.GetRelativeTime > 0)
            {
                // Transforma el tiempo de subpasos a una fracción de beats
                // y asignamos el tiempo actual al de este grupo
                uint dividend, divisor;
                Subdivision.FromSubsteps(currentNote.StartTime - lastTime, out dividend, out divisor);
                lastTime = currentNote.StartTime;

                // Agregar a la linea de texto el tiempo en beats y fraccionado
                string textLine = charTime.ToString() + dividend;
                if (divisor > 1)
                {
                    // Si el divisor es mayor que 1, agregamos el divisor al tiempo
                    textLine += charFraction.ToString() + divisor;
                }
                // Dar dos espacios de separación
                yield return null;
                yield return null;
                // Y devolver la linea que diga el tiempo
                yield return textLine;

                // Devolvemos otra linea con las notas de este grupo y reseteamos el BitArray
                textLine = string.Empty;
                foreach (bool noteState in inputArray)
                {
                    // Por cada bit del BitArray agregamos un caracter verdadero o falso
                    textLine += noteState ? charNoteTrue : charNoteFalse;
                }
                yield return textLine;
                inputArray.SetAll(false);
            }
        }
    }
    */


    /// <summary>
    /// A partir del path que se le de, trata de leer las líneas de texto de un archivo
    /// y devuelve un grupo de notas a partir de eso. Devuelve un bool indicando si pudo 
    /// o no importar la canción
    /// </summary>
    /// <param name="path">La dirección del archivo a leer.</param>
    /// <param name="notes">Si pudo importar la partitura, el array a modifcar.</param>
    /// <returns>La importación fue completada con éxito.</returns>
    public static bool Import(string path, ref NoteGroup[] notes)
    {
        if (string.IsNullOrEmpty(path)) return false;

        try
        {
            var importer = new TextImporter(File.ReadAllLines(path));
            notes = importer.GetNoteGroups;

            importer.Dispose();

            //Debug.LogFormat("Importación de notesheet completada con éxito. {0}", notes.Length);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
            return false;
        }
    }
    class TextImporter : IDisposable
    {

        // La línea actual que estamos leyendo
        readonly int currentLine = 0;

        // El tiempo relativo que se va a asignar al próximo grupo
        int relativeTime = 0;
        // El tiempo de duración que se va asginar al próximo grupo
        int duration = 0;
        // Los grupos de notas que tenemos hasta ahora
        List<NoteGroup> groups = new List<NoteGroup>();
        public NoteGroup[] GetNoteGroups { get { return groups.ToArray(); } }


        public TextImporter(string[] lines)
        {
            // Leer cada línea del texto
            for (currentLine = 0; currentLine < lines.Length; currentLine++)
            {
                ReadLine(lines[currentLine]);
            }
        }

        // Leer una línea de texto y decidir que hacer dependiendo del primer caracter
        void ReadLine(string line)
        {
            for (int i = 0; i < line.Length; i++)
            {
                switch (line[i])
                {
                    case charComment:
                        return;
                    case ' ':
                        continue;
                    case charTime:
                        ReadRelativeTime(line.Substring(++i));
                        return;
                    case charDuration:
                        ReadDuration(line.Substring(++i));
                        return;
                    case charNoteTrue:
                    case charNoteFalse:
                        ReadNotes(line);
                        return;
                    default:
                        throw new InvalidCharacterException(line[i], currentLine);
                }

            }
        }

        // Leer el tiempo relativo de este grupo y si además está en la misma línea,
        // leemos el tiempo de duración
        void ReadRelativeTime(string line)
        {
            int i = 0;
            relativeTime += ReadTime(line, ref i);
            duration = 0;

            // Detectar que hay después del tiempo
            for (; i < line.Length; i++)
            {
                char c = line[i];
                switch (c)
                {
                    case charComment:
                        return;
                    case ' ':
                        continue;
                    case charDuration:
                        ReadDuration(line.Substring(++i));
                        return;
                    default:
                        throw new InvalidCharacterException(c, currentLine);
                }
            }
        }
        void ReadDuration(string line)
        {
            int i = 0;
            duration = ReadTime(line, ref i);
        }

        // Lee una fraccion de tiempo en beats y la transforma en subpasos
        // Ej: 1/2 -> 8
        int ReadTime(string line, ref int charNumber)
        {
            // 0 = leyendo el dividendo, 1 = buscando el carácter de fracción, 2 = leyendo el divisor, 3 = leido los dos
            int readingState = 0;
            // Declaramos el dividendo y divisor con sus valores por defecto
            int dividend = 0, divisor = 1;

            for (; charNumber <= line.Length; charNumber++)
            {
                switch (readingState)
                {
                    case 0:
                        dividend = ReadInt(line, ref charNumber);
                        readingState = 1;
                        break;
                    case 2:
                        divisor = ReadInt(line, ref charNumber);
                        readingState = 3;
                        break;
                    default:
                        goto ReturnValue;
                }

                // Si no estamos buscando una fracción, devolvemos el valor actual
                if (charNumber >= line.Length || readingState != 1) goto ReturnValue;

                switch (line[charNumber])
                {
                    case charComment:
                    default:
                        goto ReturnValue;
                    case ' ':
                        continue;
                    case charFraction:
                        readingState = 2;
                        break;
                }
            }

            ReturnValue:
            return Subdivision.GetSubsteps(dividend, divisor);
        }
        int ReadInt(string line, ref int charNumber)
        {
            bool foundNumber = false;
            string numberString = string.Empty;

            for (; charNumber < line.Length; charNumber++)
            {
                char c = line[charNumber];
                switch (c)
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        numberString += c;
                        foundNumber = true;
                        break;
                    case ' ':
                        if (!foundNumber) continue;
                        else goto default;
                    default:
                        goto ReturnInt;
                }
            }

            ReturnInt:

            int parsedNumber;
            //Debug.Log("Number: " + numberString);
            if (int.TryParse(numberString, out parsedNumber))
            {
                return parsedNumber;
            }
            else throw new Exception(string.Format("No number found in line " + currentLine));
        }

        // Lee los carácteres de una línea para formar un grupo de notas
        void ReadNotes(string line)
        {
            // Si no estamos en el primer grupo y el tiempo relativo es menor que 1
            // no creamos ningún grupo
            if (relativeTime < 1 && groups.Count > 0) return;

            byte noteNumber = 0;
            int inputArray = 0;

            foreach (char c in line)
            {
                switch (c)
                {
                    case charComment:
                        return;
                    case ' ':
                        continue;
                    default:
                        throw new InvalidCharacterException(c, currentLine);
                    case charNoteTrue:
                        inputArray |= 1 << noteNumber;
                        goto case charNoteFalse;
                    case charNoteFalse:
                        noteNumber++;
                        break;
                }
            }

            if (inputArray != 0)
            {
                groups.Add(new NoteGroup(unchecked((byte)inputArray), relativeTime, duration));
                duration = relativeTime = 0;
            }
        }

        // Para indicar al GC que deje de usar este objeto cuando no lo necesitemos,
        // usando IDisposable
        public void Dispose()
        {
            groups.Clear();
        }

    }


    /* Importador viejo
    /// <summary>
    /// Importa un archivo .txt a un array de notas para asignar a dicha canción.
    /// </summary>
    /// <param name="toSong"></param>
    /// <param name="path"></param>
    public static void Import(Song toSong, string path)
    {
        if (string.IsNullOrEmpty(path)) return;

        // Tratamos de conseguir una lista de notas
        // y de asignar esa lista a la canción actual.
        // Si en el medio hay una excepción, se cancela y va al catch
        try
        {
            LinesToNoteArray(File.ReadAllLines(path));
            toSong.notes = noteList.ToArray();
            toSong.OnValidate();
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
        }

        // Borramos la lista entera
        noteList.Clear();
    }


    // El tiempo relativo y la lista de notas que se usa cuando se importa un texto.
    static uint relativeTime;
    static List<Note> noteList = new List<Note>();

    static int currentLine = 0;

    /// <summary>
    /// Transforma varias líneas de texto en una lista de notas, que se agregan a
    /// <see cref="noteList"/>.
    /// </summary>
    /// <param name="lines">Líneas de texto a parsear.</param>
    static void LinesToNoteArray(string[] lines)
    {
        // Asignar el tiempo relativo inicial a 0
        relativeTime = 0;

        // Leer cada línea del texto
        for (currentLine = 0; currentLine < lines.Length; currentLine++)
        {
            ReadLine(lines[currentLine]);
        }
    }
    // Lee una linea de texto y busca un tiempo o una nota, dependiendo del primer carácter. 
    static void ReadLine(string line)
    {
        for (int i = 0; currentLine < line.Length; currentLine++)
        {
            switch (line[i])
            {
                case charComment:
                    return;
                case ' ':
                    continue;
                case charTime:
                    relativeTime += ReadTime(line.Substring(i + 1));
                    return;
                case charNoteTrue:
                case charNoteFalse:
                    ReadNotes(line);
                    return;
                default:
                    throw new InvalidCharacterException(line[i], currentLine);
            }

        }
    }
    // Trata de buscar el tiempo en una línea de texto, se llama cuando se encuentra el correspondiente
    // caracter que representa tiempo.
    static uint ReadTime (string line)
    {
        string numberString = string.Empty;
        // 0 = leyendo el dividendo, 1 = leyendo el divisor, 2 = leido los dos
        int readingState = 0;
        // Declaramos el dividendo y divisor con sus valores por defecto
        uint dividend = 0, divisor = 1;

        for (int i = 0; i <= line.Length; i++)
        {
            if (i >= line.Length || readingState >= 2)
            {
                // En caso de que no lo hayamos hecho parseamos el último número
                uint parsedNumber;
                if (uint.TryParse(numberString, out parsedNumber))
                {
                    if (readingState <= 0) dividend = parsedNumber;
                    else divisor = parsedNumber;
                }

                return Subdivision.GetSubsteps(dividend, divisor);
            }

            switch (line[i])
            {
                case charFraction:
                case ' ':
                case charComment:
                    uint parsedNumber;
                    if (uint.TryParse(numberString, out parsedNumber))
                    {
                        if (readingState <= 0) dividend = parsedNumber;
                        else divisor = parsedNumber;

                        readingState++;
                    }
                    numberString = string.Empty;
                    break;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    numberString += line[i];
                    break;
                default:
                    throw new InvalidCharacterException(line[i], currentLine);
            }
        }

        return 0;
    }
    /// <summary>
    /// Lee todos los caracteres de una línea, para agregar a la lista las notas,
    /// dependiendo de los carácteres que hayan salido, y reinician el tiempo relativo
    /// cuando añadimos una nota, esto nos permite agregar tiempos sin notas, para poder
    /// agregar pausas.
    /// </summary>
    /// <param name="line">La línea con los carácteres.</param>
    static void ReadNotes(string line)
    {
        bool hasNotes = false;
        byte noteNumber = 0;
        foreach (char c in line)
        {
            switch (c)
            {
                case ' ':
                    continue;
                case charComment:
                    continue;
                default:
                    throw new InvalidCharacterException(c, currentLine);
                case charNoteTrue:
                    noteList.Add(new Note(noteNumber, !hasNotes ? relativeTime : 0));
                    hasNotes = true;
                    goto case charNoteFalse;
                case charNoteFalse:
                    noteNumber++;
                    break;
            }
        }

        if (hasNotes) relativeTime = 0;
    }
    */

    class InvalidCharacterException : Exception
    {
        readonly char c;
        readonly int lineNumber;

        public InvalidCharacterException (char character, int lineNumber)
        {
            this.c = character;
            this.lineNumber = lineNumber;
        }

        public override string Message
        {
            get
            {
                return string.Format("Unnexpected character in line {0} ({1})", lineNumber , c);
            }
        }
    }

}
