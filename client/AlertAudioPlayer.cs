using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace myseq
{
    internal static class AlertAudioPlayer
    {
        public const string FileDialogFilter = "Audio files (*.wav;*.mp3;*.wma;*.aac;*.m4a)|*.wav;*.mp3;*.wma;*.aac;*.m4a|All files (*.*)|*.*";

        private static readonly object SyncRoot = new object();
        private static int nextAliasId;

        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        private static extern int mciSendString(string command, StringBuilder returnValue, int returnLength, IntPtr callback);

        public static void Play(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return;
            }

            string alias;
            lock (SyncRoot)
            {
                alias = $"myseq_alert_{++nextAliasId}";
            }

            if (SendCommand($"open \"{filePath}\" alias {alias}") != 0)
            {
                return;
            }

            SendCommand($"play {alias} from 0");
            CloseWhenFinished(alias);
        }

        private static async void CloseWhenFinished(string alias)
        {
            int length = GetLength(alias);
            int delay = length > 0 ? length + 1000 : 30000;

            await Task.Delay(delay).ConfigureAwait(false);
            SendCommand($"close {alias}");
        }

        private static int GetLength(string alias)
        {
            var buffer = new StringBuilder(32);
            int result = mciSendString($"status {alias} length", buffer, buffer.Capacity, IntPtr.Zero);

            return result == 0 && int.TryParse(buffer.ToString(), out int length) ? length : 0;
        }

        private static int SendCommand(string command) => mciSendString(command, null, 0, IntPtr.Zero);
    }
}
