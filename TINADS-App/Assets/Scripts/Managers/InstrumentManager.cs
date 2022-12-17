using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Composing;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.MusicTheory;
using Melanchall.DryWetMidi.Standards;
using Melanchall.DryWetMidi.Tools;
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
    public Collider collider;
    /// <summary>
    /// Scalar velocity between 0 and 1.
    /// </summary>
    public float normalizedVelocity;
}

public class InstrumentManager : SingletonBehaviour<InstrumentManager>
{
    private const string OutputDeviceName = "Microsoft GS Wavetable Synth";

    public bool enableImmediateNotePlayback;
    
    private OutputDevice m_OutputDevice;
    private Playback m_Playback;

    private PatternBuilder m_MetronomePatternBuilder;
    private MidiFile m_MetronomeMidiFile;
    private Playback m_MetronomePlayback;
    private const int TIME_SPAN_INT = 128 / 4;
    private readonly MusicalTimeSpan TIME_SPAN = new MusicalTimeSpan(TIME_SPAN_INT * 4);
    private bool m_IsMetronomePlaying = false;
    private MidiTimeSpan m_CurrentMetronomeTime;

    private string m_BackgroundMidiFilePath;
    private MidiFile m_BackgroundMidiFile;
    private Playback m_BackgroundPlayback;
    private bool m_IsBackgroundPlaying = false;

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
    private DateTime m_RecordingStarted;

    private bool m_IsCalculatingBpm = false;
    private bool m_HasBpmCounterStarted = false;
    private DateTime m_LastBpmDateTime;
    private double m_BpmSumByMilliseconds = 0.0;
    private int m_BpmCounter = 0;

    public Action<HitInfo> onInstrumentHit;
    public Action<bool> onIsRecordingChanged;

