/*
Copyright(c) 2019 Chris Leclair https://www.xcvgsystems.com

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System.IO;
using UnityEngine;

namespace WaveLoader
{
    /// <summary>
    /// Wave loader extensions and convenience methods for Unity
    /// </summary>
    /// <remarks>
    /// Separated out so you can use WaveFile for a non-Unity application.
    /// </remarks>
    public static class WaveLoader
    {

        /// <summary>
        /// Creates an Audio Clip from bytes containing wav file data
        /// </summary>
        public static AudioClip LoadWaveToAudioClip(byte[] data)
        {
            return WaveFile.Load(data, false).ToAudioClip("LoadedWave");
        }

        /// <summary>
        /// Creates an Audio Clip from bytes containing wav file data
        /// </summary>
        public static AudioClip LoadWaveToAudioClip(byte[] data, string name)
        {
            return WaveFile.Load(data, false).ToAudioClip(name);
        }

        /// <summary>
        /// Creates an Audio Clip from a wav file on disk
        /// </summary>
        public static AudioClip LoadWaveToAudioClip(string path)
        {
            return WaveFile.Load(path).ToAudioClip($"LoadedWave ({Path.GetFileName(path)})");
        }

        /// <summary>
        /// Creates an Audio Clip from a wav file on disk
        /// </summary>
        public static AudioClip LoadWaveToAudioClip(string path, string name)
        {
            return WaveFile.Load(path).ToAudioClip(name);
        }

        /// <summary>
        /// Creates an Audio Clip from a wav file in a binary asset
        /// </summary>
        public static AudioClip LoadWaveToAudioClip(TextAsset binaryAsset)
        {
            return WaveFile.Load(binaryAsset.bytes, false).ToAudioClip($"LoadedWave ({binaryAsset.name})");
        }

        /// <summary>
        /// Creates an Audio Clip from a wav file in a binary asset
        /// </summary>
        public static AudioClip LoadWaveToAudioClip(TextAsset binaryAsset, string name)
        {
            return WaveFile.Load(binaryAsset.bytes, false).ToAudioClip(name);
        }

        /// <summary>
        /// Creates an Audio Clip from this wave file
        /// </summary>
        public static AudioClip ToAudioClip(this WaveFile waveFile)
        {
            return waveFile.ToAudioClip("LoadedWave");
        }

        /// <summary>
        /// Creates an Audio Clip from this wave file
        /// </summary>
        public static AudioClip ToAudioClip(this WaveFile waveFile, string name)
        {
            AudioClip audioClip = AudioClip.Create(name, waveFile.Samples, waveFile.Channels, waveFile.SampleRate, false);
            audioClip.SetData(waveFile.GetDataFloat(), 0);
            return audioClip;
        }
    }
}