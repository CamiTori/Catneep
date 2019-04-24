using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

using Catneep.Data;


namespace Catneep.NoteCreation
{

    public enum NoteCreatorEvent { None, Down, Hold, Up }

    public class NoteCreator : SongManager
    {


        [Header("Creator")]

        // Cuanto es el tiempo minimo para una nota larga
        public float longNoteMinTime = 0.3f;

        // Todas las teclas que detectamos, con su posición en el array como número de input
        public KeyCode[] keys = new KeyCode[]
        {
            KeyCode.T,
            KeyCode.Y,
            KeyCode.U,
            KeyCode.I,
            KeyCode.O,
            KeyCode.P,
        };


        [Header("UI")]

        public TextMeshProUGUI selectedSong;
        string defaultSongText;

        static readonly Difficulty[] difficulties = (Difficulty[])Enum.GetValues(typeof(Difficulty));
        Difficulty currentDifficulty;

        public TMP_Dropdown difficultyDropdown;

        public Button playButton;
        TextMeshProUGUI playButtonText;
        //string playButtonOriginalLabel;

        [Space]
        [Space]

        public Transform[] noteIndicators;

        [Space]

        public NoteCreatorIcon noteIconPrefab;
        public float iconMargin = 24f;
        public float timeScale = 100f;


        public class NoteIcon : IDisposable
        {

            public NoteIcon(NoteCreator owner, NoteInfo note)
            {
                this.owner = owner;
                this.note = note;

                this.icon = Instantiate(owner.noteIconPrefab, owner.noteIndicators[note.noteInput]);

                this.updateScale = note.ShouldUpdate;
                if (updateScale)
                {
                    note.OnNoteReleased += SetDurationScale;
                }
                else SetDurationScale();

                OnRemove += () => owner.noteIcons.Remove(this);

                icon.OnRightClick += Dispose;
                icon.OnDragVertical += SetPosition;
            }

            public readonly NoteCreator owner;
            public readonly NoteInfo note;
            public readonly NoteCreatorIcon icon;

            bool updateScale;

            readonly Action OnRemove;
            public void Dispose()
            {
                note.Dispose();

                Destroy(icon.gameObject);
                OnRemove();
            }

            void SetPosition(float position)
            {
                note.Start = (position / owner.timeScale) + owner.CurrentSongPosition;
            }

            public void UpdatePosition(float currentTime)
            {
                float pos = (note.Start - currentTime) * owner.timeScale;
                icon.SetVerticalPosition(pos);

                if (updateScale) UpdateScale(-pos);
            }

            void SetDurationScale()
            {
                updateScale = false;
                UpdateScale(note.duration * owner.timeScale);
                //Debug.Log(note.duration);
            }
            void UpdateScale(float setScale)
            {
                setScale = setScale * 2 + owner.iconMargin * 2;
                setScale = Mathf.Max(setScale, 150f);

                icon.SetVerticalScale(setScale);
            }

        }
        List<NoteIcon> noteIcons = new List<NoteIcon>();
        public IEnumerable<NoteInfo> Notes { get { return noteIcons.Select(n => n.note); } }


        // Las notas que estamos presionando ahora mismo
        // para después ver que duración le damos
        NoteInfo[] currentNotes;



        protected override void OnAwake()
        {
            currentNotes = new NoteInfo[keys.Length];

            // Seteamos la configuración de los botones
            playButton.onClick.AddListener(PlayButton);
            playButtonText = playButton.GetComponentInChildren<TextMeshProUGUI>();
            //playButtonOriginalLabel = playButtonText.text;

            List<string> difficultyStrings = new List<string>();
            foreach (var diff in difficulties)
            {
                difficultyStrings.Add(string.Format("{0} ({1} notas)", diff, (int)diff));
            }

            difficultyDropdown.ClearOptions();
            difficultyDropdown.AddOptions(difficultyStrings);
            SetDifficulty(0);
        }

        protected override void OnStart()
        {
            // Overrideamos para que no empiece automáticamente la canción

            defaultSongText = selectedSong.text;
            SetAssignedSong(assignSong);
        }
        void SetAssignedSong(Song song)
        {
            bool validSong = Song.CheckValid(song);

            playButton.interactable = validSong;
            selectedSong.text = validSong ? assignSong.Title : defaultSongText;
            assignSong = validSong ? song : null;
        }


