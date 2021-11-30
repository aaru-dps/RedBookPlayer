namespace RedBookPlayer.Models.Hardware.Karaoke
{
    /// <see cref="https://jbum.com/cdg_revealed.html"/>
    internal class TileBlock
    {
        // Only lower 4 bits are used, mask with 0x0F
        public byte Color0 { get; private set; }

        // Only lower 4 bits are used, mask with 0x0F
        public byte Color1 { get; private set; }

        // Only lower 5 bits are used, mask with 0x1F
        public byte Row { get; private set; }

        // Only lower 6 bits are used, mask with 0x3F
        public byte Column { get; private set; }

        // Only lower 6 bits of each byte are used
        public byte[] TilePixels { get; private set; } = new byte[12];

        /// <summary>
        /// Interpret subchannel packet data as Tile Block
        /// </summary>
        public TileBlock(byte[] bytes)
        {
            if(bytes == null || bytes.Length != 16)
                return;

            this.Color0 = (byte)(bytes[0] & 0x0F);
            this.Color1 = (byte)(bytes[1] & 0x0F);
            this.Row    = (byte)(bytes[2] & 0x1F);
            this.Column = (byte)(bytes[3] & 0x3F);

            for(int i = 0; i < 12; i++)
            {
                this.TilePixels[i] = (byte)(bytes[4 + i] & 0x3F);
            }
        }
    }
}