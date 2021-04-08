using System;
using System.Threading.Tasks;
using Aaru.CommonTypes.Enums;
using Aaru.Decoders.CD;
using Aaru.DiscImages;
using Aaru.Helpers;
using System.Linq;
using CSCore.SoundOut;
using CSCore;
using NWaves.Audio;
using NWaves.Filters.BiQuad;
using static Aaru.Decoders.CD.FullTOC;

namespace RedBookPlayer
{
    public class Player
    {
        public bool Initialized = false;
        private int currentTrack = 0;
        public int CurrentTrack
        {
            get
            {
                return currentTrack;
            }

            private set
            {
                if (Image != null)
                {
                    if (value >= Image.Tracks.Count)
                    {
                        currentTrack = 0;
                    }
                    else if (value < 0)
                    {
                        currentTrack = Image.Tracks.Count - 1;
                    }
                    else
                    {
                        currentTrack = value;
                    }

                    byte[] flagsData = Image.ReadSectorTag(Image.Tracks[CurrentTrack].TrackSequence, SectorTagType.CdTrackFlags);
                    HasPreEmphasis = ((CdFlags)flagsData[0]).HasFlag(CdFlags.PreEmphasis);

                    if (!HasPreEmphasis)
                    {
                        byte[] subchannel = Image.ReadSectorTag(
                            Image.Tracks[CurrentTrack].TrackStartSector,
                            SectorTagType.CdSectorSubchannel
                        );

                        HasPreEmphasis = (subchannel[3] & 0b01000000) != 0;
                    }

                    TotalIndexes = Image.Tracks[CurrentTrack].Indexes.Count;
                    CurrentIndex = Image.Tracks[CurrentTrack].Indexes.Keys.GetEnumerator().Current;
                }
            }
        }
        public ushort CurrentIndex { get; private set; } = 1;
        private ulong currentSector = 0;
        private int currentSectorReadPosition = 0;
        public ulong CurrentSector
        {
            get
            {
                return currentSector;
            }

            private set
            {
                currentSector = value;

                if (Image != null)
                {
                    if (CurrentTrack < Image.Tracks.Count - 1 && CurrentSector >= Image.Tracks[CurrentTrack + 1].TrackStartSector)
                    {
                        CurrentTrack++;
                    }
                    else if (CurrentTrack > 0 && CurrentSector < Image.Tracks[CurrentTrack].TrackStartSector)
                    {
                        CurrentTrack--;
                    }

                    foreach (var item in Image.Tracks[CurrentTrack].Indexes.Reverse())
                    {
                        if ((int)CurrentSector >= item.Value)
                        {
                            CurrentIndex = item.Key;
                            return;
                        }
                    }
                    CurrentIndex = 0;
                }
            }
        }
        public bool HasPreEmphasis { get; private set; } = false;
        public int TotalTracks { get; private set; } = 0;
        public int TotalIndexes { get; private set; } = 0;
        public ulong TimeOffset { get; private set; } = 0;
        int volume = 100;
        public int Volume
        {
            get
            {
                return volume;
            }

            set
            {
                if (volume >= 0 && volume <= 100)
                {
                    volume = value;
                }
            }
        }
        public AaruFormat Image { get; private set; }
        FullTOC.CDFullTOC toc;
        PlayerSource source;
        ALSoundOut soundOut;
        BiQuadFilter deEmphasisFilterLeft;
        BiQuadFilter deEmphasisFilterRight;
        bool readingImage = false;

