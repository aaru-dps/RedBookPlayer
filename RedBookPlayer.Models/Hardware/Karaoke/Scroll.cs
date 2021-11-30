using System;

namespace RedBookPlayer.Models.Hardware.Karaoke
{
    /// <see cref="https://jbum.com/cdg_revealed.html"/>
    internal class Scroll
    {
        // Only lower 4 bits are used, mask with 0x0F
        public byte Color { get; private set; }

        // Only lower 6 bits are used, mask with 0x3F
        public byte HScroll { get; private set; }

        // Only lower 6 bits are used, mask with 0x3F
        public byte VScroll { get; private set; }

        public byte[] Filler { get; private set; } = new byte[13];

        /// <summary>
        /// Interpret subchannel packet data as Scroll
        /// </summary>
        public Scroll(byte[] bytes)
        {
            if(bytes == null || bytes.Length != 16)
                return;

            this.Color      = (byte)(bytes[0] & 0x0F);
            this.HScroll    = (byte)(bytes[1] & 0x3F);
            this.VScroll    = (byte)(bytes[2] & 0x3F);

            Array.Copy(bytes, 3,  this.Filler, 0, 13);
        }
    }
}