    public static GeneralMidiPercussion ConvertInstrumentTypeToGeneralMidiPercussion(eInstrumentType instrumentType)
    {
        switch (instrumentType)
        {
            case eInstrumentType.Bass:
                return GeneralMidiPercussion.BassDrum1;
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

    public bool IsRecording()
    {
        return m_IsRecording;
    }

    public bool IsMetronomePlaying()
    {
        return m_IsMetronomePlaying;
    }

    public bool IsCalculatingBpm()
    {
        return m_IsCalculatingBpm;
    }

    public double GetCurrentRecordingDurationBySeconds()
    {
        return (DateTime.Now - m_RecordingStarted).TotalSeconds;
    }

    public long GetCurrentBpm()
    {
        if (m_IsCalculatingBpm)
        {
            if (m_BpmCounter > 0)
            {
                return 60000000 / (long)(((m_BpmSumByMilliseconds / (double)m_BpmCounter) * 1000.0) + 0.5);
            }

        }
        else if (m_CurrentMetronomeTime != null)
        {
            return (long) (m_RecordTempoMap.GetTempoAtTime(m_CurrentMetronomeTime).BeatsPerMinute + 0.5);
        }

        return 0;
    }

    public bool InitializeBackground(string midiFilePath)
    {
        Debug.Log("[MIDI] Initializing background midifile and playback...");

        m_BackgroundMidiFilePath = midiFilePath;

        m_BackgroundMidiFile = MidiFile.Read(m_BackgroundMidiFilePath);

        if (m_BackgroundMidiFile == null)
        {
            return false;
        }

        m_BackgroundPlayback = m_BackgroundMidiFile.GetPlayback(m_OutputDevice);

        Debug.Log($"[MIDI] Background {m_RecordTempoMap.TimeDivision} initialized.");

        return true;
    }

    public void SetTempo(int tempoByQuarters)
    {
        if (tempoByQuarters <= 0)
        {
            Debug.Log($"[MIDI] Invalid tempo of {tempoByQuarters}!!");
            return;
        }

        long microsecondsPerQuarterNote = 60000000 / (long)tempoByQuarters;
        Tempo tempo = new Tempo(microsecondsPerQuarterNote);
        m_RecordTempoMap = TempoMap.Create(tempo);
    }

    public void StartBackground()
    {
        Debug.Log("[MIDI] Starting background...");

        if (m_IsMetronomePlaying)
        {
            StopMetronome();
        }

        if (m_IsBackgroundPlaying)
        {
            StopBackground();

            if (m_BackgroundPlayback != null)
            {
                m_BackgroundPlayback.Dispose();
                m_BackgroundPlayback = null;
            }
        }

        if (m_BackgroundPlayback == null)
        {
            InitializeBackground(m_BackgroundMidiFilePath);
        }

        if (m_MetronomePlayback != null)
        {
            m_MetronomePlayback.NotesPlaybackStarted -= OnMetronomePlaybackStarted;
            m_MetronomePlayback.Dispose();
            m_MetronomeMidiFile = null;
        }

        m_RecordTempoMap = m_BackgroundMidiFile.GetTempoMap();

        if (m_MetronomeMidiFile == null || m_MetronomePlayback == null)
        {
            CreateMetronomePatternBuilder();
            m_MetronomeMidiFile = m_MetronomePatternBuilder.Build().ToFile(TempoMap.Default, GeneralMidi.PercussionChannel);
            MidiTimeSpan metronomeTimeSpan = Melanchall.DryWetMidi.Interaction.MidiFileUtilities.GetDuration<MidiTimeSpan>(m_MetronomeMidiFile);
            MidiTimeSpan backgroundTimeSpan = Melanchall.DryWetMidi.Interaction.MidiFileUtilities.GetDuration<MidiTimeSpan>(m_BackgroundMidiFile);
            double repeatTimes = backgroundTimeSpan.Divide(metronomeTimeSpan);
            Debug.Log($"repeatTimes: {repeatTimes}");
            m_MetronomePatternBuilder.Repeat(TIME_SPAN_INT * 4, (int)(repeatTimes + 0.5f));
            m_MetronomeMidiFile = m_MetronomePatternBuilder.Build().ToFile(m_RecordTempoMap, GeneralMidi.PercussionChannel);

            InitializeMetronomePlayback();
        }

        PlaybackCurrentTimeWatcher.Instance.AddPlayback(m_MetronomePlayback, TimeSpanType.Midi);
        PlaybackCurrentTimeWatcher.Instance.CurrentTimeChanged += OnCurrentTimeChanged;

        m_IsMetronomePlaying = true;
        m_MetronomePlayback.Loop = true;
        m_MetronomePlayback.MoveToStart();

        m_IsBackgroundPlaying = true;
        m_BackgroundPlayback.MoveToStart();

        StartRecording();
        PlaybackCurrentTimeWatcher.Instance.Start();
        StartMetronomePlayback();
        StartBackgroundPlayback();
    }

    public void StartBpmCounter()
    {
        if (m_IsMetronomePlaying)
        {
            StopMetronome();
        }

        m_IsCalculatingBpm = true;
        m_HasBpmCounterStarted = false;
    }

    public void StartMetronome(int tempoByQuarters)
    {
        if (m_IsMetronomePlaying)
        {
            StopMetronome();

            if (m_MetronomePlayback != null)
            {
                m_MetronomePlayback.NotesPlaybackStarted -= OnMetronomePlaybackStarted;
                m_MetronomePlayback.Dispose();
                m_MetronomeMidiFile = null;
                m_MetronomePlayback = null;
            }
        }

        SetTempo(tempoByQuarters);
        CreateMetronomePatternBuilder();
        m_MetronomeMidiFile = m_MetronomePatternBuilder.Build().ToFile(m_RecordTempoMap, GeneralMidi.PercussionChannel);
        InitializeMetronomePlayback();

        m_IsMetronomePlaying = true;

        PlaybackCurrentTimeWatcher.Instance.AddPlayback(m_MetronomePlayback, TimeSpanType.Midi);
        PlaybackCurrentTimeWatcher.Instance.CurrentTimeChanged += OnCurrentTimeChanged;
        PlaybackCurrentTimeWatcher.Instance.Start();

        StartMetronomePlayback();
    }

    public void StartRecording()
    {
        if (m_IsMetronomePlaying || m_IsBackgroundPlaying)
        {
            m_IsRecording = true;
            onIsRecordingChanged?.Invoke(true);

            // Initialize record variables
            m_RecordPatternBuilder = new PatternBuilder()
                .SetNoteLength(TIME_SPAN);
            m_RecordNotesSize = 0;

            m_RecordingStarted = DateTime.Now;
        }
    }

    public void StopBackground()
    {
        m_BackgroundPlayback.Stop();

        m_IsBackgroundPlaying = false;

        if (m_IsRecording)
        {
            StopRecording();
        }

        if (m_IsMetronomePlaying)
        {
            StopMetronome();
        }

        Debug.Log("[MIDI] Background stopped.");
    }

    public void StopBpmCounter()
    {
        m_IsCalculatingBpm = false;
        m_HasBpmCounterStarted = false;

        Tempo tempo = new Tempo((long) (((m_BpmSumByMilliseconds / (double) m_BpmCounter) * 1000.0) + 0.5));

        Debug.Log($"Tempo set to {60000000 / (long)(((m_BpmSumByMilliseconds / (double)m_BpmCounter) * 1000.0) + 0.5)}");
        m_RecordTempoMap = TempoMap.Create(tempo);
    }

    public void StopMetronome()
    {
        m_MetronomePlayback.Stop();

        m_IsMetronomePlaying = false;

        if (m_IsRecording)
        {
            StopRecording();
        }

        Debug.Log("[MIDI] Metronome stopped.");
    }

    public void StopRecording()
    {
        m_IsRecording = false;
        onIsRecordingChanged?.Invoke(false);

        var recordFile = m_RecordPatternBuilder.Build().ToFile(m_RecordTempoMap, GeneralMidi.PercussionChannel);

        Debug.Log("[MIDI] Recorded MIDI file created.");

        var tinadsRecordings = Directory.CreateDirectory($"{System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal)}/TINADS Recordings");

        recordFile.Write($"{System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal)}/TINADS Recordings/{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss", System.Globalization.CultureInfo.InvariantCulture)}.mid", overwriteFile: true);
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
            m_DrumMidiFiles[(int)instrumentType] = MidiFile.Read($"Drum_{instrumentType}.mid");
            if (m_DrumMidiFiles[(int)instrumentType] == null)
            {
                m_DrumMidiFiles[(int)instrumentType] = CreateDrumFile(instrumentType);
            }
            InitializeDrumPlayback(instrumentType);
        }

        // Initialize record variables
        m_RecordPatternBuilder = new PatternBuilder()
            .SetNoteLength(TIME_SPAN);
        m_RecordNotesBuffer = new HitInfo[MAX_NOTES];

        onInstrumentHit += info =>
        {
            if (enableImmediateNotePlayback) PlayNote(info);

            if (m_HasBpmCounterStarted)
            {
                DateTime now = DateTime.Now;
                m_BpmSumByMilliseconds += (now - m_LastBpmDateTime).TotalMilliseconds;
                m_LastBpmDateTime = now;
                ++m_BpmCounter;
            }
            else if (m_IsCalculatingBpm)
            {
                m_HasBpmCounterStarted = true;
                m_BpmSumByMilliseconds = 0.0f;
                m_BpmCounter = 0;
                m_LastBpmDateTime = DateTime.Now;
            }
        };
    }

