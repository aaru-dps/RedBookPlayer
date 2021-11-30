namespace RedBookPlayer.Models.Hardware.Karaoke
{
    /// <see cref="https://jbum.com/cdg_revealed.html"/>
    internal class KaraokeDisplay
    {
        /// <summary>
        /// Display data as a 2-dimensional byte array
        /// </summary>
        /// <remarks>
        // CONFLICTING INFO:
        ///
        /// In the top part of the document, it states:
        ///     In the CD+G system, 16 color graphics are displayed on a raster field which is
        ///     300 x 216 pixels in size.  The middle 294 x 204 area is within the TV's
        ///     "safe area", and that is where the graphics are displayed.  The outer border is
        ///     set to a solid color.  The colors are stored in a 16 entry color lookup table.
        ///
        /// And in the bottom part of the document around CDG_BorderPreset, it states:
        ///     Color refers to a color to clear the screen to.  The border area of the screen
        ///     should be cleared to this color.  The border area is the area contained with a
        ///     rectangle defined by (0,0,300,216) minus the interior pixels which are contained
        ///     within a rectangle defined by (6,12,294,204).
        ///
        /// With both of these in mind, does that mean that the "drawable" area is:
        ///     a) (3, 6,  297, 210) [Dimensions of 294 x 204]
        ///     b) (6, 12, 294, 204) [Dimensions of 288 x 192]
        /// </remarks>
        public byte[,] DisplayData { get; private set; }

        /// <summary>
        /// Current 8-entry color table
        /// </summary>
        /// <remarks>
        /// In practice, this should be 8 colors, probably similar to the CGA palette,
        /// including the "bright" or "dark" variant (possibly mapping from "high" to "low").
        /// </remarks>
        public byte[] ColorTable { get; private set; }

        /// <summary>
        /// Color currently defined as transparent
        /// </summary>
        public byte TransparentColor { get; private set; }

        /// <summary>
        /// Create a new, blank karaoke display
        /// </summary>
        public KaraokeDisplay()
        {
            this.DisplayData        = new byte[300,216];
            this.ColorTable         = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 };
            this.TransparentColor   = 0x00;
        }

        /// <summary>
        /// Process a subchannel packet and update the display as necessary
        /// </summary>
        /// <param name="packet">Subchannel packet data to process</param>
        public void ProcessData(SubchannelPacket packet)
        {
            if(!packet.IsCDGPacket())
                return;

            switch(packet.Instruction)
            {
                case SubchannelInstruction.MemoryPreset:
                    var memoryPreset = new MemPreset(packet.Data);
                    SetScreenColor(memoryPreset);
                    break;
                case SubchannelInstruction.BorderPreset:
                    var borderPreset = new BorderPreset(packet.Data);
                    SetBorderColor(borderPreset);
                    break;
                case SubchannelInstruction.TileBlockNormal:
                    var tileBlockNormal = new TileBlock(packet.Data);
                    LoadTileBlock(tileBlockNormal, false);
                    break;
                case SubchannelInstruction.ScrollPreset:
                    var scrollPreset = new Scroll(packet.Data);
                    break;
                case SubchannelInstruction.ScrollCopy:
                    var scrollCopy = new Scroll(packet.Data);
                    break;
                case SubchannelInstruction.DefineTransparentColor:
                    var transparentColor = new BorderPreset(packet.Data);
                    this.TransparentColor = transparentColor.Color;
                    break;
                case SubchannelInstruction.LoadColorTableLower:
                    var loadColorTableLower = new LoadCLUT(packet.Data);
                    break;
                case SubchannelInstruction.LoadColorTableUpper:
                    var loadColorTableUpper = new LoadCLUT(packet.Data);
                    break;
                case SubchannelInstruction.TileBlockXOR:
                    var tileBlockXor = new TileBlock(packet.Data);
                    LoadTileBlock(tileBlockXor, true);
                    break;
            };
        }
    
        /// <summary>
        /// Set the screen to a particular color
        /// </summary>
        /// <param name="memPreset">MemPreset with the new data</param>
        /// <param name="consistentData">True if all subchannel data is present, false otherwise</param>
        /// <remarks>
        /// It is unclear if this is supposed to set the entire screen to the same color or
        /// if it is only setting the interior pixels. To err on the side of caution, this sets
        /// the viewable area only.
        ///
        /// The area that is considered the border is unclear. Please see the remarks
        /// on <see cref="DisplayData"/> for more details.
        /// </remarks>
        private void SetScreenColor(MemPreset memPreset, bool consistentData = false)
        {
            if(memPreset == null)
                return;

            // Skip in a consistent data case
            if(consistentData && memPreset.Repeat > 0)
                return;

            for(int x = 3; x < 297; x++)
            for(int y = 6; y < 210; y++)
            {
                this.DisplayData[x,y] = memPreset.Color;
            }
        }

        /// <summary>
        /// Set the border to a particular color
        /// </summary>
        /// <param name="borderPreset">BorderPreset with the new data</param>
        /// <remarks>
        /// The area that is considered the border is unclear. Please see the remarks
        /// on <see cref="DisplayData"/> for more details.
        /// </remarks>
        private void SetBorderColor(BorderPreset borderPreset)
        {
            if(borderPreset == null)
                return;

            for(int x = 0; x < 3; x++)
            for(int y = 0; y < 216; y++)
            {
                this.DisplayData[x,y] = borderPreset.Color;
            }

            for(int x = 297; x < 300; x++)
            for(int y = 0; y < 216; y++)
            {
                this.DisplayData[x,y] = borderPreset.Color;
            }

            for(int x = 0; x < 300; x++)
            for(int y = 0; y < 6; y++)
            {
                this.DisplayData[x,y] = borderPreset.Color;
            }

            for(int x = 0; x < 300; x++)
            for(int y = 210; y < 216; y++)
            {
                this.DisplayData[x,y] = borderPreset.Color;
            }
        }
    
        /// <summary>
        /// Load a block of pixels with a certain pattern
        /// </summary>
        /// <param name="borderPreset">BorderPreset with the new data</param>
        /// <param name="xor">
        /// If true, the color values are combined with the color values
        /// that are already onscreen using the XOR operator
        /// </param>
        private void LoadTileBlock(TileBlock tileBlock, bool xor)
        {
            if(tileBlock == null)
                return;

            // Extract out the "bitmap" into a byte pattern
            byte[,] pattern = new byte[12,6];
            for(int i = 0; i < tileBlock.TilePixels.Length; i++)
            {
                byte b = tileBlock.TilePixels[i];
                pattern[i,0] = (byte)(b & (1 << 0));
                pattern[i,1] = (byte)(b & (1 << 1));
                pattern[i,2] = (byte)(b & (1 << 2));
                pattern[i,3] = (byte)(b & (1 << 3));
                pattern[i,4] = (byte)(b & (1 << 4));
                pattern[i,5] = (byte)(b & (1 << 5));
            }

            // Now load the bitmap starting in the correct place
            for(int x = 0; x < 12; x++)
            for(int y = 0; y < 6; y++)
            {
                int adjustedX = x + tileBlock.Column;
                int adjustedY = y + tileBlock.Row;
                int colorIndex = pattern[x,y] == 0 ? tileBlock.Color0 : tileBlock.Color1;

                if(xor)
                    this.DisplayData[adjustedX, adjustedY] = (byte)(this.ColorTable[colorIndex] ^ this.DisplayData[adjustedX, adjustedY]);
                else
                    this.DisplayData[adjustedX, adjustedY] = this.ColorTable[colorIndex];
            }
        }
    }
}