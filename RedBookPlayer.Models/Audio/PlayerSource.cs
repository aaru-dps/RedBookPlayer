using System;
using CSCore;
using WaveFormat = CSCore.WaveFormat;

namespace RedBookPlayer.Models.Audio
{
    public class PlayerSource : IWaveSource
    {
        public delegate int ReadFunction(byte[] buffer, int offset, int count);

        readonly ReadFunction _read;

        public bool Run = true;

        public PlayerSource(ReadFunction read) => _read = read;

        public WaveFormat WaveFormat => new WaveFormat();
        bool IAudioSource.CanSeek    => throw new NotImplementedException();

        public long Position
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public long Length => throw new NotImplementedException();

        public int Read(byte[] buffer, int offset, int count)
        {
            if(Run)
                return _read(buffer, offset, count);

            Array.Clear(buffer, offset, count);

            return count;
        }

        public void Dispose() {}

        public void Start() => Run = true;

        public void Stop() => Run = false;
    }
}