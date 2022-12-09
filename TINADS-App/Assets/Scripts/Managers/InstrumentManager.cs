using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Composing;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.MusicTheory;
using Melanchall.DryWetMidi.Standards;
using UnityEngine;

public enum eInstrumentType : byte
{
    Bass = 0,
    Snare,
    ClosedHiHat,
    CrashCymbal,
    HighTom,
    MidTom,
    LowTom,
    OpenHiHat,
    RideBell,
    RideCymbal1,
    RideCymbal2,
    SideStick,
    SplashCymbal,
    Count,
}

public struct HitInfo
{
    public float time;
    public eInstrumentType instrument;
    public Vector3 velocity;
    public Vector3 point;
    /// <summary>
    /// Scalar velocity between 0 and 1.
    /// </summary>
    public float normalizedVelocity;
}

public class InstrumentManager : SingletonBehaviour<InstrumentManager>
{
    private const string OutputDeviceName = "Microsoft GS Wavetable Synth";

    private OutputDevice m_OutputDevice;
    private Playback m_Playback;

    private PatternBuilder m_MetronomePatternBuilder;
    private MidiFile m_MetronomeMidiFile;
    private Playback m_MetronomePlayback;
    private readonly MusicalTimeSpan TIME_SPAN = MusicalTimeSpan.ThirtySecond;
    private const int TIME_SPAN_INT = 32 / 4;

    private MidiFile[] m_DrumMidiFiles;
    private Playback[] m_DrumPlayback;
    private bool[] m_DrumPlaybackCheck;
    private MidiFile m_MidiFile;

    private TempoMap m_RecordTempoMap = TempoMap.Default;
    private MidiFile m_RecordFile;
    private PatternBuilder m_RecordPatternBuilder;
    private HitInfo[] m_RecordNotes;
    private static readonly int MAX_NOTES = 4;
    private int m_RecordNotesSize = 0;

    public Action<HitInfo> onInstrumentHit;

    public static GeneralMidiPercussion ConvertInstrumentTypeToGeneralMidiPercussion(eInstrumentType instrumentType)
    {
        switch (instrumentType)
        {
            case eInstrumentType.Bass:
                return GeneralMidiPercussion.AcousticBassDrum;
            case eInstrumentType.Snare:
                return GeneralMidiPercussion.AcousticSnare;
            case eInstrumentType.ClosedHiHat:
                return GeneralMidiPercussion.ClosedHiHat;
            case eInstrumentType.CrashCymbal:
                return GeneralMidiPercussion.CrashCymbal1;
            case eInstrumentType.HighTom:
                return GeneralMidiPercussion.HighTom;
            case eInstrumentType.MidTom:
                return GeneralMidiPercussion.HiMidTom;
            case eInstrumentType.LowTom:
                return GeneralMidiPercussion.LowTom;
            case eInstrumentType.OpenHiHat:
                return GeneralMidiPercussion.OpenHiHat;
            case eInstrumentType.RideBell:
                return GeneralMidiPercussion.RideBell;
            case eInstrumentType.RideCymbal1:
                return GeneralMidiPercussion.RideCymbal1;
            case eInstrumentType.RideCymbal2:
                return GeneralMidiPercussion.RideCymbal2;
            case eInstrumentType.SideStick:
                return GeneralMidiPercussion.SideStick;
            case eInstrumentType.SplashCymbal:
                return GeneralMidiPercussion.SplashCymbal;
        }

        return GeneralMidiPercussion.Cowbell;
    }

    private void PlayNote(HitInfo info)
    {
        switch (info.instrument)
        {
            case eInstrumentType.Bass:
            case eInstrumentType.Snare:
            case eInstrumentType.ClosedHiHat:
            case eInstrumentType.CrashCymbal:
            case eInstrumentType.HighTom:
            case eInstrumentType.MidTom:
            case eInstrumentType.LowTom:
            case eInstrumentType.OpenHiHat:
            case eInstrumentType.RideBell:
            case eInstrumentType.RideCymbal1:
            case eInstrumentType.RideCymbal2:
            case eInstrumentType.SideStick:
            case eInstrumentType.SplashCymbal:
                if (m_DrumPlaybackCheck[(int)info.instrument])
                {
                    m_DrumPlayback[(int)info.instrument].MoveToStart();
                }
                m_DrumPlaybackCheck[(int)info.instrument] = true;
                m_DrumPlayback[(int)info.instrument].Start();
                break;
        }

        if (m_RecordNotesSize < MAX_NOTES)
        {
            m_RecordNotes[m_RecordNotesSize++] = info;
        }
    }

