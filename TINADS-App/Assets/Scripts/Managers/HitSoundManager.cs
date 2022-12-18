using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitSoundManager : MonoBehaviour
{
    [Serializable]
    public class HitSoundMapping
    {
        public eInstrumentType instrument;
        public AudioClip[] clips;
    }

    public List<HitSoundMapping> settings;
    public AnimationCurve physToAudioVelocityCurve;
    public GameObject notePlayerPrefab;

    private List<GameObject> _alivePlayers = new List<GameObject>(); 

    void Start()
    {
        InstrumentManager.instance.onInstrumentHit += OnInstrumentHit;
        InstrumentManager.instance.onMetronomeBeat += beat =>
        {
            if (beat == 0)
            {
                PlayNote(new HitInfo
                {
                    instrument = eInstrumentType.Bass,
                    normalizedVelocity = 1
                });
                return;
            }
            PlayNote(new HitInfo
            {
                instrument = eInstrumentType.Bass,
                normalizedVelocity = 0.7f,
                point = Drumset.instance.transform.position
            });
        };
    }

    private void PlayNote(HitInfo obj)
    {
        foreach (var mapping in settings)
        {
            if (mapping.instrument != obj.instrument) continue;
            var index = Mathf.RoundToInt(obj.normalizedVelocity * mapping.clips.Length);
            index = Mathf.Clamp(index, 0, mapping.clips.Length - 1);
            var player = Instantiate(notePlayerPrefab, obj.point, Quaternion.identity);
            player.name = $"Note {mapping.clips[index].name}";
            var audio = player.GetComponent<AudioSource>();
            audio.clip = mapping.clips[index];
            audio.Play();
            _alivePlayers.Add(player);
            if (_alivePlayers.Count > 10)
            {
                if (_alivePlayers[0] != null) Destroy(_alivePlayers[0]);
                _alivePlayers.RemoveAt(0);
            }
        }
    }
    
    private void OnInstrumentHit(HitInfo obj)
    {
        obj.normalizedVelocity = physToAudioVelocityCurve.Evaluate(obj.normalizedVelocity);
        PlayNote(obj);
    }
}
