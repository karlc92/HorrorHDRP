using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ResourceCache
{
    private static Dictionary<string, Object> _objectDictionary = new Dictionary<string, Object>();
    private static Dictionary<string, bool> _objectReleaseOnSceneChange = new Dictionary<string, bool>();

    //================================================================

    public static T Get<T>(string name, bool releaseOnSceneChange = true) where T : UnityEngine.Object
    {
        if (_objectDictionary.ContainsKey(name))
        {
            Object obj = null;
            if (_objectDictionary.TryGetValue(name, out obj))
            {
                return (T)obj;
            }
            else
            {
                Console.Print("[Debug] Resource Cache unable to get value for " + name);
                return (T)obj;
            }
        }
        else
        {
            var obj = Resources.Load(name);
            if (obj == null)
            {
                Console.Print("[Debug] Resource Cache failed to load " + name);
                return (T)obj;
            }
            else
            {
                _objectDictionary.Add(name, obj);
                _objectReleaseOnSceneChange.Add(name, releaseOnSceneChange);
                return (T)obj;
            }
        }
    }

    //================================================================

    public static void ReleaseObjectsOnSceneChange()
    {
        List<string> toRelease = new List<string>();
        foreach (var entry in _objectReleaseOnSceneChange)
        {
            if (entry.Value)
            {
                toRelease.Add(entry.Key);
            }
        }
        if (toRelease.Count > 0)
        {
            foreach (var name in toRelease)
            {
                _objectDictionary.Remove(name);
                _objectReleaseOnSceneChange.Remove(name);
            }
        }
    }

    //================================================================

    public static void Release()
    {
        _objectDictionary.Clear();
        _objectReleaseOnSceneChange.Clear();
    }

    //================================================================

}