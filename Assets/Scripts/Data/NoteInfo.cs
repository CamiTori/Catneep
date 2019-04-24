using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Catneep.Data
{
    // Clase que nos permite guardar información de cada una de las notas en segundos
    // en el tiempo que comienzan y cuanto duran y que número de notas es, para que el editor de notas
    // sea capaz de almacenarlo en un archivo serializable y poder exportarlo a una canción o volver editarlo
    // para agregar más notas.
    [Serializable]
    public class NoteInfo : IDisposable
    {

        public byte noteInput; // Número de input

        float startTime; // Cuando comienza la nota en segundos
        public float Start
        {
            get { return startTime; }
            set
            {
                startTime = Math.Max(value, 0f);
            }
        }

        public float duration; // Cuanto dura la nota en segundos

        [field: NonSerialized]
        public event Action OnNoteReleased;

        bool updateDuration = false;
        public bool ShouldUpdate { get { return updateDuration; } }

        public NoteInfo(byte input, float startTime) : this(input, startTime, 0)
        {
            updateDuration = true;
        }
        public NoteInfo(byte input, float startTime, float duration)
        {
            this.noteInput = input;
            this.startTime = startTime;
            this.duration = duration;
        }

        public void UpdateTime(float endTime, float minLength)
        {
            if (!updateDuration) return;
            updateDuration = false;

            // Dependiendo del tiempo de fin, asignamos la duración siempre y cuando sea
            // mayor que el tiempo minLength, sino la duración se asigna a 0
            duration = endTime - startTime;
            if (duration < minLength) duration = 0;

            if (OnNoteReleased != null) OnNoteReleased();
        }


        public void Dispose()
        {
            
        }


        #region Loading and saving

        public const string notesheetFileFormat = "ns";


        public static void SaveNotesheet(NoteInfo[] notesheet, string path)
        {
            DataManager.SaveData(notesheet, path);
        }
        public static void SaveNotesheetFromNoteGroups(IEnumerable<NoteGroup> notesheet, byte noteQuantity, float bpm, string path)
        {
            SaveNotesheet(FromNoteGroups(notesheet, noteQuantity, bpm), path);
        }
        public static NoteInfo[] FromNoteGroups(IEnumerable<NoteGroup> notesheet, byte noteQuantity, float bpm)
        {
            float substepToTime = (Subdivision.subtepSize * 60f) / bpm;
            //Debug.Log(bpm + ": " + substepToTime);
            List<NoteInfo> notes = new List<NoteInfo>();

            float time = 0;
            foreach (var group in notesheet)
            {
                time += group.GetRelativeTime * substepToTime;

                float duration = group.GetDuration * substepToTime;
                for (byte i = 0; i < noteQuantity; i++)
                {
                    if ((group.NoteBits & 1 << i) != 0)
                    {
                        //Debug.Log(string.Format("{0}: @{1} *{2}", i, time, duration));
                        notes.Add(new NoteInfo(i, time, duration));
                    }
                }
            }

            return notes.ToArray();
        }

        /// <summary>
        /// Devuelve un array de notas cargando de un archivo serializado.
        /// </summary>
        /// <param name="path">Ruta del archivo a cargar</param>
        /// <returns>Array de notas.</returns>
        public static NoteInfo[] LoadNotesheet(string path)
        {
            return DataManager.LoadData<NoteInfo[]>(path);
        }
        public static NoteGroup[] LoadNoteSheetAsNoteGroups(string path, float bpm)
        {
            return ToNoteGroups(LoadNotesheet(path), bpm);
        }
        public static NoteGroup[] ToNoteGroups(NoteInfo[] noteInfos, float bpm)
        {
            if (noteInfos.Length < 1) return new NoteGroup[0];

            float timeToSubtep = (bpm / 60f) * Subdivision.substepDivision;
            noteInfos = noteInfos.OrderBy(i => i.startTime).ToArray();

            List<NoteGroup> notes = new List<NoteGroup>();

            NoteInfo cur;
            NoteInfo next = noteInfos[0];
            int lastGroupStep = 0;
            int nextNoteStep = Mathf.RoundToInt(next.startTime * timeToSubtep);

            int currentNoteArray = 0;

            for (int i = 0; i < noteInfos.Length; i++)
            {
                cur = next;
                int curStep = nextNoteStep;

                bool lastNote = i + 1 >= noteInfos.Length;
                if (!lastNote)
                {
                    next = noteInfos[i + 1];
                    nextNoteStep = Mathf.RoundToInt(next.startTime * timeToSubtep);
                    Debug.Log(nextNoteStep);
                }

                currentNoteArray |= 1 << cur.noteInput;

                if (lastNote || nextNoteStep > curStep)
                {
                    notes.Add(new NoteGroup(unchecked((byte)currentNoteArray), curStep - lastGroupStep,
                        Mathf.RoundToInt(cur.duration * timeToSubtep)));
                    currentNoteArray = 0;
                    lastGroupStep = curStep;
                }
            }

            return notes.ToArray();
        }

        #endregion

    }
}
