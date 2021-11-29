using NWaves.Audio;
using NWaves.Filters.BiQuad;

namespace RedBookPlayer.Models.Audio
{
    /// <summary>
    /// Output stage that represents all filters on the audio
    /// </summary>
    public class FilterStage
    {
        /// <summary>
        /// Left channel de-emphasis filter
        /// </summary>
        private BiQuadFilter _deEmphasisFilterLeft;

        /// <summary>
        /// Right channel de-emphasis filter
        /// </summary>
        private BiQuadFilter _deEmphasisFilterRight;

        /// <summary>
        /// Process audio data with internal filters
        /// </summary>
        /// <param name="audioData">Audio data to process</param>
        public void ProcessAudioData(byte[] audioData)
        {
            float[][] floatAudioData = new float[2][];
            floatAudioData[0] = new float[audioData.Length / 4];
            floatAudioData[1] = new float[audioData.Length / 4];
            ByteConverter.ToFloats16Bit(audioData, floatAudioData);

            for(int i = 0; i < floatAudioData[0].Length; i++)
            {
                floatAudioData[0][i] = _deEmphasisFilterLeft.Process(floatAudioData[0][i]);
                floatAudioData[1][i] = _deEmphasisFilterRight.Process(floatAudioData[1][i]);
            }

            ByteConverter.FromFloats16Bit(floatAudioData, audioData);
        }

        /// <summary>
        /// Sets or resets the output filters
        /// </summary>
        public void SetupFilters()
        {
            if(_deEmphasisFilterLeft == null)
            {
                _deEmphasisFilterLeft = new DeEmphasisFilter();
                _deEmphasisFilterRight = new DeEmphasisFilter();
            }
            else
            {
                _deEmphasisFilterLeft.Reset();
                _deEmphasisFilterRight.Reset();
            }
        }
    }
}