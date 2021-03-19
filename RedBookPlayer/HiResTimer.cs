using System;
using System.Diagnostics;
using System.Threading;

namespace RedBookPlayer {
    public class HiResTimer
    {
        private static readonly float tickFrequency = 1000f / Stopwatch.Frequency;

        public event EventHandler<HiResTimerElapsedEventArgs> Elapsed;

        private volatile float interval;
        private volatile bool isRunning;

        public HiResTimer() : this(1f)
        {
        }

        public HiResTimer(float interval)
        {
            if (interval < 0f || Single.IsNaN(interval))
                throw new ArgumentOutOfRangeException(nameof(interval));
            this.interval = interval;
        }

        public float Interval
        {
            get { return interval; }
            set
            {
                if (value < 0f || Single.IsNaN(value))
                    throw new ArgumentOutOfRangeException(nameof(value));
                interval = value;
            }
        }

        public bool Enabled
        {
            set
            {
                if (value)
                    Start();
                else
                    Stop();
            }
            get { return isRunning; }
        }

        public void Start()
        {
            if (isRunning)
                return;

            isRunning = true;
            Thread thread = new Thread(ExecuteTimer);
            thread.Priority = ThreadPriority.Highest;
            thread.Start();
        }

        public void Stop()
        {
            isRunning = false;
        }

        private void ExecuteTimer()
        {
            float nextTrigger = 0f;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            while (isRunning)
            {
                nextTrigger += interval;
                float elapsed;

                while (true)
                {
                    elapsed = ElapsedHiRes(stopwatch);
                    float diff = nextTrigger - elapsed;
                    if (diff <= 0f)
                        break;

                    if (diff < 1f)
                        Thread.SpinWait(10);
                    else if (diff < 5f)
                        Thread.SpinWait(100);
                    else if (diff < 15f)
                        Thread.Sleep(1);
                    else
                        Thread.Sleep(10);

                    if (!isRunning)
                        return;
                }


                float delay = elapsed - nextTrigger;
                Elapsed?.Invoke(this, new HiResTimerElapsedEventArgs(delay));

                if (stopwatch.Elapsed.TotalHours >= 1d)
                {
                    stopwatch.Restart();
                    nextTrigger = 0f;
                }
            }

            stopwatch.Stop();
        }

        private static float ElapsedHiRes(Stopwatch stopwatch)
        {
            return stopwatch.ElapsedTicks * tickFrequency;
        }
    }

    public class HiResTimerElapsedEventArgs : EventArgs
    {
        public float Delay { get; }

        internal HiResTimerElapsedEventArgs(float delay)
        {
            Delay = delay;
        }
    }
}