        public async void Init(AaruFormat image, bool autoPlay = false)
        {
            this.Image = image;

            if (await Task.Run(() => image.Info.ReadableMediaTags?.Contains(MediaTagType.CD_FullTOC)) != true)
            {
                Console.WriteLine("Full TOC not found");
                return;
            }

            byte[] tocBytes = await Task.Run(() => image.ReadDiskTag(MediaTagType.CD_FullTOC));

            if ((tocBytes?.Length ?? 0) == 0)
            {
                Console.WriteLine("Error reading TOC from disc image");
                return;
            }

            if (Swapping.Swap(BitConverter.ToUInt16(tocBytes, 0)) + 2 != tocBytes.Length)
            {
                byte[] tmp = new byte[tocBytes.Length + 2];
                Array.Copy(tocBytes, 0, tmp, 2, tocBytes.Length);
                tmp[0] = (byte)((tocBytes.Length & 0xFF00) >> 8);
                tmp[1] = (byte)(tocBytes.Length & 0xFF);
                tocBytes = tmp;
            }

            FullTOC.CDFullTOC? nullableToc = await Task.Run(() => FullTOC.Decode(tocBytes));

            if (nullableToc == null)
            {
                Console.WriteLine("Error decoding TOC");
                return;
            }

            toc = nullableToc.Value;

            Console.WriteLine(FullTOC.Prettify(toc));

            deEmphasisFilterLeft = new DeEmphasisFilter();
            deEmphasisFilterRight = new DeEmphasisFilter();
            source = new PlayerSource(ProviderRead);

            soundOut = new ALSoundOut(50);
            soundOut.Initialize(source);
            if (autoPlay)
            {
                soundOut.Play();
            }

            CurrentTrack = 0;
            LoadTrack(0);

            TotalTracks = image.Tracks.Count;
            TrackDataDescriptor firstTrack = toc.TrackDescriptors.First(d => d.ADR == 1 && d.POINT == 1);
            TimeOffset = (ulong)(firstTrack.PMIN * 60 * 75 + firstTrack.PSEC * 75 + firstTrack.PFRAME);

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
                sectorsToRead = (ulong)count / 2352 + 2;
                zeroSectorsAmount = 0;

                if (CurrentSector + sectorsToRead > Image.Info.Sectors)
                {
                    ulong oldSectorsToRead = sectorsToRead;
                    sectorsToRead = Image.Info.Sectors - CurrentSector;
                    zeroSectorsAmount = oldSectorsToRead - sectorsToRead;
                }

                if (sectorsToRead <= 0)
                {
                    LoadTrack(0);
                    currentSectorReadPosition = 0;
                }
            } while (sectorsToRead <= 0);

            byte[] zeroSectors = new Byte[zeroSectorsAmount * 2352];
            Array.Clear(zeroSectors, 0, zeroSectors.Length);
            byte[] audioData;

            Task<byte[]> task = Task.Run(() =>
            {
                try
                {
                    return Image.ReadSectors(CurrentSector, (uint)sectorsToRead).Concat(zeroSectors).ToArray();
                }
                catch (System.ArgumentOutOfRangeException)
                {
                    LoadTrack(0);
                    return Image.ReadSectors(CurrentSector, (uint)sectorsToRead).Concat(zeroSectors).ToArray();
                }
            });