        public void SetDifficulty(int difficultyIndex)
        {
            currentDifficulty = difficulties[difficultyIndex];
            Debug.Log("Difficulty set to " + currentDifficulty);
        }


        void PlayButton()
        {
            StartSong(assignSong);
            playButton.interactable = false;
        }

        protected override void OnSongTimeSet()
        {
            StartCoroutine(StartSongForNoteCreation());
        }
        IEnumerator StartSongForNoteCreation()
        {
            while (CurrentSongPosition < 0)
            {
                playButtonText.text = (-CurrentSongPosition).ToString("0");
                yield return null;
            }

            playButtonText.text = string.Join(" | ", keys.Select(k => k.ToString()).ToArray());
        }


        protected override void OnUpdateSongTime(float seconds, float beats)
        {
            // Después de actualizar el tiempo, tomamos todos los inputs 
            if (seconds >= 0)
            {
                for (byte i = 0; i < keys.Length; i++) HandleInput(i, seconds);
            }

            // Actualizamos la posición de todos los íconos de notas
            foreach (var icon in noteIcons)
            {
                icon.UpdatePosition(seconds);
            }
        }
        public void HandleInput(byte i, float time)
        {
            if (Input.GetKeyDown(keys[i]))
            {
                // Cuando presionemos la tecla, la añadimos a la lista
                // y al array de notas que se están presionando para saber si hacemos una nota larga
                NoteInfo newNote = new NoteInfo(i, time);

                AddNote(newNote);
                currentNotes[i] = newNote;
            }
            else if (Input.GetKeyUp(keys[i]))
            {
                NoteInfo note = currentNotes[i];
                if (note != null)
                {
                    note.UpdateTime(time, longNoteMinTime);

                    currentNotes[i] = null;
                }
            }
        }


        void AddNoteRange(IEnumerable<NoteInfo> range)
        {
            foreach (var note in range) AddNote(note, true);
        }
        void AddNote(NoteInfo newNote, bool checkDuplicates = false)
        {
            /*
            if (checkDuplicates && 
                notes.Any(n => 
                n.noteInput == newNote.noteInput &&
                Mathf.Approximately(n.startTime, newNote.startTime)
                ))
            {
                return;
            }
            */

            noteIcons.Add(new NoteIcon(this, newNote));
        }
        public void ClearAllNotes()
        {
            foreach (var note in noteIcons.ToArray())
            {
                note.Dispose();
            }
        }


        public void UseCurrentSheet()
        {
            if (assignSong == null) return;

            NoteSheet notesheet = assignSong.GetNotesheet(currentDifficulty);
            AddNoteRange(NoteInfo.FromNoteGroups(notesheet.GetNoteGroups, (byte)currentDifficulty, assignSong.BPM));
        }

        public void OpenNotesheet()
        {
            int previousVelocity = GetCurrentVelocityIndex;
            SetVelocityIndex(stopVelocityIndex);

            string path = EditorUtility.OpenFilePanel("Save notesheet", "", NoteInfo.notesheetFileFormat);
            if (!string.IsNullOrEmpty(path))
            {
                var noteRange = NoteInfo.LoadNotesheet(path);
                //Debug.Log(noteRange.Length);
                AddNoteRange(noteRange);
            }

            SetVelocityIndex(previousVelocity);
        }

        public void SaveNotesheet()
        {
            int previousVelocity = GetCurrentVelocityIndex;
            SetVelocityIndex(stopVelocityIndex);

            string path = EditorUtility.SaveFilePanel("Save notesheet", "", "notesheet", NoteInfo.notesheetFileFormat);
            if (!string.IsNullOrEmpty(path))
            {
                NoteInfo.SaveNotesheet(Notes.ToArray(), path);
            }

            SetVelocityIndex(previousVelocity);
        }


        protected override IEnumerable<string> DebugInfo()
        {
            yield return GetTimeDebug;
            yield return GetVelocityDebug;
        }


    }
}