    private void Start()
    {
        InitializeOutputDevice();

        // Initialize metronome
        m_MetronomeMidiFile = CreateMetronomeFile(m_RecordTempoMap);
        InitializeMetronomePlayback();

        // Initialize drum midi files
        m_DrumMidiFiles = new MidiFile[(int)eInstrumentType.Count];
        m_DrumPlayback = new Playback[(int)eInstrumentType.Count];
        m_DrumPlaybackCheck = new bool[(int)eInstrumentType.Count];
        for (int i = 0; i < (int)eInstrumentType.Count; ++i)
        {
            m_DrumPlaybackCheck[i] = false;
        }
        Debug.Log($"Drum midi files count: {(int)eInstrumentType.Count}");
        foreach (eInstrumentType instrumentType in System.Enum.GetValues(typeof(eInstrumentType)))
        {
            if (instrumentType == eInstrumentType.Count)
            {
                break;
            }
            Debug.Log($"for each: {(int)instrumentType}/{(int)eInstrumentType.Count}");
            m_DrumMidiFiles[(int)instrumentType] = CreateDrumFile(instrumentType);
            InitializeDrumPlayback(instrumentType);
        }

        // Initialize record variables
        m_RecordPatternBuilder = new PatternBuilder()
            .SetNoteLength(TIME_SPAN);
        m_RecordNotes = new HitInfo[MAX_NOTES];
        
        StartMetronomePlayback();
        onInstrumentHit += PlayNote;
    }

    private void OnApplicationQuit()
    {
        Debug.Log("Releasing playback and device...");

        var recordFile = m_RecordPatternBuilder.Build().ToFile(m_RecordTempoMap, GeneralMidi.PercussionChannel);

        Debug.Log("Recorded MIDI file created.");

        recordFile.Write("Record.mid", overwriteFile: true);

        if (m_Playback != null)
        {
            m_Playback.NotesPlaybackStarted -= OnNotesPlaybackStarted;
            m_Playback.NotesPlaybackFinished -= OnNotesPlaybackFinished;
            m_Playback.Dispose();
        }

        if (m_MetronomePlayback != null)
        {
            m_MetronomePlayback.NotesPlaybackStarted -= OnMetronomePlaybackStarted;
            m_MetronomePlayback.NotesPlaybackFinished -= OnMetronomePlaybackFinished;
            m_MetronomePlayback.Dispose();
        }

        foreach (Playback playback in m_DrumPlayback)
        {
            if (playback != null)
            {
                playback.NotesPlaybackStarted -= OnNotesPlaybackStarted;
                playback.NotesPlaybackFinished -= OnNotesPlaybackFinished;
                playback.Dispose();
            }
        }

        if (m_OutputDevice != null)
            m_OutputDevice.Dispose();

        Debug.Log("Playback and device released.");
    }

    private void InitializeOutputDevice()
    {
        Debug.Log($"Initializing output device [{OutputDeviceName}]...");

        var allOutputDevices = OutputDevice.GetAll();
        if (!allOutputDevices.Any(d => d.Name == OutputDeviceName))
        {
            var allDevicesList = string.Join(Environment.NewLine, allOutputDevices.Select(d => $"  {d.Name}"));
            Debug.Log($"There is no [{OutputDeviceName}] device presented in the system. Here the list of all device:{Environment.NewLine}{allDevicesList}");
            return;
        }

        m_OutputDevice = OutputDevice.GetByName(OutputDeviceName);
        Debug.Log($"Output device [{OutputDeviceName}] initialized.");
    }

