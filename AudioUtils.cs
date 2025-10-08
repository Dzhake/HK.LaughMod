using System.Collections.Generic;
using System.IO;
using UnityEngine;
using WavLib;

namespace LaughMod;

public class AudioUtils
{
    public static AudioClip LoadAudioClip(string file)
    {

        FileStream stream = File.OpenRead(file);
        WavData wavData = new();
        wavData.Parse(stream);
        stream.Close();


        float[] wavSoundData = wavData.GetSamples();
        AudioClip audioClip = AudioClip.Create(Path.GetFileNameWithoutExtension(file), wavSoundData.Length / wavData.FormatChunk.NumChannels, wavData.FormatChunk.NumChannels, (int)wavData.FormatChunk.SampleRate, false);
        audioClip.SetData(wavSoundData, 0);
        
        
        return audioClip;
    }

    public static AudioClip[] LoadAudioClips(string dir)
    {
        List<AudioClip> result = new();
        foreach (string file in Directory.GetFiles(dir)) result.Add(LoadAudioClip(file));
        return result.ToArray();
    }
}