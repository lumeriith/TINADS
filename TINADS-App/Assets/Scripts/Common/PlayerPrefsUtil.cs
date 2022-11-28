using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerPrefsUtil
{
    #region Helper Functions
    
        private static string GetKeyForVector3X(string key)
        {
            return key + "_x";
        }
        
        private static string GetKeyForVector3Y(string key)
        {
            return key + "_y";
        }
        
        private static string GetKeyForVector3Z(string key)
        {
            return key + "_z";
        }
        
        private static string GetKeyForQuaternionX(string key)
        {
            return key + "_qx";
        }
        
        private static string GetKeyForQuaternionY(string key)
        {
            return key + "_qy";
        }
        
        private static string GetKeyForQuaternionZ(string key)
        {
            return key + "_qz";
        }
        
        private static string GetKeyForQuaternionW(string key)
        {
            return key + "_qw";
        }
    
    #endregion

    
    public static Vector3 GetVector3(string key, Vector3 defaultValue = default)
    {
        if (!PlayerPrefs.HasKey(GetKeyForVector3X(key))) return defaultValue;
        return new Vector3(
            PlayerPrefs.GetFloat(GetKeyForVector3X(key)), 
            PlayerPrefs.GetFloat(GetKeyForVector3Y(key)),
            PlayerPrefs.GetFloat(GetKeyForVector3Z(key)));
    }

    public static void SetVector3(string key, Vector3 value)
    {
        PlayerPrefs.SetFloat(GetKeyForVector3X(key), value.x);
        PlayerPrefs.SetFloat(GetKeyForVector3Y(key), value.y);
        PlayerPrefs.SetFloat(GetKeyForVector3Z(key), value.z);
    }

    public static Quaternion GetQuaternion(string key, Quaternion defaultValue = default)
    {
        if (!PlayerPrefs.HasKey(GetKeyForQuaternionX(key))) return defaultValue;
        return new Quaternion(
            PlayerPrefs.GetFloat(GetKeyForQuaternionX(key)),
            PlayerPrefs.GetFloat(GetKeyForQuaternionY(key)),
            PlayerPrefs.GetFloat(GetKeyForQuaternionZ(key)),
            PlayerPrefs.GetFloat(GetKeyForQuaternionW(key)));
    }
    
    public static void SetQuaternion(string key, Quaternion value)
    {
        PlayerPrefs.SetFloat(GetKeyForQuaternionX(key), value.x);
        PlayerPrefs.SetFloat(GetKeyForQuaternionY(key), value.y);
        PlayerPrefs.SetFloat(GetKeyForQuaternionZ(key), value.z);
        PlayerPrefs.SetFloat(GetKeyForQuaternionW(key), value.w);
    }
}
