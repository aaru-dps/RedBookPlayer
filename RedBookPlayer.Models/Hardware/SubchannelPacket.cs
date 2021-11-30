using System;
using System.Collections.Generic;

namespace RedBookPlayer.Models.Hardware
{
    /// <summary>
    /// Represents a single packet of subcode data
    /// </summary>
    /// <see cref="https://jbum.com/cdg_revealed.html"/>
    internal class SubchannelPacket
    {
        public byte Command { get; private set; }
        
        public SubchannelInstruction Instruction { get; private set; }
        
        public byte[] ParityQ { get; private set; } = new byte[2];
        
        public byte[] Data { get; private set; } = new byte[16];
        
        public byte[] ParityP { get; private set; } = new byte[4];

        /// <summary>
        /// Create a new subchannel packet from a byte array
        /// </summary>
        public SubchannelPacket(byte[] bytes)
        {
            if(bytes == null || bytes.Length != 24)
                return;

            this.Command        = bytes[0];
            this.Instruction    = (SubchannelInstruction)bytes[1];

            Array.Copy(bytes, 2,  this.ParityQ, 0, 2);
            Array.Copy(bytes, 4,  this.Data,    0, 16);
            Array.Copy(bytes, 20, this.ParityP, 0, 4);
        }

        #region Standard Handling

        /// <summary>
        /// Convert the data into separate named subchannels
        /// </summary>
        public Dictionary<char, byte[]> ConvertData()
        {
            if(this.Data == null || this.Data.Length != 16)
                return null;

            // Create the output dictionary for the formatted data
            Dictionary<char, byte[]> formattedData = new Dictionary<char, byte[]>
            {
                ['P'] = new byte[2],
                ['Q'] = new byte[2],
                ['R'] = new byte[2],
                ['S'] = new byte[2],
                ['T'] = new byte[2],
                ['U'] = new byte[2],
                ['V'] = new byte[2],
                ['W'] = new byte[2],
            };

            // Loop through all bytes in the subchannel data and populate
            int index = -1;
            for(int i = 0; i < 16; i++)
            {
                // Get the modulo value of the current byte
                int modValue = i % 8;
                if(modValue == 0)
                    index++;

                // Retrieve the next byte
                byte b = this.Data[i];

                // Set the respective bit in the new byte data
                formattedData['P'][index] |= (byte)(HasBitSet(b, 7) ? 1 << (7 - modValue) : 0);
                formattedData['Q'][index] |= (byte)(HasBitSet(b, 6) ? 1 << (7 - modValue) : 0);
                formattedData['R'][index] |= (byte)(HasBitSet(b, 5) ? 1 << (7 - modValue) : 0);
                formattedData['S'][index] |= (byte)(HasBitSet(b, 4) ? 1 << (7 - modValue) : 0);
                formattedData['T'][index] |= (byte)(HasBitSet(b, 3) ? 1 << (7 - modValue) : 0);
                formattedData['U'][index] |= (byte)(HasBitSet(b, 2) ? 1 << (7 - modValue) : 0);
                formattedData['V'][index] |= (byte)(HasBitSet(b, 1) ? 1 << (7 - modValue) : 0);
                formattedData['W'][index] |= (byte)(HasBitSet(b, 0) ? 1 << (7 - modValue) : 0);
            }

            return formattedData;
        }

        /// <summary>
        /// Check if a bit is set in a byte
        /// </summary>
        /// <param name="value">Byte value to check</param>
        /// <param name="bitIndex">Index of the bit to check</param>
        /// <returns>True if the bit was set, false otherwise</returns>
        private bool HasBitSet(byte value, int bitIndex) => (value & (1 << bitIndex)) != 0;
    
        #endregion

        #region CD+G Handling

        /// <summary>
        /// Determine if a packet is CD+G data
        /// </summary>
        public bool IsCDGPacket()
        {
            byte lowerSixBits = (byte)(this.Command & 0x3F);
            return lowerSixBits == 0x09;
        }

        #endregion
    }
}