using System;

namespace RedBookPlayer.Models.Hardware.Karaoke
{
    /// <see cref="https://jbum.com/cdg_revealed.html"/>
    internal class KaraokeDisplay
    {
        /// <summary>
        /// Display data as a 2-dimensional byte array
        /// </summary>
        /// <remarks>
        /// Coordinate (0,0) is the upper left corner of the display
        /// Coordinate (299, 215) is the lower right corner of the display
        ///
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
        /// Print the current color map to console
        /// </summary>
        public void DebugPrintScreen()
        {
            string screenDump = string.Empty;

            for(int y = 0; y < 216; y++)
            {
                for(int x = 0; x < 300; x++)
                    screenDump += $"{this.DisplayData[x,y]:X}";

                screenDump += Environment.NewLine;
            }
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

        #region Command Processors

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
                    this.DisplayData[x,y] = memPreset.Color;
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
                    this.DisplayData[x,y] = borderPreset.Color;

            for(int x = 297; x < 300; x++)
                for(int y = 0; y < 216; y++)
                    this.DisplayData[x,y] = borderPreset.Color;

            for(int x = 0; x < 300; x++)
                for(int y = 0; y < 6; y++)
                    this.DisplayData[x,y] = borderPreset.Color;

            for(int x = 0; x < 300; x++)
                for(int y = 210; y < 216; y++)
                    this.DisplayData[x,y] = borderPreset.Color;
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
        private void ScrollDisplay(Scroll scroll, bool copy)
        {
            if(scroll == null || scroll.HScrollOffset < 0 || scroll.VScrollOffset < 0)
                return;

            // Derive the scroll values based on offsets
            int hOffsetTotal = 6  + scroll.HScrollOffset;
            int vOffsetTotal = 12 + scroll.VScrollOffset;

            // If we're scrolling horizontally
            if(scroll.HScrollCommand == ScrollCommand.Positive
                || (scroll.HScrollCommand == ScrollCommand.NoScroll && scroll.HScrollOffset > 0))
            {
                for(int y = 0; y < 216; y++)
                {
                    byte[] overflow = new byte[hOffsetTotal];
                    
                    for(int x = 299; x >= 0; x--)
                    {
                        if(x + hOffsetTotal >= 300)
                            overflow[(x + hOffsetTotal) % 300] = this.DisplayData[x,y];
                        else
                            this.DisplayData[x + hOffsetTotal, y] = this.DisplayData[x,y];
                    }

                    // Fill in the now-empty pixels
                    for(int i = 0; i < hOffsetTotal; i++)
                        this.DisplayData[i,y] = copy ? overflow[i] : scroll.Color;
                }
            }
            else if(scroll.HScrollCommand == ScrollCommand.Negative)
            {
                for(int y = 0; y < 216; y++)
                {
                    byte[] overflow = new byte[hOffsetTotal];
                    
                    for(int x = 0; x < 300; x++)
                    {
                        if(x - hOffsetTotal < 0)
                            overflow[x] = this.DisplayData[x,y];
                        else
                            this.DisplayData[x - hOffsetTotal, y] = this.DisplayData[x,y];
                    }

                    // Fill in the now-empty pixels
                    for(int i = 299; i > 299 - hOffsetTotal; i++)
                        this.DisplayData[i,y] = copy ? overflow[i] : scroll.Color;
                }
            }

            // If we're scrolling vertically
            if(scroll.VScrollCommand == ScrollCommand.Positive
                || (scroll.VScrollCommand == ScrollCommand.NoScroll && scroll.VScrollOffset > 0))
            {
                for(int x = 0; x < 300; x++)
                {
                    byte[] overflow = new byte[vOffsetTotal];
                    
                    for(int y = 215; y >= 0; y--)
                    {
                        if(y + vOffsetTotal >= 216)
                            overflow[(y + vOffsetTotal) % 216] = this.DisplayData[x,y];
                        else
                            this.DisplayData[x, y + vOffsetTotal] = this.DisplayData[x,y];
                    }

                    // Fill in the now-empty pixels
                    for(int i = 0; i < vOffsetTotal; i++)
                        this.DisplayData[x,i] = copy ? overflow[i] : scroll.Color;
                }
            }
            else if(scroll.VScrollCommand == ScrollCommand.Negative)
            {
                for(int x = 0; x < 300; x++)
                {
                    byte[] overflow = new byte[vOffsetTotal];
                    
                    for(int y = 0; y < 216; y++)
                    {
                        if(y - vOffsetTotal < 0)
                            overflow[y] = this.DisplayData[x,y];
                        else
                            this.DisplayData[x, y - vOffsetTotal] = this.DisplayData[x,y];
                    }

                    // Fill in the now-empty pixels
                    for(int i = 215; i > 215 - vOffsetTotal; i++)
                        this.DisplayData[x,i] = copy ? overflow[i] : scroll.Color;
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

        #endregion
    }
}