            if (task.Wait(TimeSpan.FromMilliseconds(100)))
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
                if (!readingImage)
                {
                    readingImage = true;
                    Image.ReadSector(CurrentSector + 375);
                    readingImage = false;
                }
            });

            byte[] audioDataSegment = new byte[count];
            Array.Copy(audioData, currentSectorReadPosition, audioDataSegment, 0, count);

            if (HasPreEmphasis)
            {
                float[][] floatAudioData = new float[2][];
                floatAudioData[0] = new float[audioDataSegment.Length / 4];
                floatAudioData[1] = new float[audioDataSegment.Length / 4];
                ByteConverter.ToFloats16Bit(audioDataSegment, floatAudioData);

                for (int i = 0; i < floatAudioData[0].Length; i++)
                {
                    floatAudioData[0][i] = deEmphasisFilterLeft.Process(floatAudioData[0][i]);
                    floatAudioData[1][i] = deEmphasisFilterRight.Process(floatAudioData[1][i]);
                }

                ByteConverter.FromFloats16Bit(floatAudioData, audioDataSegment);
            }

            Array.Copy(audioDataSegment, 0, buffer, offset, count);

            currentSectorReadPosition += count;
            if (currentSectorReadPosition >= 2352)
            {
                CurrentSector += (ulong)currentSectorReadPosition / 2352;
                currentSectorReadPosition %= 2352;
            }

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
            if (Image == null)
            {
                return;
            }

            soundOut.Play();
        }

        public void Pause()
        {
            if (Image == null)
            {
                return;
            }

            soundOut.Stop();
        }

        public void Stop()
        {
            if (Image == null)
            {
                return;
            }

            soundOut.Stop();
            LoadTrack(CurrentTrack);
        }

        public void NextTrack()
        {
            if (Image == null)
            {
                return;
            }

            if (++CurrentTrack >= Image.Tracks.Count)
            {
                CurrentTrack = 0;
            }

            LoadTrack(CurrentTrack);
        }

        public void PreviousTrack()
        {
            if (Image == null)
            {
                return;
            }

            if (CurrentSector < (ulong)Image.Tracks[CurrentTrack].Indexes[1] + 75)
            {
                if (--CurrentTrack < 0)
                {
                    CurrentTrack = Image.Tracks.Count - 1;
                }
            }

            LoadTrack(CurrentTrack);
        }

        public void NextIndex(bool changeTrack)
        {
            if (Image == null)
            {
                return;
            }

            if (++CurrentIndex > Image.Tracks[CurrentTrack].Indexes.Keys.Max())
            {
                if (changeTrack)
                {
                    NextTrack();
                    CurrentSector = (ulong)Image.Tracks[CurrentTrack].Indexes[1];
                }
                else
                {
                    CurrentSector = (ulong)Image.Tracks[CurrentTrack].Indexes[--CurrentIndex];
                }
            }
            else
            {
                CurrentSector = (ulong)Image.Tracks[CurrentTrack].Indexes[CurrentIndex];
            }
        }

        public void PreviousIndex(bool changeTrack)
        {
            if (Image == null)
            {
                return;
            }

            if (CurrentIndex <= 1 || --CurrentIndex < Image.Tracks[CurrentTrack].Indexes.Keys.Min())
            {
                if (changeTrack)
                {
                    PreviousTrack();
                    CurrentSector = (ulong)Image.Tracks[CurrentTrack].Indexes.Values.Max();
                }
                else
                {
                    CurrentSector = (ulong)Image.Tracks[CurrentTrack].Indexes[1];
                }
            }
            else
            {
                CurrentSector = (ulong)Image.Tracks[CurrentTrack].Indexes[CurrentIndex];
            }
        }

        public void FastForward()
        {
            if (Image == null)
            {
                return;
            }

            CurrentSector = Math.Min(Image.Info.Sectors - 1, CurrentSector + 75);
        }

        public void Rewind()
        {
            if (Image == null)
            {
                return;
            }

            if (CurrentSector >= 75)
                CurrentSector -= 75;
        }

        public void EnableDeEmphasis()
        {
            HasPreEmphasis = true;
        }

        public void DisableDeEmphasis()
        {
            HasPreEmphasis = false;
        }
    }

    public class PlayerSource : IWaveSource
    {
        public CSCore.WaveFormat WaveFormat => new CSCore.WaveFormat();
        bool IAudioSource.CanSeek => throw new NotImplementedException();
        public long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public long Length => throw new NotImplementedException();

        public bool Run = true;
        private ReadFunction read;

        public delegate int ReadFunction(byte[] buffer, int offset, int count);

        public PlayerSource(ReadFunction read)
        {
            this.read = read;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (!Run)
            {
                Array.Clear(buffer, offset, count);
                return count;
            }
            else
            {
                return read(buffer, offset, count);
            }
        }

        public void Start()
        {
            Run = true;
        }

        public void Stop()
        {
            Run = false;
        }

        public void Dispose()
        {
        }
    }
}
