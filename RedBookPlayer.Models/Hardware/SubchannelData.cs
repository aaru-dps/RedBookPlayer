using System;
using System.Collections.Generic;

namespace RedBookPlayer.Models.Hardware
{
    /// <summary>
    /// Represents subchannel data for a single sector
    /// </summary>
    /// <see cref="https://jbum.com/cdg_revealed.html"/>
    internal class SubchannelData
    {
        public SubchannelPacket[] Packets { get; private set; } = new SubchannelPacket[4];

        /// <summary>
        /// Create a new subchannel data from a byte array
        /// </summary>
        public SubchannelData(byte[] bytes)
        {
            if(bytes == null || bytes.Length != 96)
                return;

            byte[] buffer = new byte[24];
            for(int i = 0; i < 4; i++)
            {
                Array.Copy(bytes, 24 * i, buffer, 0, 24);
                Packets[i] = new SubchannelPacket(buffer);
            }
        }

        /// <summary>
        /// Convert the packet data into separate named subchannels
        /// </summary>
        public Dictionary<char, byte[]> ConvertData()
        {
            if(this.Packets == null || this.Packets.Length != 4)
                return null;

            // Prepare the output formatted data
            Dictionary<char, byte[]> formattedData = new Dictionary<char, byte[]>
            {
                ['P'] = new byte[8],
                ['Q'] = new byte[8],
                ['R'] = new byte[8],
                ['S'] = new byte[8],
                ['T'] = new byte[8],
                ['U'] = new byte[8],
                ['V'] = new byte[8],
                ['W'] = new byte[8],
            };

            // Loop through all subchannel packets
            for(int i = 0; i < 4; i++)
            {
                Dictionary<char, byte[]> singleData = Packets[i].ConvertData();

                Array.Copy(singleData['P'], 0, formattedData['P'], 2 * i, 2);
                Array.Copy(singleData['Q'], 0, formattedData['Q'], 2 * i, 2);
                Array.Copy(singleData['R'], 0, formattedData['R'], 2 * i, 2);
                Array.Copy(singleData['S'], 0, formattedData['S'], 2 * i, 2);
                Array.Copy(singleData['T'], 0, formattedData['T'], 2 * i, 2);
                Array.Copy(singleData['U'], 0, formattedData['U'], 2 * i, 2);
                Array.Copy(singleData['V'], 0, formattedData['V'], 2 * i, 2);
                Array.Copy(singleData['W'], 0, formattedData['W'], 2 * i, 2);
            }

            return formattedData;
        }
    }
}