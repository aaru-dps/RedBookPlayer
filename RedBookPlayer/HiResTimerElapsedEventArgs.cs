using System;

namespace RedBookPlayer
{
    public class HiResTimerElapsedEventArgs : EventArgs
    {
        internal HiResTimerElapsedEventArgs(float delay) => Delay = delay;

        public float Delay { get; }
    }
}