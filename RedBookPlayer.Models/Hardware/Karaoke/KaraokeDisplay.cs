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
        /// The current interpretation of this may be incorrect, as the internal color table
        /// may actually include all 16 RGB values immediately.
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
                    ScrollDisplay(scrollPreset, false);
                    break;
                case SubchannelInstruction.ScrollCopy:
                    var scrollCopy = new Scroll(packet.Data);
                    ScrollDisplay(scrollCopy, true);
                    break;
                case SubchannelInstruction.DefineTransparentColor:
                    var transparentColor = new BorderPreset(packet.Data);
                    this.TransparentColor = transparentColor.Color;
                    break;
                case SubchannelInstruction.LoadColorTableLower:
                    var loadColorTableLower = new LoadCLUT(packet.Data);
                    // TODO: Load color table data
                    break;
                case SubchannelInstruction.LoadColorTableUpper:
                    var loadColorTableUpper = new LoadCLUT(packet.Data);
                    // TODO: Load color table data
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
        /// <param name="tileBlock">TileBlock with the pattern data</param>
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

        /// <summary>
        /// Scroll the display according to the instruction
        /// </summary>
        /// <param name="scroll">Scroll with the new data</param>
        /// <param name="copy">True if data wraps around on scroll, false if filled by a solid color</param>
        /// <remarks>
        /// Based on the documentation, there's a bit of ambiguity how the Offset fields are used.
        /// The current best understanding is that the offset can be combined with any scroll command
        /// to add (or subtract) between 0 and 5 pixels in a particular axis.
        /// TODO: Offsets are not currently implemented in the code below
        /// </remarks>
        private void ScrollDisplay(Scroll scroll, bool copy)
        {
            if(scroll == null)
                return;

            // If we're scrolling horizontally
            if(scroll.HScrollCommand == ScrollCommand.Positive)
            {
                for(int y = 0; y < 216; y++)
                {
                    byte[] overflow = new byte[6];
                    for(int x = 299; x >= 0; x--)
                    {
                        if(x + 6 >= 300)
                            overflow[(x + 6) % 300] = this.DisplayData[x,y];
                        else
                            this.DisplayData[x + 6, y] = this.DisplayData[x,y];
                    }

                    // Fill in the now-empty pixels
                    this.DisplayData[0,y] = copy ? overflow[0] : this.ColorTable[scroll.Color];
                    this.DisplayData[1,y] = copy ? overflow[1] : this.ColorTable[scroll.Color];
                    this.DisplayData[2,y] = copy ? overflow[2] : this.ColorTable[scroll.Color];
                    this.DisplayData[3,y] = copy ? overflow[3] : this.ColorTable[scroll.Color];
                    this.DisplayData[4,y] = copy ? overflow[4] : this.ColorTable[scroll.Color];
                    this.DisplayData[5,y] = copy ? overflow[5] : this.ColorTable[scroll.Color];
                }
            }
            else if(scroll.HScrollCommand == ScrollCommand.Negative)
            {
                for(int y = 0; y < 216; y++)
                {
                    byte[] overflow = new byte[6];
                    for(int x = 0; x < 300; x++)
                    {
                        if(x - 6 < 0)
                            overflow[x] = this.DisplayData[x,y];
                        else
                            this.DisplayData[x - 6, y] = this.DisplayData[x,y];
                    }

                    // Fill in the now-empty pixels
                    this.DisplayData[294,y] = copy ? overflow[0] : this.ColorTable[scroll.Color];
                    this.DisplayData[295,y] = copy ? overflow[1] : this.ColorTable[scroll.Color];
                    this.DisplayData[296,y] = copy ? overflow[2] : this.ColorTable[scroll.Color];
                    this.DisplayData[297,y] = copy ? overflow[3] : this.ColorTable[scroll.Color];
                    this.DisplayData[298,y] = copy ? overflow[4] : this.ColorTable[scroll.Color];
                    this.DisplayData[299,y] = copy ? overflow[5] : this.ColorTable[scroll.Color];
                }
            }

            // If we're scrolling vertically
            if(scroll.VScrollCommand == ScrollCommand.Positive)
            {
                for(int x = 0; x < 300; x++)
                {
                    byte[] overflow = new byte[12];
                    for(int y = 215; y >= 0; y--)
                    {
                        if(y + 12 >= 216)
                            overflow[(y + 12) % 216] = this.DisplayData[x,y];
                        else
                            this.DisplayData[x, y + 12] = this.DisplayData[x,y];
                    }

                    // Fill in the now-empty pixels
                    this.DisplayData[x,0]  = copy ? overflow[0]  : this.ColorTable[scroll.Color];
                    this.DisplayData[x,1]  = copy ? overflow[1]  : this.ColorTable[scroll.Color];
                    this.DisplayData[x,2]  = copy ? overflow[2]  : this.ColorTable[scroll.Color];
                    this.DisplayData[x,3]  = copy ? overflow[3]  : this.ColorTable[scroll.Color];
                    this.DisplayData[x,4]  = copy ? overflow[4]  : this.ColorTable[scroll.Color];
                    this.DisplayData[x,5]  = copy ? overflow[5]  : this.ColorTable[scroll.Color];
                    this.DisplayData[x,6]  = copy ? overflow[6]  : this.ColorTable[scroll.Color];
                    this.DisplayData[x,7]  = copy ? overflow[7]  : this.ColorTable[scroll.Color];
                    this.DisplayData[x,8]  = copy ? overflow[8]  : this.ColorTable[scroll.Color];
                    this.DisplayData[x,9]  = copy ? overflow[9]  : this.ColorTable[scroll.Color];
                    this.DisplayData[x,10] = copy ? overflow[10] : this.ColorTable[scroll.Color];
                    this.DisplayData[x,11] = copy ? overflow[11] : this.ColorTable[scroll.Color];
                }
            }
            else if(scroll.VScrollCommand == ScrollCommand.Negative)
            {
                for(int x = 0; x < 300; x++)
                {
                    byte[] overflow = new byte[12];
                    for(int y = 0; y < 216; y++)
                    {
                        if(y - 12 < 0)
                            overflow[y] = this.DisplayData[x,y];
                        else
                            this.DisplayData[x, y - 12] = this.DisplayData[x,y];
                    }

                    // Fill in the now-empty pixels
                    this.DisplayData[x,204] = copy ? overflow[0]  : this.ColorTable[scroll.Color];
                    this.DisplayData[x,205] = copy ? overflow[1]  : this.ColorTable[scroll.Color];
                    this.DisplayData[x,206] = copy ? overflow[2]  : this.ColorTable[scroll.Color];
                    this.DisplayData[x,207] = copy ? overflow[3]  : this.ColorTable[scroll.Color];
                    this.DisplayData[x,208] = copy ? overflow[4]  : this.ColorTable[scroll.Color];
                    this.DisplayData[x,209] = copy ? overflow[5]  : this.ColorTable[scroll.Color];
                    this.DisplayData[x,210] = copy ? overflow[6]  : this.ColorTable[scroll.Color];
                    this.DisplayData[x,211] = copy ? overflow[7]  : this.ColorTable[scroll.Color];
                    this.DisplayData[x,212] = copy ? overflow[8]  : this.ColorTable[scroll.Color];
                    this.DisplayData[x,213] = copy ? overflow[9]  : this.ColorTable[scroll.Color];
                    this.DisplayData[x,214] = copy ? overflow[10] : this.ColorTable[scroll.Color];
                    this.DisplayData[x,215] = copy ? overflow[11] : this.ColorTable[scroll.Color];
                }
            }
        }
    }
}