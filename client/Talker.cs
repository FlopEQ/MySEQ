using myseq.Properties;
using SpeechLib;
using System.Collections.Generic;

namespace Structures
{
    public class Talker
    {
        public string SpeakingText { get; set; }
        private readonly SpVoice speech;

        public Talker(string text)
        {
            SpeakingText = text;
            speech = new SpVoice();
            SelectConfiguredVoice();
        }

        public void SpeakText()
        {
            speech.Speak(SpeakingText, SpeechVoiceSpeakFlags.SVSFDefault);
        }

        public static List<string> GetInstalledVoices()
        {
            var voices = new List<string>();
            var spVoice = new SpVoice();

            foreach (ISpeechObjectToken voice in spVoice.GetVoices())
            {
                voices.Add(voice.GetDescription());
            }

            return voices;
        }

        private void SelectConfiguredVoice()
        {
            if (string.IsNullOrWhiteSpace(Settings.Default.AlertVoiceName))
            {
                return;
            }

            foreach (ISpeechObjectToken voice in speech.GetVoices())
            {
                if (voice.GetDescription() == Settings.Default.AlertVoiceName)
                {
                    speech.Voice = (SpObjectToken)voice;
                    return;
                }
            }
        }
    }
}
