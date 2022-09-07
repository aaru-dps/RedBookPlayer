using System;

namespace RedBookPlayer.Models.Hardware.Karaoke
{
    /// <see cref="https://jbum.com/cdg_revealed.html"/>
    internal class MemPreset
    {
        // Only lower 4 bits are used, mask with 0x0F
        public byte Color { get; private set; }

        // Only lower 4 bits are used, mask with 0x0F
        public byte Repeat { get; private set; }

        public byte[] Filler { get; private set; } = new byte[14];

        /// <summary>
        /// Interpret subchannel packet data as Memory Preset
        /// </summary>
        public MemPreset(byte[] bytes)
        {
            if(bytes == null || bytes.Length != 16)
                return;

            this.Color  = (byte)(bytes[0] & 0x0F);
            this.Repeat = (byte)(bytes[1] & 0x0F);

            Array.Copy(bytes, 2,  this.Filler, 0, 14);
        }
    }
}