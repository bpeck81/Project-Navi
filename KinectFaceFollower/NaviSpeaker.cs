using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Synthesis;
using Microsoft.Speech.AudioFormat;
using Microsoft.Kinect;
using Microsoft.Kinect.Input;
using Microsoft.Speech.Recognition;

namespace KinectFaceFollower
{
    class NaviSpeaker
    {
        SpeechSynthesizer synth;
        KinectAudioStream convertStream;
        SpeechRecognitionEngine sre;
        private Dictionary<string, string> speechOptionList;
        bool speechIsRunning;

        public NaviSpeaker(KinectSensor sensor)
        {
            speechIsRunning = false;
            speechOptionList = new Dictionary<string, string>();
            synth = new SpeechSynthesizer();
            synth.Volume = 100;
            synth.SelectVoiceByHints(VoiceGender.Male);
            synth.Rate = 1;
            if(sensor != null){
                IReadOnlyList<AudioBeam> audioBeamList = sensor.AudioSource.AudioBeams;
                var audioStream = audioBeamList[0].OpenInputStream();
                convertStream = new KinectAudioStream(audioStream);
                var gb = new GrammarBuilder();
                sre = new SpeechRecognitionEngine();
                var phrases = new Choices();
                speechOptionList["LIGHTS"] = "A please would be nice";
                speechOptionList["OPEN DOOR"] = "Dont let it hit you on the way out";
                speechOptionList["HI"] = "What do you need this time";



                phrases.Add(new SemanticResultValue("Turn on the lights", "LIGHTS"));
                phrases.Add(new SemanticResultValue("Hey Navi", "HI"));
                phrases.Add(new SemanticResultValue("Open the door", "OPEN DOOR"));



                gb.Append(phrases);
                var g = new Grammar(gb);
                sre.LoadGrammar(g);
                sre.SpeechRecognized += Sre_SpeechRecognized; ;
                convertStream.SpeechActive = true;
                this.sre.SetInputToAudioStream(
                this.convertStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                sre.RecognizeAsync(RecognizeMode.Multiple);
            }
        }

        //not being used
        private void Sre_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            var key = e.Result.Semantics.Value.ToString();
            if (e.Result.Confidence > .8)
            {
                lockSre();
                synth.Speak(speechOptionList[key]);
                switch (key)
                {
                    case "LIGHTS":
                        break;
                    case "OPEN DOOR":
                        break;
                    case "HI":
                        break;
                }
            }
        }
        private void lockSre()
        {
            lock (sre)
            {
                if (speechIsRunning)
                {
                    return;
                }
                else
                {
                    speechIsRunning = true;
                }
            }

        }
    }
}
