using System;
using System.Diagnostics;
using System.Threading;

namespace RedBookPlayer
{
    /// <summary>
    /// Recurring timer wrapper with a high degree of accuracy
    /// </summary>
    public class HiResTimer
    {
        static readonly float tickFrequency = 1000f / Stopwatch.Frequency;

        volatile float _interval;
        volatile bool  _isRunning;

        public HiResTimer() : this(1f) {}

        public HiResTimer(float interval)
        {
            if(interval < 0f ||
               float.IsNaN(interval))
                throw new ArgumentOutOfRangeException(nameof(interval));

            _interval = interval;
        }

        public float Interval
        {
            get => _interval;
            set
            {
                if(value < 0f ||
                   float.IsNaN(value))
                    throw new ArgumentOutOfRangeException(nameof(value));

                _interval = value;
            }
        }

        public bool Enabled
        {
            get => _isRunning;
            set
            {
                if(value)
                    Start();
                else
                    Stop();
            }
        }

        public event EventHandler<HiResTimerElapsedEventArgs> Elapsed;

        public void Start()
        {
            if(_isRunning)
                return;

            _isRunning = true;
            var thread = new Thread(ExecuteTimer);
            thread.Priority = ThreadPriority.Highest;
            thread.Start();
        }

        public void Stop() => _isRunning = false;

        void ExecuteTimer()
        {
            float nextTrigger = 0f;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while(_isRunning)
            {
                nextTrigger += _interval;
                float elapsed;

                while(true)
                {
                    elapsed = ElapsedHiRes(stopwatch);
                    float diff = nextTrigger - elapsed;

                    if(diff <= 0f)
                        break;

                    if(diff < 1f)
                        Thread.SpinWait(10);
                    else if(diff < 5f)
                        Thread.SpinWait(100);
                    else if(diff < 15f)
                        Thread.Sleep(1);
                    else
                        Thread.Sleep(10);

                    if(!_isRunning)
                        return;
                }

                float delay = elapsed - nextTrigger;
                Elapsed?.Invoke(this, new HiResTimerElapsedEventArgs(delay));

                if(!(stopwatch.Elapsed.TotalHours >= 1d))
                    continue;

                stopwatch.Restart();
                nextTrigger = 0f;
            }

            stopwatch.Stop();
        }

        static float ElapsedHiRes(Stopwatch stopwatch) => stopwatch.ElapsedTicks * tickFrequency;
    }
}