using System;
using System.Linq;
using System.Threading.Tasks;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using Aaru.DiscImages;
using Aaru.Helpers;
using CSCore;
using CSCore.SoundOut;
using NWaves.Audio;
using NWaves.Filters.BiQuad;
using static Aaru.Decoders.CD.FullTOC;
using WaveFormat = CSCore.WaveFormat;

namespace RedBookPlayer
{
    public class Player
    {
        public enum TrackType
        {
            Audio, Data
        }

        readonly object readingImage = new object();

        ushort       currentIndex = 1;
        ulong        currentSector;
        int          currentSectorReadPosition;
        int          currentTrack;
        BiQuadFilter deEmphasisFilterLeft;
        BiQuadFilter deEmphasisFilterRight;
        public bool  Initialized;
        ALSoundOut   soundOut;
        PlayerSource source;
        CDFullTOC    toc;
        int          volume = 100;

        public int CurrentTrack
        {
            get => currentTrack;

            private set
            {
                if(Image == null)
                    return;

                if(value >= Image.Tracks.Count)
                    currentTrack = 0;
                else if(value < 0)
                    currentTrack = Image.Tracks.Count - 1;
                else
                    currentTrack = value;

                byte[] flagsData =
                    Image.ReadSectorTag(Image.Tracks[CurrentTrack].TrackSequence, SectorTagType.CdTrackFlags);

                ApplyDeEmphasis = ((CdFlags)flagsData[0]).HasFlag(CdFlags.PreEmphasis);

                byte[] subchannel = Image.ReadSectorTag(Image.Tracks[CurrentTrack].TrackStartSector,
                                                        SectorTagType.CdSectorSubchannel);

                if(!ApplyDeEmphasis)
                    ApplyDeEmphasis = (subchannel[3] & 0b01000000) != 0;

                CopyAllowed = (subchannel[2] & 0b01000000) != 0;
                TrackType_  = (subchannel[1] & 0b01000000) != 0 ? TrackType.Data : TrackType.Audio;

                TrackHasEmphasis = ApplyDeEmphasis;

                TotalIndexes = Image.Tracks[CurrentTrack].Indexes.Keys.Max();
                CurrentIndex = Image.Tracks[CurrentTrack].Indexes.Keys.Min();
            }
        }

        public ushort CurrentIndex
        {
            get => currentIndex;

            private set
            {
                currentIndex = value;

                SectionStartSector = (ulong)Image.Tracks[CurrentTrack].Indexes[CurrentIndex];
                TotalTime = Image.Tracks[CurrentTrack].TrackEndSector - Image.Tracks[CurrentTrack].TrackStartSector;
            }
        }

        public ulong CurrentSector
        {
            get => currentSector;

            private set
            {
                currentSector = value;

                if(Image == null)
                    return;

                if((CurrentTrack  < Image.Tracks.Count - 1 &&
                    CurrentSector >= Image.Tracks[CurrentTrack + 1].TrackStartSector) ||
                   (CurrentTrack > 0 && CurrentSector < Image.Tracks[CurrentTrack].TrackStartSector))
                {
                    foreach(Track track in Image.Tracks.ToArray().Reverse())
                    {
                        if(CurrentSector < track.TrackStartSector)
                            continue;

                        CurrentTrack = (int)track.TrackSequence - 1;

                        break;
                    }
                }

                foreach((ushort key, int i) in Image.Tracks[CurrentTrack].Indexes.Reverse())
                {
                    if((int)CurrentSector < i)
                        continue;

                    CurrentIndex = key;

                    return;
                }

                CurrentIndex = 0;
            }
        }

        public bool       TrackHasEmphasis   { get; private set; }
        public bool       ApplyDeEmphasis    { get; private set; }
        public bool       CopyAllowed        { get; private set; }
        public TrackType? TrackType_         { get; private set; }
        public ulong      SectionStartSector { get; private set; }
        public int        TotalTracks        { get; private set; }
        public int        TotalIndexes       { get; private set; }
        public ulong      TimeOffset         { get; private set; }
        public ulong      TotalTime          { get; private set; }

        public int Volume
        {
            get => volume;

            set
            {
                if(volume >= 0 &&
                   volume <= 100)
                    volume = value;
            }
        }

        public AaruFormat Image { get; private set; }

