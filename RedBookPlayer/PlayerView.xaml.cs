using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Aaru.CommonTypes.Interfaces;
using Aaru.DiscImages;
using Aaru.Filters;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ReactiveUI;

namespace RedBookPlayer
{
    public class PlayerView : UserControl
    {
        public static Player Player = new Player();
        TextBlock            currentTrack;
        Image[]              digits;
        Timer                updateTimer;

        public PlayerView() => InitializeComponent(null);

        public PlayerView(string xaml) => InitializeComponent(xaml);

        public async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            string path = await GetPath();

            if(path == null)
            {
                return;
            }

            await Task.Run(() =>
            {
                var     image  = new AaruFormat();
                IFilter filter = new ZZZNoFilter();
                filter.Open(path);
                image.Open(filter);

                Player.Init(image, App.Settings.AutoPlay);
            });

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MainWindow.Instance.Title = "RedBookPlayer - " + path.Split('/').Last().Split('\\').Last();
            });
        }

        public async Task<string> GetPath()
        {
            var dialog = new OpenFileDialog();
            dialog.AllowMultiple = false;

            List<string> knownExtensions = new AaruFormat().KnownExtensions.ToList();

            dialog.Filters.Add(new FileDialogFilter
            {
                Name       = "Aaru Image Format (*" + string.Join(", *", knownExtensions) + ")",
                Extensions = knownExtensions.ConvertAll(e => e.Substring(1))
            });

            return (await dialog.ShowAsync((Window)Parent.Parent))?.FirstOrDefault();
        }

        public void PlayButton_Click(object sender, RoutedEventArgs e) => Player.Play();

        public void PauseButton_Click(object sender, RoutedEventArgs e) => Player.Pause();

        public void StopButton_Click(object sender, RoutedEventArgs e) => Player.Stop();

        public void NextTrackButton_Click(object sender, RoutedEventArgs e) => Player.NextTrack();

        public void PreviousTrackButton_Click(object sender, RoutedEventArgs e) => Player.PreviousTrack();

        public void NextIndexButton_Click(object sender, RoutedEventArgs e) =>
            Player.NextIndex(App.Settings.IndexButtonChangeTrack);

        public void PreviousIndexButton_Click(object sender, RoutedEventArgs e) =>
            Player.PreviousIndex(App.Settings.IndexButtonChangeTrack);

        public void FastForwardButton_Click(object sender, RoutedEventArgs e) => Player.FastForward();

        public void RewindButton_Click(object sender, RoutedEventArgs e) => Player.Rewind();

        public void EnableDeEmphasisButton_Click(object sender, RoutedEventArgs e) => Player.EnableDeEmphasis();

        public void DisableDeEmphasisButton_Click(object sender, RoutedEventArgs e) => Player.DisableDeEmphasis();

        void UpdateView(object sender, ElapsedEventArgs e)
        {
            if(Player.Initialized)
            {
                ulong sectorTime = Player.CurrentSector;

                if(Player.SectionStartSector != 0)
                {
                    sectorTime -= Player.SectionStartSector;
                }
                else
                {
                    sectorTime += Player.TimeOffset;
                }

                int[] numbers =
                {
                    Player.CurrentTrack + 1, Player.CurrentIndex, (int)(sectorTime / (75 * 60)),
                    (int)(sectorTime / 75 % 60), (int)(sectorTime % 75), Player.TotalTracks, Player.TotalIndexes,
                    (int)(Player.TotalTime / (75 * 60)), (int)(Player.TotalTime / 75 % 60), (int)(Player.TotalTime % 75)
                };

                string digitString = string.Join("", numbers.Select(i => i.ToString().PadLeft(2, '0').Substring(0, 2)));

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    for(int i = 0; i < digits.Length; i++)
                    {
                        if(digits[i] != null)
                        {
                            digits[i].Source = GetBitmap(digitString[i]);
                        }
                    }

                    var dataContext = (PlayerViewModel)DataContext;
                    dataContext.HiddenTrack      = Player.TimeOffset > 150;
                    dataContext.ApplyDeEmphasis  = Player.ApplyDeEmphasis;
                    dataContext.TrackHasEmphasis = Player.TrackHasEmphasis;
                    dataContext.CopyAllowed      = Player.CopyAllowed;
                    dataContext.IsAudioTrack     = Player.TrackType_ == Player.TrackType.Audio;
                    dataContext.IsDataTrack      = Player.TrackType_ == Player.TrackType.Data;
                });
            }
            else
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach(Image digit in digits)
                    {
                        if(digit != null)
                        {
                            digit.Source = GetBitmap('-');
                        }
                    }
                });
            }
        }

        Bitmap GetBitmap(char character)
        {
            if(App.Settings.SelectedTheme == "default")
            {
                IAssetLoader assets = AvaloniaLocator.Current.GetService<IAssetLoader>();

                return new Bitmap(assets.Open(new Uri($"avares://RedBookPlayer/Assets/{character}.png")));
            }

            string themeDirectory = Directory.GetCurrentDirectory() + "/themes/" + App.Settings.SelectedTheme;
            Bitmap bitmap;

            using(FileStream stream = File.Open(themeDirectory + $"/{character}.png", FileMode.Open))
            {
                bitmap = new Bitmap(stream);
            }

            return bitmap;
        }

        public void Initialize()
        {
            digits = new Image[20];

            digits[0] = this.FindControl<Image>("TrackDigit1");
            digits[1] = this.FindControl<Image>("TrackDigit2");

            digits[2] = this.FindControl<Image>("IndexDigit1");
            digits[3] = this.FindControl<Image>("IndexDigit2");

            digits[4] = this.FindControl<Image>("TimeDigit1");
            digits[5] = this.FindControl<Image>("TimeDigit2");
            digits[6] = this.FindControl<Image>("TimeDigit3");
            digits[7] = this.FindControl<Image>("TimeDigit4");
            digits[8] = this.FindControl<Image>("TimeDigit5");
            digits[9] = this.FindControl<Image>("TimeDigit6");

            digits[10] = this.FindControl<Image>("TotalTracksDigit1");
            digits[11] = this.FindControl<Image>("TotalTracksDigit2");

            digits[12] = this.FindControl<Image>("TotalIndexesDigit1");
            digits[13] = this.FindControl<Image>("TotalIndexesDigit2");

            digits[14] = this.FindControl<Image>("TotalTimeDigit1");
            digits[15] = this.FindControl<Image>("TotalTimeDigit2");
            digits[16] = this.FindControl<Image>("TotalTimeDigit3");
            digits[17] = this.FindControl<Image>("TotalTimeDigit4");
            digits[18] = this.FindControl<Image>("TotalTimeDigit5");
            digits[19] = this.FindControl<Image>("TotalTimeDigit6");

            currentTrack = this.FindControl<TextBlock>("CurrentTrack");
        }

        void InitializeComponent(string xaml)
        {
            DataContext = new PlayerViewModel();

            if(xaml != null)
            {
                new AvaloniaXamlLoader().Load(xaml, null, this);
            }
            else
            {
                AvaloniaXamlLoader.Load(this);
            }

            Initialize();

            updateTimer = new Timer(1000 / 60);

            updateTimer.Elapsed += (sender, e) =>
            {
                try
                {
                    UpdateView(sender, e);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
            };

            updateTimer.AutoReset = true;
            updateTimer.Start();
        }
    }

    public class PlayerViewModel : ReactiveObject
    {
        bool applyDeEmphasis;
        bool copyAllowed;
        bool hiddenTrack;
        bool isAudioTrack;
        bool isDataTrack;
        bool trackHasEmphasis;

        public bool ApplyDeEmphasis
        {
            get => applyDeEmphasis;
            set => this.RaiseAndSetIfChanged(ref applyDeEmphasis, value);
        }

        public bool TrackHasEmphasis
        {
            get => trackHasEmphasis;
            set => this.RaiseAndSetIfChanged(ref trackHasEmphasis, value);
        }

        public bool HiddenTrack
        {
            get => hiddenTrack;
            set => this.RaiseAndSetIfChanged(ref hiddenTrack, value);
        }

        public bool CopyAllowed
        {
            get => copyAllowed;
            set => this.RaiseAndSetIfChanged(ref copyAllowed, value);
        }

        public bool IsAudioTrack
        {
            get => isAudioTrack;
            set => this.RaiseAndSetIfChanged(ref isAudioTrack, value);
        }

        public bool IsDataTrack
        {
            get => isDataTrack;
            set => this.RaiseAndSetIfChanged(ref isDataTrack, value);
        }
    }
}