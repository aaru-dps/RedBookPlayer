using System;

namespace RedBookPlayer.Models.Hardware.Karaoke
{
    /// <see cref="https://jbum.com/cdg_revealed.html"/>
    internal class LoadCLUT
    {
        // AND with 0x3F3F to clear P and Q channel
        public short[] ColorSpec { get; private set; } = new short[8];

        /// <summary>
        /// Interpret subchannel packet data as Load Color Lookup Table
        /// </summary>
        public LoadCLUT(byte[] bytes)
        {
            if(bytes == null || bytes.Length != 16)
                return;

            for(int i = 0; i < 8; i++)
                this.ColorSpec[i] = (short)(BitConverter.ToInt16(bytes, 2 * i) & 0x3F3F);
        }
    }
}