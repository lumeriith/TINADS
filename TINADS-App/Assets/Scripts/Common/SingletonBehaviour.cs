using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingletonBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T instance
    {
        get
        {
            if (_instance == null) _instance = FindObjectOfType<T>();
            return _instance;
        }
    }
    private static T _instance;
}