        public async void Init(AaruFormat image, bool autoPlay = false)
        {
            Image = image;

            if(await Task.Run(() => image.Info.ReadableMediaTags?.Contains(MediaTagType.CD_FullTOC)) != true)
            {
                Console.WriteLine("Full TOC not found");

                return;
            }

            byte[] tocBytes = await Task.Run(() => image.ReadDiskTag(MediaTagType.CD_FullTOC));

            if((tocBytes?.Length ?? 0) == 0)
            {
                Console.WriteLine("Error reading TOC from disc image");

                return;
            }

            if(Swapping.Swap(BitConverter.ToUInt16(tocBytes, 0)) + 2 != tocBytes.Length)
            {
                byte[] tmp = new byte[tocBytes.Length + 2];
                Array.Copy(tocBytes, 0, tmp, 2, tocBytes.Length);
                tmp[0]   = (byte)((tocBytes.Length & 0xFF00) >> 8);
                tmp[1]   = (byte)(tocBytes.Length & 0xFF);
                tocBytes = tmp;
            }

            CDFullTOC? nullableToc = await Task.Run(() => Decode(tocBytes));

            if(nullableToc == null)
            {
                Console.WriteLine("Error decoding TOC");

                return;
            }

            toc = nullableToc.Value;

            Console.WriteLine(Prettify(toc));

            if(deEmphasisFilterLeft == null)
            {
                deEmphasisFilterLeft  = new DeEmphasisFilter();
                deEmphasisFilterRight = new DeEmphasisFilter();
            }
            else
            {
                deEmphasisFilterLeft.Reset();
                deEmphasisFilterRight.Reset();
            }

            if(source == null)
            {
                source = new PlayerSource(ProviderRead);

                soundOut = new ALSoundOut(100);
                soundOut.Initialize(source);
            }
            else
                soundOut.Stop();

            CurrentTrack = 0;
            LoadTrack(0);

            if(autoPlay)
                soundOut.Play();
            else
                TotalIndexes = 0;

            TotalTracks = image.Tracks.Count;
            TrackDataDescriptor firstTrack = toc.TrackDescriptors.First(d => d.ADR == 1 && d.POINT == 1);
            TimeOffset = (ulong)((firstTrack.PMIN * 60 * 75) + (firstTrack.PSEC * 75) + firstTrack.PFRAME);
            TotalTime  = TimeOffset + image.Tracks.Last().TrackEndSector;

            Volume = App.Settings.Volume;

            Initialized = true;

            source.Start();
        }

        public int ProviderRead(byte[] buffer, int offset, int count)
        {
            soundOut.Volume = (float)Volume / 100;

            ulong sectorsToRead;
            ulong zeroSectorsAmount;

            do
            {
                sectorsToRead     = ((ulong)count / 2352) + 2;
                zeroSectorsAmount = 0;

                if(CurrentSector + sectorsToRead > Image.Info.Sectors)
                {
                    ulong oldSectorsToRead = sectorsToRead;
                    sectorsToRead     = Image.Info.Sectors - CurrentSector;
                    zeroSectorsAmount = oldSectorsToRead   - sectorsToRead;
                }

                if(sectorsToRead > 0)
                    continue;

                LoadTrack(0);
                currentSectorReadPosition = 0;
            } while(sectorsToRead <= 0);

            byte[] zeroSectors = new byte[zeroSectorsAmount * 2352];
            Array.Clear(zeroSectors, 0, zeroSectors.Length);
            byte[] audioData;

            Task<byte[]> task = Task.Run(() =>
            {
                lock(readingImage)
                {
                    try
                    {
                        return Image.ReadSectors(CurrentSector, (uint)sectorsToRead).Concat(zeroSectors).ToArray();
                    }
                    catch(ArgumentOutOfRangeException)
                    {
                        LoadTrack(0);

                        return Image.ReadSectors(CurrentSector, (uint)sectorsToRead).Concat(zeroSectors).ToArray();
                    }
                }
            });

            if(task.Wait(TimeSpan.FromMilliseconds(100)))
            {
                audioData = task.Result;
            }
            else
            {
                Array.Clear(buffer, offset, count);

                return count;
            }

            Task.Run(() =>
            {
                lock(readingImage)
                {
                    Image.ReadSector(CurrentSector + 375);
                }
            });

            byte[] audioDataSegment = new byte[count];
            Array.Copy(audioData, currentSectorReadPosition, audioDataSegment, 0, count);

            if(ApplyDeEmphasis)
            {
                float[][] floatAudioData = new float[2][];
                floatAudioData[0] = new float[audioDataSegment.Length / 4];
                floatAudioData[1] = new float[audioDataSegment.Length / 4];
                ByteConverter.ToFloats16Bit(audioDataSegment, floatAudioData);

                for(int i = 0; i < floatAudioData[0].Length; i++)
                {
                    floatAudioData[0][i] = deEmphasisFilterLeft.Process(floatAudioData[0][i]);
                    floatAudioData[1][i] = deEmphasisFilterRight.Process(floatAudioData[1][i]);
                }

                ByteConverter.FromFloats16Bit(floatAudioData, audioDataSegment);
            }

            Array.Copy(audioDataSegment, 0, buffer, offset, count);

            currentSectorReadPosition += count;

            if(currentSectorReadPosition < 2352)
                return count;

            CurrentSector             += (ulong)currentSectorReadPosition / 2352;
            currentSectorReadPosition %= 2352;

            return count;
        }

