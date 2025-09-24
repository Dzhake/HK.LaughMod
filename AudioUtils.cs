using System.IO;
using UnityEngine;
using WavLib;

namespace LaughMod;

public class AudioUtils
{
    public static AudioClip LoadAudioClip(string file)
    {
        string filename = Path.GetFileNameWithoutExtension(file);

        FileStream stream = File.OpenRead(file);
        WavData wavData = new();
        wavData.Parse(stream);
        stream.Close();


        float[] wavSoundData = wavData.GetSamples();
        AudioClip audioClip = AudioClip.Create(filename, wavSoundData.Length / wavData.FormatChunk.NumChannels, wavData.FormatChunk.NumChannels, (int)wavData.FormatChunk.SampleRate, false);
        audioClip.SetData(wavSoundData, 0);
        
        
        return audioClip;
    }
}