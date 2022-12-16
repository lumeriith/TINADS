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

    void Start()
    {
        InstrumentManager.instance.onInstrumentHit += OnInstrumentHit;
    }

    private void OnInstrumentHit(HitInfo obj)
    {
        var normalizedVel = physToAudioVelocityCurve.Evaluate(obj.normalizedVelocity);
        
        foreach (var mapping in settings)
        {
            if (mapping.instrument != obj.instrument) continue;
            var index = Mathf.RoundToInt(normalizedVel * mapping.clips.Length);
            index = Mathf.Clamp(index, 0, mapping.clips.Length - 1);
            var player = Instantiate(notePlayerPrefab, obj.point, Quaternion.identity);
            player.name = $"Note {mapping.clips[index].name}";
            var audio = player.GetComponent<AudioSource>();
            audio.clip = mapping.clips[index];
            audio.Play();
        }
    }
}
