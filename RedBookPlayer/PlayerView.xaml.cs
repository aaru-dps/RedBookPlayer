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
        public PlayerView()
        {
            InitializeComponent(null);
        }

        public PlayerView(string xaml)
        {
            InitializeComponent(xaml);
        }

        public static Player player = new Player();
        TextBlock currentTrack;
        Image[] digits;
        Timer updateTimer;

        public async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            string path = await GetPath();

            if (path == null)
            {
                return;
            }

            await Task.Run(() =>
            {
                AaruFormat image = new AaruFormat();
                IFilter filter = new ZZZNoFilter();
                filter.Open(path);
                image.Open(filter);

                player?.Shutdown();
                player = new Player();
                player.Init(image);
            });

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MainWindow.Instance.Title = "RedBookPlayer - " + path.Split('/').Last().Split('\\').Last();
            });
        }

        public async Task<string> GetPath()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.AllowMultiple = false;

            List<string> knownExtensions = (new Aaru.DiscImages.AaruFormat()).KnownExtensions.ToList();
            dialog.Filters.Add(new FileDialogFilter()
            {
                Name = "Aaru Image Format (*" + string.Join(", *", knownExtensions) + ")",
                Extensions = knownExtensions.ConvertAll(e => e.Substring(1))
            }
            );

            return (await dialog.ShowAsync((Window)this.Parent.Parent))?.FirstOrDefault();
        }

        public void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            player.Play();
        }

        public void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            player.Pause();
        }

        public void StopButton_Click(object sender, RoutedEventArgs e)
        {
            player.Stop();
        }

        public void NextTrackButton_Click(object sender, RoutedEventArgs e)
        {
            player.NextTrack();
        }

        public void PreviousTrackButton_Click(object sender, RoutedEventArgs e)
        {
            player.PreviousTrack();
        }

        public void NextIndexButton_Click(object sender, RoutedEventArgs e)
        {
            player.NextIndex();
        }

        public void PreviousIndexButton_Click(object sender, RoutedEventArgs e)
        {
            player.PreviousIndex();
        }

        public void FastForwardButton_Click(object sender, RoutedEventArgs e)
        {
            player.FastForward();
        }

        public void RewindButton_Click(object sender, RoutedEventArgs e)
        {
            player.Rewind();
        }

        public void EnableDeEmphasisButton_Click(object sender, RoutedEventArgs e)
        {
            player.EnableDeEmphasis();
        }

        public void DisableDeEmphasisButton_Click(object sender, RoutedEventArgs e)
        {
            player.DisableDeEmphasis();
        }

        private void UpdateView(object sender, ElapsedEventArgs e)
        {
            if (player.Initialized)
            {
                string track = (player.CurrentTrack + 1).ToString().PadLeft(2, '0');
                string index = (player.CurrentIndex).ToString().PadLeft(2, '0');
                string frames = (player.CurrentSector % 75).ToString().PadLeft(2, '0');
                string seconds = ((player.CurrentSector / 75) % 60).ToString().PadLeft(2, '0');
                string minutes = ((player.CurrentSector / (75 * 60)) % 60).ToString().PadLeft(2, '0');

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    int i = 0;
                    foreach (char digit in track + index + minutes + seconds + frames)
                        if (digits[i] != null)
                            digits[i++].Source = GetBitmap(digit);

                    ((PlayerViewModel)DataContext).PreEmphasis = player.HasPreEmphasis;
                });
            }
            else
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (Image digit in digits)
                    {
                        if (digit != null)
                            digit.Source = GetBitmap('-');
                    }
                });
            }
        }

        private Bitmap GetBitmap(char character)
        {
            if (App.Settings.SelectedTheme == "default")
            {
                IAssetLoader assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                return new Bitmap(assets.Open(new Uri($"avares://RedBookPlayer/Assets/{character}.png")));
            }
            else
            {
                string themeDirectory = Directory.GetCurrentDirectory() + "/themes/" + App.Settings.SelectedTheme;
                Bitmap bitmap;
                using (FileStream stream = File.Open(themeDirectory + $"/{character}.png", FileMode.Open))
                {
                    bitmap = new Bitmap(stream);
                }
                return bitmap;
            }
        }

        public void Initialize()
        {
            digits = new Image[10];

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

            currentTrack = this.FindControl<TextBlock>("CurrentTrack");
        }

        private void InitializeComponent(string xaml)
        {
            DataContext = new PlayerViewModel();

            if (xaml != null)
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
                catch (Exception ex)
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
        private bool preEmphasis;
        public bool PreEmphasis
        {
            get => preEmphasis;
            set => this.RaiseAndSetIfChanged(ref preEmphasis, value);
        }
    }
}