using System.Collections.Generic;
using UnityEngine;

public static class MusicLibrary
{
    private const string Prefix = "BallBrawl_";

    private static Dictionary<string, List<AudioClip>> tracks;

    public static AudioClip RandomVersion(string track)
    {
        if (tracks == null) LoadAll();

        List<AudioClip> versions;
        if (tracks.TryGetValue(track, out versions) && versions.Count > 0)
        {
            return versions[Random.Range(0, versions.Count)];
        }

        Debug.LogWarning("Music: no clip named " + Prefix + track + " in Resources/Music");
        return null;
    }

    private static void LoadAll()
    {
        tracks = new Dictionary<string, List<AudioClip>>();
        foreach (AudioClip clip in Resources.LoadAll<AudioClip>("Music"))
        {
            string name = TrackName(clip.name);
            if (!tracks.ContainsKey(name)) tracks[name] = new List<AudioClip>();
            tracks[name].Add(clip);
        }
    }

    private static string TrackName(string fileName)
    {
        string name = fileName;
        if (name.StartsWith(Prefix)) name = name.Substring(Prefix.Length);

        int underscore = name.LastIndexOf('_');
        int number;
        if (underscore > 0 && int.TryParse(name.Substring(underscore + 1), out number))
        {
            name = name.Substring(0, underscore);
        }
        return name;
    }
}