    private MidiFile CreateDrumFile(eInstrumentType instrumentType)
    {
        Debug.Log("Creating test MIDI file...");

        var patternBuilder = new PatternBuilder()
            .SetNoteLength(MusicalTimeSpan.Eighth)
            .SetVelocity(SevenBitNumber.MaxValue)
            .Note(
                Melanchall.DryWetMidi.MusicTheory.Note.Get(
                    GeneralMidiUtilities.AsSevenBitNumber(ConvertInstrumentTypeToGeneralMidiPercussion(instrumentType))
                    )
                );


        var midiFile = patternBuilder.Build().ToFile(TempoMap.Default, GeneralMidi.PercussionChannel);

        Debug.Log("Test MIDI file created.");

        midiFile.Write($"Drum_{instrumentType}.mid", overwriteFile: true);

        return midiFile;
    }

    private MidiFile CreateMetronomeFile(TempoMap tempo)
    {
        Debug.Log("Creating test MIDI file...");

        MusicalTimeSpan TIME_SPAN = MusicalTimeSpan.ThirtySecond;
        int TIME_SPAN_INT = 32 / 4;

        m_MetronomePatternBuilder = new PatternBuilder()
            .SetNoteLength(TIME_SPAN);

        // first beat
        m_MetronomePatternBuilder.SetVelocity(SevenBitNumber.MaxValue)
            .Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(GeneralMidiUtilities.AsSevenBitNumber(GeneralMidiPercussion.Cabasa)));
        for (int i = 0; i < TIME_SPAN_INT - 1; ++i)
        {
            m_MetronomePatternBuilder.SetVelocity(SevenBitNumber.MinValue)
                .Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(GeneralMidiUtilities.AsSevenBitNumber(GeneralMidiPercussion.Cabasa)));
        }

        // second beat
        m_MetronomePatternBuilder.SetVelocity((SevenBitNumber)101)
            .Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(GeneralMidiUtilities.AsSevenBitNumber(GeneralMidiPercussion.Cabasa)));
        for (int i = 0; i < TIME_SPAN_INT - 1; ++i)
        {
            m_MetronomePatternBuilder.SetVelocity(SevenBitNumber.MinValue)
                .Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(GeneralMidiUtilities.AsSevenBitNumber(GeneralMidiPercussion.Cabasa)));
        }

        // third beat
        m_MetronomePatternBuilder.SetVelocity((SevenBitNumber)75)
            .Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(GeneralMidiUtilities.AsSevenBitNumber(GeneralMidiPercussion.Cabasa)));
        for (int i = 0; i < TIME_SPAN_INT - 1; ++i)
        {
            m_MetronomePatternBuilder.SetVelocity(SevenBitNumber.MinValue)
                .Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(GeneralMidiUtilities.AsSevenBitNumber(GeneralMidiPercussion.Cabasa)));
        }

        // fourth beat
        m_MetronomePatternBuilder.SetVelocity((SevenBitNumber)75)
            .Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(GeneralMidiUtilities.AsSevenBitNumber(GeneralMidiPercussion.Cabasa)));
        for (int i = 0; i < TIME_SPAN_INT - 1; ++i)
        {
            m_MetronomePatternBuilder.SetVelocity(SevenBitNumber.MinValue)
                .Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(GeneralMidiUtilities.AsSevenBitNumber(GeneralMidiPercussion.Cabasa)));
        }

        var midiFile = m_MetronomePatternBuilder.Build().ToFile(tempo, GeneralMidi.PercussionChannel);

        Debug.Log("Test MIDI file created.");

        return midiFile;
    }

    private MidiFile CreateTestFile()
    {
        Debug.Log("Creating test MIDI file...");

        var patternBuilder = new PatternBuilder()
            .SetNoteLength(MusicalTimeSpan.Eighth)
            .SetVelocity(SevenBitNumber.MaxValue)
            //.ProgramChange(GeneralMidiProgram.Harpsichord);
            .Anchor()
            .Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(GeneralMidiUtilities.AsSevenBitNumber(GeneralMidiPercussion.AcousticBassDrum)))
            .MoveToFirstAnchor()
            .Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(GeneralMidiUtilities.AsSevenBitNumber(GeneralMidiPercussion.ClosedHiHat)))
            .Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(GeneralMidiUtilities.AsSevenBitNumber(GeneralMidiPercussion.ClosedHiHat)))
            .Anchor()
            .Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(GeneralMidiUtilities.AsSevenBitNumber(GeneralMidiPercussion.AcousticSnare)))
            .MoveToLastAnchor()
            .Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(GeneralMidiUtilities.AsSevenBitNumber(GeneralMidiPercussion.ClosedHiHat)))
            .Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(GeneralMidiUtilities.AsSevenBitNumber(GeneralMidiPercussion.ClosedHiHat)))
            .Repeat(3, 1);


        var midiFile = patternBuilder.Build().ToFile(TempoMap.Default, GeneralMidi.PercussionChannel);

        Debug.Log("Test MIDI file created.");

        midiFile.Write("SampleFile.mid", overwriteFile: true);

        return midiFile;
    }

    private void InitializeDrumPlayback(eInstrumentType instrumentType)
    {
        Debug.Log("Initializing drum playback...");

        m_DrumPlayback[(int)instrumentType] = m_DrumMidiFiles[(int)instrumentType].GetPlayback(m_OutputDevice);
        m_DrumPlayback[(int)instrumentType].NotesPlaybackStarted += OnNotesPlaybackStarted;
        m_DrumPlayback[(int)instrumentType].NotesPlaybackFinished += OnNotesPlaybackFinished;

        Debug.Log("Playback initialized.");
    }

    private void InitializeFilePlayback(MidiFile midiFile)
    {
        Debug.Log("Initializing playback...");

        m_Playback = midiFile.GetPlayback(m_OutputDevice);
        m_Playback.Loop = true;
        m_Playback.NotesPlaybackStarted += OnNotesPlaybackStarted;
        m_Playback.NotesPlaybackFinished += OnNotesPlaybackFinished;

        Debug.Log("Playback initialized.");
    }

    private void InitializeMetronomePlayback()
    {
        Debug.Log("Initializing metronome playback...");

        m_MetronomePlayback = m_MetronomeMidiFile.GetPlayback(m_OutputDevice);
        m_MetronomePlayback.Loop = true;
        m_MetronomePlayback.NotesPlaybackStarted += OnMetronomePlaybackStarted;
        m_MetronomePlayback.NotesPlaybackFinished += OnMetronomePlaybackFinished;

        Debug.Log("Metronome playback initialized.");
    }

    private void StartMetronomePlayback()
    {
        Debug.Log("Starting metronome playback...");
        m_MetronomePlayback.Start();
    }

    private void StartPlayback()
    {
        Debug.Log("Starting playback...");
        m_Playback.Start();
    }

    private void OnNotesPlaybackFinished(object sender, NotesEventArgs e)
    {
        LogNotes("Notes finished:", e);
    }

    private void OnNotesPlaybackStarted(object sender, NotesEventArgs e)
    {
        LogNotes("Notes started:", e);
    }

    private void OnMetronomePlaybackFinished(object sender, NotesEventArgs e)
    {
        //LogNotes("Metronome finished:", e);
    }

    private void OnMetronomePlaybackStarted(object sender, NotesEventArgs e)
    {
        LogNotes("Metronome started:", e);

        if (m_RecordNotesSize > 0)
        {
            Debug.Log($"Notes in queue1: {m_RecordNotesSize}");
            m_RecordPatternBuilder.Anchor();
            for (int i = 0; i < m_RecordNotesSize; ++i)
            {
                Debug.Log($"\tNote[{i}]: {m_RecordNotes[i].instrument}");
                m_RecordPatternBuilder.MoveToLastAnchor()
                    .SetVelocity((SevenBitNumber)(int)((float)((int)SevenBitNumber.MaxValue) * m_RecordNotes[i].normalizedVelocity))
                    .Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(GeneralMidiUtilities.AsSevenBitNumber(ConvertInstrumentTypeToGeneralMidiPercussion(m_RecordNotes[i].instrument))));
            }
            m_RecordNotesSize = 0;
        }
        else
        {
            Debug.Log($"Notes in queue2: {m_RecordNotesSize}");
            m_RecordPatternBuilder.SetVelocity(SevenBitNumber.MinValue)
                .Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(GeneralMidiUtilities.AsSevenBitNumber(GeneralMidiPercussion.CrashCymbal1)));
        }
    }

    private void LogNotes(string title, NotesEventArgs e)
    {
        var message = new StringBuilder()
            .AppendLine(title)
            .AppendLine(string.Join(Environment.NewLine, e.Notes.Select(n => $"  {n}")))
            .ToString();
        Debug.Log(message.Trim());
    }
}
