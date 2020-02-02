using System;

namespace PoopAudio
{
    public class Tone
    {
        public int[] freq, len;

        public Tone(int[] frequency, int[] length)
        {
            freq = frequency;
            len = length;
        }
    }
    public class Audio
    {
        static float playbackrate = 0.75f;

        static void pause(int ms)
        {
            float res = ms * (playbackrate);
            System.Threading.Thread.Sleep((int)res);
        }
        static int playb(int delay)
        {
            return (int)Math.Floor(delay * playbackrate);
        }
        public static void play(Tone tune)
        {
            for (int i = 0; i < tune.freq.Length; i++)
            {
                Console.Beep(tune.freq[i], playb(tune.len[i]));
                pause(70);
            }
        }

        public Audio()
        {
            Utils.log("Audio Player created");
        }

    }
}