    private void OnApplicationQuit()
    {
        Debug.Log("[MIDI] Releasing playback and device...");

        if (m_IsCalculatingBpm)
        {
            StopBpmCounter();
        }

        if (m_IsBackgroundPlaying)
        {
            StopBackground();
        }

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
            m_Playback.Dispose();
        }

        if (m_MetronomePlayback != null)
        {
            m_MetronomePlayback.NotesPlaybackStarted -= OnMetronomePlaybackStarted;
            m_MetronomePlayback.Dispose();
        }

        foreach (Playback playback in m_DrumPlayback)
        {
            if (playback != null)
            {
                playback.Dispose();
            }
        }

        if (m_OutputDevice != null)
            m_OutputDevice.Dispose();

        Debug.Log("[MIDI] Playback and device released.");
    }

    private void InitializeOutputDevice()
    {
        Debug.Log($"[MIDI] Initializing output device [{OutputDeviceName}]...");

        var allOutputDevices = OutputDevice.GetAll();
        if (!allOutputDevices.Any(d => d.Name == OutputDeviceName))
        {
            var allDevicesList = string.Join(Environment.NewLine, allOutputDevices.Select(d => $"  {d.Name}"));
            Debug.Log($"[MIDI] There is no [{OutputDeviceName}] device presented in the system. Here the list of all device:{Environment.NewLine}{allDevicesList}");
            return;
        }

        m_OutputDevice = OutputDevice.GetByName(OutputDeviceName);
        Debug.Log($"[MIDI] Output device [{OutputDeviceName}] initialized.");
    }

    private MidiFile CreateDrumFile(eInstrumentType instrumentType)
    {
        Debug.Log("[MIDI] Creating drum MIDI file...");

        var patternBuilder = new PatternBuilder()
            .SetNoteLength(MusicalTimeSpan.Eighth)
            .SetVelocity(SevenBitNumber.MaxValue)
            .Note(
                Melanchall.DryWetMidi.MusicTheory.Note.Get(
                    GeneralMidiUtilities.AsSevenBitNumber(ConvertInstrumentTypeToGeneralMidiPercussion(instrumentType))
                    )
                );


        var midiFile = patternBuilder.Build().ToFile(TempoMap.Default, GeneralMidi.PercussionChannel);

        Debug.Log("[MIDI] Drum MIDI file created.");

        midiFile.Write($"Drum_{instrumentType}.mid", overwriteFile: true);

        return midiFile;
    }

    private PatternBuilder CreateMetronomePatternBuilder()
    {
        Debug.Log("[MIDI] Creating Metronome pattern builder...");

        m_MetronomePatternBuilder = new PatternBuilder()
            .SetNoteLength(TIME_SPAN);

        // first beat
        m_MetronomePatternBuilder.SetVelocity(SevenBitNumber.MaxValue)
            .Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(GeneralMidiUtilities.AsSevenBitNumber(GeneralMidiPercussion.BassDrum1)));
        for (int i = 0; i < TIME_SPAN_INT - 1; ++i)
        {
            m_MetronomePatternBuilder.SetVelocity(SevenBitNumber.MinValue)
                .Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(GeneralMidiUtilities.AsSevenBitNumber(GeneralMidiPercussion.BassDrum1)));
        }

        // second beat
        m_MetronomePatternBuilder.SetVelocity((SevenBitNumber)101)
            .Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(GeneralMidiUtilities.AsSevenBitNumber(GeneralMidiPercussion.BassDrum1)));
        for (int i = 0; i < TIME_SPAN_INT - 1; ++i)
        {
            m_MetronomePatternBuilder.SetVelocity(SevenBitNumber.MinValue)
                .Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(GeneralMidiUtilities.AsSevenBitNumber(GeneralMidiPercussion.BassDrum1)));
        }

        // third beat
        m_MetronomePatternBuilder.SetVelocity((SevenBitNumber)75)
            .Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(GeneralMidiUtilities.AsSevenBitNumber(GeneralMidiPercussion.BassDrum1)));
        for (int i = 0; i < TIME_SPAN_INT - 1; ++i)
        {
            m_MetronomePatternBuilder.SetVelocity(SevenBitNumber.MinValue)
                .Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(GeneralMidiUtilities.AsSevenBitNumber(GeneralMidiPercussion.BassDrum1)));
        }

        // fourth beat
        m_MetronomePatternBuilder.SetVelocity((SevenBitNumber)75)
            .Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(GeneralMidiUtilities.AsSevenBitNumber(GeneralMidiPercussion.BassDrum1)));
        for (int i = 0; i < TIME_SPAN_INT - 1; ++i)
        {
            m_MetronomePatternBuilder.SetVelocity(SevenBitNumber.MinValue)
                .Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(GeneralMidiUtilities.AsSevenBitNumber(GeneralMidiPercussion.BassDrum1)));
        }

        Debug.Log("[MIDI] Metronome pattern builder created.");

        return m_MetronomePatternBuilder;
    }

    private void InitializeDrumPlayback(eInstrumentType instrumentType)
    {
        //Debug.Log("[MIDI] Initializing drum playback...");

        m_DrumPlayback[(int)instrumentType] = m_DrumMidiFiles[(int)instrumentType].GetPlayback(m_OutputDevice);

        //Debug.Log("[MIDI] Playback initialized.");
    }

    private void InitializeFilePlayback(MidiFile midiFile)
    {
        Debug.Log("[MIDI] Initializing playback...");

        m_Playback = midiFile.GetPlayback(m_OutputDevice);
        m_Playback.Loop = true;

        Debug.Log("[MIDI] Playback initialized.");
    }

    private void InitializeMetronomePlayback()
    {
        Debug.Log("[MIDI] Initializing metronome playback...");

        m_MetronomePlayback = m_MetronomeMidiFile.GetPlayback(m_OutputDevice);
        m_MetronomePlayback.Loop = true;
        m_MetronomePlayback.NotesPlaybackStarted += OnMetronomePlaybackStarted;

        Debug.Log("[MIDI] Metronome playback initialized.");
    }

    private void StartBackgroundPlayback()
    {
        m_BackgroundPlayback.Start();
    }

    private void StartMetronomePlayback()
    {
        m_MetronomePlayback.Start();
    }

    private void StartPlayback()
    {
        m_Playback.Start();
    }

    private void OnMetronomePlaybackStarted(object sender, NotesEventArgs e)
    {
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

    private void OnCurrentTimeChanged(object sender, PlaybackCurrentTimeChangedEventArgs e)
    {
        foreach (var playbackTime in e.Times)
        {
            var playback = playbackTime.Playback;
            m_CurrentMetronomeTime = (MidiTimeSpan)playbackTime.Time;
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
