using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effect : MonoBehaviour
{
    public void Play()
    {
        if (TryGetComponent<AudioSource>(out var audio))
            audio.Play();

        if (TryGetComponent<ParticleSystem>(out var ps))
            ps.Play();
    }
    
    public Effect PlayNew()
    {
        var newEff = Instantiate(this, transform.position, transform.rotation);
        newEff.gameObject.AddComponent<EffectAutoDestroy>();
        newEff.Play();
        return newEff;
    }
    
    public Effect PlayNew(Vector3 pos, Quaternion rot)
    {
        var newEff = Instantiate(this, pos, rot);
        newEff.gameObject.AddComponent<EffectAutoDestroy>();
        newEff.Play();
        return newEff;
    }
}