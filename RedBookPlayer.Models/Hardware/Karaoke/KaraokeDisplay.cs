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
        /// Current 16-entry color table
        /// </summary>
        /// <remarks>
        /// Each color entry has the following format:
        ///
        /// [---high byte---]   [---low byte----]
        ///  7 6 5 4 3 2 1 0     7 6 5 4 3 2 1 0
        ///  X X r r r r g g     X X g g b b b b
        /// 
        /// Note that P and Q channel bits need to be masked off (they are marked
        /// here with Xs.
        /// </remarks>
        public short[] ColorTable { get; private set; }

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
            this.ColorTable         = new short[16];
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
                    LoadColorTable(loadColorTableLower, false);
                    break;
                case SubchannelInstruction.LoadColorTableUpper:
                    var loadColorTableUpper = new LoadCLUT(packet.Data);
                    LoadColorTable(loadColorTableUpper, true);
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
                byte colorIndex = pattern[x,y] == 0 ? tileBlock.Color0 : tileBlock.Color1;

                if(xor)
                    this.DisplayData[adjustedX, adjustedY] = (byte)(colorIndex ^ this.DisplayData[adjustedX, adjustedY]);
                else
                    this.DisplayData[adjustedX, adjustedY] = colorIndex;
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
                    this.DisplayData[0,y] = copy ? overflow[0] : scroll.Color;
                    this.DisplayData[1,y] = copy ? overflow[1] : scroll.Color;
                    this.DisplayData[2,y] = copy ? overflow[2] : scroll.Color;
                    this.DisplayData[3,y] = copy ? overflow[3] : scroll.Color;
                    this.DisplayData[4,y] = copy ? overflow[4] : scroll.Color;
                    this.DisplayData[5,y] = copy ? overflow[5] : scroll.Color;
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
                    this.DisplayData[294,y] = copy ? overflow[0] : scroll.Color;
                    this.DisplayData[295,y] = copy ? overflow[1] : scroll.Color;
                    this.DisplayData[296,y] = copy ? overflow[2] : scroll.Color;
                    this.DisplayData[297,y] = copy ? overflow[3] : scroll.Color;
                    this.DisplayData[298,y] = copy ? overflow[4] : scroll.Color;
                    this.DisplayData[299,y] = copy ? overflow[5] : scroll.Color;
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
                    this.DisplayData[x,0]  = copy ? overflow[0]  : scroll.Color;
                    this.DisplayData[x,1]  = copy ? overflow[1]  : scroll.Color;
                    this.DisplayData[x,2]  = copy ? overflow[2]  : scroll.Color;
                    this.DisplayData[x,3]  = copy ? overflow[3]  : scroll.Color;
                    this.DisplayData[x,4]  = copy ? overflow[4]  : scroll.Color;
                    this.DisplayData[x,5]  = copy ? overflow[5]  : scroll.Color;
                    this.DisplayData[x,6]  = copy ? overflow[6]  : scroll.Color;
                    this.DisplayData[x,7]  = copy ? overflow[7]  : scroll.Color;
                    this.DisplayData[x,8]  = copy ? overflow[8]  : scroll.Color;
                    this.DisplayData[x,9]  = copy ? overflow[9]  : scroll.Color;
                    this.DisplayData[x,10] = copy ? overflow[10] : scroll.Color;
                    this.DisplayData[x,11] = copy ? overflow[11] : scroll.Color;
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
                    this.DisplayData[x,204] = copy ? overflow[0]  : scroll.Color;
                    this.DisplayData[x,205] = copy ? overflow[1]  : scroll.Color;
                    this.DisplayData[x,206] = copy ? overflow[2]  : scroll.Color;
                    this.DisplayData[x,207] = copy ? overflow[3]  : scroll.Color;
                    this.DisplayData[x,208] = copy ? overflow[4]  : scroll.Color;
                    this.DisplayData[x,209] = copy ? overflow[5]  : scroll.Color;
                    this.DisplayData[x,210] = copy ? overflow[6]  : scroll.Color;
                    this.DisplayData[x,211] = copy ? overflow[7]  : scroll.Color;
                    this.DisplayData[x,212] = copy ? overflow[8]  : scroll.Color;
                    this.DisplayData[x,213] = copy ? overflow[9]  : scroll.Color;
                    this.DisplayData[x,214] = copy ? overflow[10] : scroll.Color;
                    this.DisplayData[x,215] = copy ? overflow[11] : scroll.Color;
                }
            }
        }

        /// <summary>
        /// Load either the upper or lower half of the color table
        /// </summary>
        /// <param name="tableData">Color table data to load</param>
        /// <param name="upper">True for colors 8-15, false for colors 0-7</param>
        private void LoadColorTable(LoadCLUT tableData, bool upper)
        {
            if(tableData == null)
                return;

            // Load the color table data directly
            int start = upper ? 8 : 0;
            this.ColorTable[start]     = tableData.ColorSpec[0];
            this.ColorTable[start + 1] = tableData.ColorSpec[1];
            this.ColorTable[start + 2] = tableData.ColorSpec[2];
            this.ColorTable[start + 3] = tableData.ColorSpec[3];
            this.ColorTable[start + 4] = tableData.ColorSpec[4];
            this.ColorTable[start + 5] = tableData.ColorSpec[5];
            this.ColorTable[start + 6] = tableData.ColorSpec[6];
            this.ColorTable[start + 7] = tableData.ColorSpec[7];
        }
    }
}