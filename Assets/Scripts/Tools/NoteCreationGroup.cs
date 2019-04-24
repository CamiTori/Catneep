using UnityEngine;
using UnityEngine.UI;

namespace Catneep.NoteCreation
{

    public enum NoteType
    {
        NONE, Simple, LongStart, LongMiddle, LongEnd
    }

    public class NoteCreationGroup : MonoBehaviour
    {

        [SerializeField]
        Image[] noteImages;

        [SerializeField]
        Text timeText;
        float time;
        public float GetGroupTime { get { return time; } }

        [SerializeField]
        NoteType[] notes = new NoteType[] {
                                    NoteType.NONE,
                                    NoteType.NONE,
                                    NoteType.NONE,
                                    NoteType.NONE,
                                    NoteType.NONE,
                                    NoteType.NONE };
        public NoteType GetNoteType(int noteIndex)
        {
            return notes[noteIndex];
        }


        public void UpdateNoteTime(float time)
        {
            this.time = time;
            UpdateNoteTime(time.ToString("0.000") + " s");
        }
        public void UpdateNoteTime(string stringTime)
        {
            timeText.text = stringTime;
        }

        public void UpdateNote(int index, NoteType noteType, Sprite sprite)
        {
            notes[index] = noteType;
            noteImages[index].sprite = sprite;
            noteImages[index].enabled = true;
        }
    }
}