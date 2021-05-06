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

using System;
using System.IO;
using System.Text;

namespace WaveLoader
{
    /// <summary>
    /// The encoding format of a wave file
    /// </summary>
    public enum WaveFileFormat
    {
        Unknown = 0, PCM = 1, ADPCM = 2, IEEEFloat = 3, MPEG = 5, ALaw = 6, MuLaw = 7, Extensible = 0xFFFE
    }

    /// <summary>
    /// A full representation of a wave file
    /// </summary>
    /// <remarks>
    /// <para>Basic implementation; allows you to load a WAV file from disk or byte array and look at it.</para>
    /// <para>Really just enough here to support loading files for Unity, though it could easily be extended.</para>
    /// </remarks>
    public class WaveFile
    {
        /// <summary>
        /// The format of this wave file
        /// </summary>
        public WaveFileFormat Format { get; private set; }

        /// <summary>
        /// The sub-format of this wave file (if it is an extensible wave file)
        /// </summary>
        public WaveFileFormat SubFormat { get; private set; }

        /// <summary>
        /// The number of channels in this wave file
        /// </summary>
        public int Channels { get; private set; }

        /// <summary>
        /// The sample rate of this wave file
        /// </summary>
        public int SampleRate { get; private set; }

        /// <summary>
        /// The number of bits per sample of this wave file
        /// </summary>
        public int BitsPerSample { get; private set; }

        /// <summary>
        /// Whether this wave file has signed or unsigned data
        /// </summary>
        public bool Signed { get; private set; }
        
        /// <summary>
        /// The raw wave data of this wave file
        /// </summary>
        /// <remarks>
        /// Caution: this references the actual backing field
        /// </remarks>
        public ArraySegment<byte> Data { get; private set; }

        public int Samples { get => Data.Count / (BitsPerSample / 8 * Channels); }

        private WaveFile()
        {

        }

        /// <summary>
        /// Loads a WaveFile from disk
        /// </summary>
        public static WaveFile Load(string path)
        {
            return Load(File.ReadAllBytes(path), false);
        }

        /// <summary>
        /// Loads a WaveFile from a byte array
        /// </summary>
        public static WaveFile Load(byte[] data)
        {
            return Load(data, true);
        }

        /// <summary>
        /// Loads a WaveFile from a byte array
        /// </summary>
        public static WaveFile Load(byte[] data, bool copyData)
        {
            WaveFile waveFile = new WaveFile();

            //handle the first WAVE/RIFF chunk
            string fileChunkID = Encoding.ASCII.GetString(data, 0, 4);
            int fileSize = (int)BitConverter.ToUInt32(data, 4) + 8; //this size does not include the first chunk ID or size bytes
            string fileFormatID = Encoding.ASCII.GetString(data, 8, 4);

            if (!(fileChunkID.Equals("RIFF", StringComparison.Ordinal) && fileFormatID.Equals("WAVE", StringComparison.Ordinal)))
                throw new FormatException();

            //start reading other chunks
            for(int i = 12; i < fileSize;)
            {
                //read the chunk header
                string currentChunkID = Encoding.ASCII.GetString(data, i, 4);
                int currentChunkLength = (int)BitConverter.ToUInt32(data, i + 4);

                //parse chunks we know, ignore ones we don't
                if(currentChunkID.Equals("fmt ", StringComparison.Ordinal))
                {
                    //fmt chunk describes the format of our wave file
                    waveFile.Format = (WaveFileFormat)BitConverter.ToUInt16(data, i + 8);
                    waveFile.Channels = (int)BitConverter.ToUInt16(data, i + 10);
                    waveFile.SampleRate = (int)BitConverter.ToUInt32(data, i + 12);
                    waveFile.BitsPerSample = (int)BitConverter.ToUInt16(data, i + 22);

                    //handle "extensible" format codes
                    if(waveFile.Format == WaveFileFormat.Extensible && currentChunkLength > 16)
                    {
                        int extensionChunkSize = (int)BitConverter.ToUInt16(data, i + 24);
                        waveFile.BitsPerSample = (int)BitConverter.ToUInt16(data, i + 26);
                        //we are lazy and only read the first two bytes of the extensible format GUID
                        waveFile.SubFormat = (WaveFileFormat)BitConverter.ToUInt16(data, i + 32);
                    }
                }
                else if(currentChunkID.Equals("data", StringComparison.Ordinal))
                {
                    //data chunk holds the actual wave data

                    if(copyData) //copying is slower and wastes memory but is safer
                    {
                        byte[] newData = new byte[currentChunkLength];
                        Array.Copy(data, i + 8, newData, 0, currentChunkLength); 
                        waveFile.Data = new ArraySegment<byte>(newData);
                    }
                    else
                    {
                        waveFile.Data = new ArraySegment<byte>(data, i + 8, currentChunkLength);
                    }

                }
                else
                {
                    //ignore chunks we don't understand
                    //Console.WriteLine("found unidentified chunk " + currentChunkID);
                }

                i += (currentChunkLength + 8); //don't forget the chunk ID and size bytes!
            }

            //assuming standards compliance: PCM 16/24/32 bit is signed, 8-bit is not, float is always signed as well
            //note that it *is* possible to create WAV files that break from the standard
            waveFile.Signed = waveFile.BitsPerSample > 8;

            return waveFile;
        }