        public void LoadTrack(int index)
        {
            bool oldRun = source.Run;
            source.Stop();

            CurrentSector = (ulong)Image.Tracks[index].Indexes[1];

            source.Run = oldRun;
        }

        public void Play()
        {
            if(Image == null)
                return;

            soundOut.Play();
            TotalIndexes = Image.Tracks[CurrentTrack].Indexes.Keys.Max();
        }

        public void Pause()
        {
            if(Image == null)
                return;

            soundOut.Stop();
        }

        public void Stop()
        {
            if(Image == null)
                return;

            soundOut.Stop();
            LoadTrack(CurrentTrack);
        }

        public void NextTrack()
        {
            if(Image == null)
                return;

            if(CurrentTrack + 1 >= Image.Tracks.Count)
                CurrentTrack = 0;
            else
                CurrentTrack++;

            LoadTrack(CurrentTrack);
        }

        public void PreviousTrack()
        {
            if(Image == null)
                return;

            if(CurrentSector < (ulong)Image.Tracks[CurrentTrack].Indexes[1] + 75)
            {
                if(App.Settings.AllowSkipHiddenTrack &&
                   CurrentTrack  == 0                &&
                   CurrentSector >= 75)
                    CurrentSector = 0;
                else
                {
                    if(CurrentTrack - 1 < 0)
                        CurrentTrack = Image.Tracks.Count - 1;
                    else
                        CurrentTrack--;
                }
            }

            LoadTrack(CurrentTrack);
        }

        public void NextIndex(bool changeTrack)
        {
            if(Image == null)
                return;

            if(CurrentIndex + 1 > Image.Tracks[CurrentTrack].Indexes.Keys.Max())
            {
                if(!changeTrack)
                    return;

                NextTrack();
                CurrentSector = (ulong)Image.Tracks[CurrentTrack].Indexes.Values.Min();
            }
            else
                CurrentSector = (ulong)Image.Tracks[CurrentTrack].Indexes[++CurrentIndex];
        }

        public void PreviousIndex(bool changeTrack)
        {
            if(Image == null)
                return;

            if(CurrentIndex - 1 < Image.Tracks[CurrentTrack].Indexes.Keys.Min())
            {
                if(!changeTrack)
                    return;

                PreviousTrack();
                CurrentSector = (ulong)Image.Tracks[CurrentTrack].Indexes.Values.Max();
            }
            else
                CurrentSector = (ulong)Image.Tracks[CurrentTrack].Indexes[--CurrentIndex];
        }

        public void FastForward()
        {
            if(Image == null)
                return;

            CurrentSector = Math.Min(Image.Info.Sectors - 1, CurrentSector + 75);
        }

        public void Rewind()
        {
            if(Image == null)
                return;

            if(CurrentSector >= 75)
                CurrentSector -= 75;
        }

        public void EnableDeEmphasis() => ApplyDeEmphasis = true;

        public void DisableDeEmphasis() => ApplyDeEmphasis = false;
    }

    public class PlayerSource : IWaveSource
    {
        public delegate int ReadFunction(byte[] buffer, int offset, int count);

        readonly ReadFunction read;

        public bool Run = true;

        public PlayerSource(ReadFunction read) => this.read = read;

        public WaveFormat WaveFormat => new WaveFormat();
        bool IAudioSource.CanSeek    => throw new NotImplementedException();

        public long Position
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public long Length => throw new NotImplementedException();

        public int Read(byte[] buffer, int offset, int count)
        {
            if(Run)
                return read(buffer, offset, count);

            Array.Clear(buffer, offset, count);

            return count;
        }

        public void Dispose() {}

        public void Start() => Run = true;

        public void Stop() => Run = false;
    }
}