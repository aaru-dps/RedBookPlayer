using System;

namespace RedBookPlayer.Models.Hardware.Karaoke
{
    /// <see cref="https://jbum.com/cdg_revealed.html"/>
    internal class BorderPreset
    {
        // Only lower 4 bits are used, mask with 0x0F
        public byte Color { get; private set; }

        public byte[] Filler { get; private set; } = new byte[15];

        /// <summary>
        /// Interpret subchannel packet data as Border Preset
        /// </summary>
        public BorderPreset(byte[] bytes)
        {
            if(bytes == null || bytes.Length != 16)
                return;

            this.Color  = (byte)(bytes[0] & 0x0F);

            Array.Copy(bytes, 1,  this.Filler, 0, 15);
        }
    }
}