        /// <summary>
        /// Gets the audio data as an array of floats.
        /// </summary>
        /// <remarks>
        /// Really meant for Unity's AudioClip
        /// </remarks>
        public float[] GetDataFloat()
        {
            if (Format == WaveFileFormat.IEEEFloat || (Format == WaveFileFormat.Extensible && SubFormat == WaveFileFormat.IEEEFloat))
                return GetDataFloatFromFloat();
            else if (Format == WaveFileFormat.PCM || (Format == WaveFileFormat.Extensible && SubFormat == WaveFileFormat.PCM))
                return GetDataFloatFromPCM();
            else
                throw new NotSupportedException();
        }

        /// <summary>
        /// Gets a float array of audio data, assuming the source data is float data
        /// </summary>
        private float[] GetDataFloatFromFloat()
        {
            if (BitsPerSample != 32)
                throw new NotSupportedException(); //we don't support DP (yet)

            int stride = 4; //assume single-precision float

            float[] floatData = new float[Data.Count / stride];
            for (int i = 0; i < floatData.Length; i++)
            {
                floatData[i] = BitConverter.ToSingle(Data.Array, i * stride + Data.Offset);                
            }

            return floatData;
        }

        /// <summary>
        /// Gets a float array of audio data, assuming the source data is PCM data
        /// </summary>
        private float[] GetDataFloatFromPCM()
        {
            int stride = (BitsPerSample / 8);

            //this is hopefully faster than doing an if/then every iteration
            Func<byte[], int, float> ConvertSample;
            switch (BitsPerSample)
            {
                case 8:
                    if (Signed)
                        throw new NotSupportedException();
                    ConvertSample = (data, index) => (data[index] - 128) / 127f; //offset binary
                    break;
                case 16:
                    if (!Signed)
                        throw new NotSupportedException();
                    ConvertSample = (data, index) => BitConverter.ToInt16(data, index) / (float)short.MaxValue;
                    break;
                case 24:
                    if (!Signed)
                        throw new NotSupportedException();
                    ConvertSample = (data, index) => {
                        int sample = data[index] | data[index + 1] << 8 | (sbyte)data[index + 2] << 16;
                        return sample / (float)8388607;
                    };
                    break;
                case 32:
                    if (!Signed)
                        throw new NotSupportedException();
                    ConvertSample = (data, index) => BitConverter.ToInt32(data, index) / (float)int.MaxValue;
                    break;
                default:
                    throw new NotSupportedException();
            }

            float[] floatData = new float[Data.Count / stride];
            for (int i = 0; i < floatData.Length; i++)
            {
                floatData[i] = ConvertSample(Data.Array, i * stride + Data.Offset);
            }

            return floatData;
        }

    }
}
