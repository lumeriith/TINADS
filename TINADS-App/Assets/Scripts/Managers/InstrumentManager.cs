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
using UnityEngine.XR;

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
    private bool m_IsMetronomePlaying = false;

    private MidiFile[] m_DrumMidiFiles;
    private Playback[] m_DrumPlayback;
    private MidiFile m_MidiFile;

    private TempoMap m_RecordTempoMap = TempoMap.Default;
    private MidiFile m_RecordFile;
    private PatternBuilder m_RecordPatternBuilder;
    private HitInfo[] m_RecordNotesBuffer;
    private static readonly int MAX_NOTES = 4;
    private int m_RecordNotesSize = 0;
    private bool m_IsRecording = false;
    private int m_RecordCount = 0;

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
                m_DrumPlayback[(int)info.instrument].MoveToStart();
                m_DrumPlayback[(int)info.instrument].Start();
                break;
        }

        if (m_IsRecording)
        {
            if (m_RecordNotesSize < MAX_NOTES)
            {
                m_RecordNotesBuffer[m_RecordNotesSize++] = info;
            }
        }
    }

    private void Start()
    {
        InitializeOutputDevice();

        // Initialize metronome
        CreateMetronomePatternBuilder();

        // Initialize drum midi files
        m_DrumMidiFiles = new MidiFile[(int)eInstrumentType.Count];
        m_DrumPlayback = new Playback[(int)eInstrumentType.Count];

        foreach (eInstrumentType instrumentType in System.Enum.GetValues(typeof(eInstrumentType)))
        {
            if (instrumentType == eInstrumentType.Count)
            {
                break;
            }
            m_DrumMidiFiles[(int)instrumentType] = CreateDrumFile(instrumentType);
            InitializeDrumPlayback(instrumentType);
        }

        // Initialize record variables
        m_RecordPatternBuilder = new PatternBuilder()
            .SetNoteLength(TIME_SPAN);
        m_RecordNotesBuffer = new HitInfo[MAX_NOTES];

        onInstrumentHit += PlayNote;
    }

    private void SetTempo(int tempoByQuarters)
    {
        long microsecondsPerQuarterNote = 60000000 / (long)tempoByQuarters;
        Tempo tempo = new Tempo(microsecondsPerQuarterNote);
        m_RecordTempoMap = TempoMap.Create(tempo);
    }

    private void StartMetronome(TempoMap tempoMap)
    {
        if (tempoMap != m_RecordTempoMap && m_MetronomePlayback != null)
        {
            m_MetronomePlayback.NotesPlaybackStarted -= OnMetronomePlaybackStarted;
            m_MetronomePlayback.NotesPlaybackFinished -= OnMetronomePlaybackFinished;
            m_MetronomePlayback.Dispose();
            m_MetronomeMidiFile = null;
        }

        if (m_MetronomePlayback == null)
        {
            m_MetronomeMidiFile = m_MetronomePatternBuilder.Build().ToFile(tempoMap, GeneralMidi.PercussionChannel);
            InitializeMetronomePlayback();
        }

        m_IsMetronomePlaying = true;

        m_MetronomePlayback.MoveToStart();
        StartMetronomePlayback();
    }

    private void StartRecording()
    {
        if (m_IsMetronomePlaying)
        {
            m_IsRecording = true;

            // Initialize record variables
            m_RecordPatternBuilder = new PatternBuilder()
                .SetNoteLength(TIME_SPAN);
            m_RecordNotesSize = 0;
        }
    }

    private void StopMetronome()
    {
        m_MetronomePlayback.Stop();

        m_IsMetronomePlaying = false;

        if (m_IsRecording)
        {
            StopRecording();
        }
    }

    private void StopRecording()
    {
        m_IsRecording = false;

        var recordFile = m_RecordPatternBuilder.Build().ToFile(m_RecordTempoMap, GeneralMidi.PercussionChannel);

        Debug.Log("Recorded MIDI file created.");

        recordFile.Write($"Record{m_RecordCount}.mid", overwriteFile: true);
        ++m_RecordCount;
    }

    private void OnApplicationQuit()
    {
        Debug.Log("Releasing playback and device...");

        if (m_IsRecording)
        {
            StopRecording();
        }

        if (m_IsMetronomePlaying)
        {
            StopMetronome();
        }

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
        Debug.Log("Creating drum MIDI file...");

        var patternBuilder = new PatternBuilder()
            .SetNoteLength(MusicalTimeSpan.Eighth)
            .SetVelocity(SevenBitNumber.MaxValue)
            .Note(
                Melanchall.DryWetMidi.MusicTheory.Note.Get(
                    GeneralMidiUtilities.AsSevenBitNumber(ConvertInstrumentTypeToGeneralMidiPercussion(instrumentType))
                    )
                );


        var midiFile = patternBuilder.Build().ToFile(TempoMap.Default, GeneralMidi.PercussionChannel);

        Debug.Log("Drum MIDI file created.");

        midiFile.Write($"Drum_{instrumentType}.mid", overwriteFile: true);

        return midiFile;
    }

    private PatternBuilder CreateMetronomePatternBuilder()
    {
        Debug.Log("Creating Metronome pattern builder...");

        const long TIME_SPAN_INT = 128 / 4;
        MusicalTimeSpan TIME_SPAN = new MusicalTimeSpan(TIME_SPAN_INT * 4);

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

        Debug.Log("Test Metronome pattern builder created.");

        return m_MetronomePatternBuilder;
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
        //m_MetronomePlayback.NotesPlaybackFinished += OnMetronomePlaybackFinished;

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
        //LogNotes("Metronome started:", e);

        if (m_IsRecording)
        {
            if (m_RecordNotesSize > 0)
            {
                m_RecordPatternBuilder.Anchor();
                for (int i = 0; i < m_RecordNotesSize; ++i)
                {
                    m_RecordPatternBuilder.MoveToLastAnchor()
                        .SetVelocity((SevenBitNumber)(int)((float)((int)SevenBitNumber.MaxValue) * m_RecordNotesBuffer[i].normalizedVelocity))
                        .Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(GeneralMidiUtilities.AsSevenBitNumber(ConvertInstrumentTypeToGeneralMidiPercussion(m_RecordNotesBuffer[i].instrument))));
                }
                m_RecordNotesSize = 0;
            }
            else
            {
                m_RecordPatternBuilder.SetVelocity(SevenBitNumber.MinValue)
                    .Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(GeneralMidiUtilities.AsSevenBitNumber(GeneralMidiPercussion.CrashCymbal1)));
            }
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
