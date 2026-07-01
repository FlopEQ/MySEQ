using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using Structures;

namespace myseq
{
    internal static class ConnectionStatusSound
    {
        private const int SampleRate = 44100;
        private const short BitsPerSample = 16;
        private const short Channels = 1;
        private const double Volume = 0.16;

        public static void PlayConnected() => PlayToneSequence(new[] { 523.25, 659.25 }, 115);

        public static void PlayDisconnected() => PlayToneSequence(new[] { 440.00, 329.63 }, 105);

        private static void PlayToneSequence(double[] frequencies, int toneMs)
        {
            Task.Run(() =>
            {
                try
                {
                    using (var stream = BuildWaveStream(frequencies, toneMs))
                    using (var player = new SoundPlayer(stream))
                    {
                        player.PlaySync();
                    }
                }
                catch (Exception ex)
                {
                    LogLib.WriteLine("Error playing connection status sound: ", ex);
                }
            });
        }

        private static MemoryStream BuildWaveStream(double[] frequencies, int toneMs)
        {
            var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true))
            {
                int samplesPerTone = SampleRate * toneMs / 1000;
                int totalSamples = samplesPerTone * frequencies.Length;
                int dataLength = totalSamples * Channels * BitsPerSample / 8;

                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataLength);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write(Channels);
                writer.Write(SampleRate);
                writer.Write(SampleRate * Channels * BitsPerSample / 8);
                writer.Write((short)(Channels * BitsPerSample / 8));
                writer.Write(BitsPerSample);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(dataLength);

                foreach (double frequency in frequencies)
                {
                    WriteTone(writer, frequency, samplesPerTone);
                }
            }

            stream.Position = 0;
            return stream;
        }

        private static void WriteTone(BinaryWriter writer, double frequency, int sampleCount)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                double position = i / (double)sampleCount;
                double envelope = Math.Sin(Math.PI * position);
                double sample = Math.Sin(2.0 * Math.PI * frequency * i / SampleRate) * Volume * envelope;
                writer.Write((short)(sample * short.MaxValue));
            }
        }
    }
}
