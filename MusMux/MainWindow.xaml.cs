using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Timers;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace MusMux
{
    public sealed partial class MainWindow : Window
    {
        private readonly string[] SupportedExtensions = [".mp3", ".flac"];

        private ObservableCollection<SongItem> Songs;
        private MediaPlayer SongPlayer;
        private Timer SliderTimer;

        private int SongIndex;
        private bool Seeking;

        public MainWindow()
        {
            Seeking = false;
            SongIndex = -1;
            InitializeComponent();
            Songs = new();
            SongPlayer = new();
            SongPlayer.MediaEnded += SongPlayer_MediaEnded;
            SliderTimer = new(500);
            SliderTimer.Elapsed += SliderTimer_Elapsed;
            SliderTimer.Start();
            SongListView.ItemsSource = Songs;
            
            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        }

        // Helper functions

        private void PauseSong()
        {
            MainPlay.Icon = new SymbolIcon(Symbol.Play);
            MainPlay.Label = "Play";
            SongPlayer.Pause();
        }

        private void PlaySong()
        {
            MainPlay.Icon = new SymbolIcon(Symbol.Pause);
            MainPlay.Label = "Pause";
            SongPlayer.Play();
        }

        private void SongPlayIndicator(bool playing)
        {
            ListViewItem container = (ListViewItem)SongListView.ContainerFromIndex(SongIndex);
            if (container != null)
            {
                FrameworkElement fe = (FrameworkElement)container.ContentTemplateRoot;
                Button btn = (Button)fe.FindName("SongPlay");
                FontIcon ico = new();

                ico.Glyph = "\uE768";
                if (playing)
                {
                    ico.Glyph = "\uE769";
                } 
                btn.Content = ico;
            }
        }

        // Event handler functions

        private void SliderTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (SongPlayer.CurrentState != MediaPlayerState.Playing) return;
            if (Seeking) return;

            MediaPlaybackSession session = SongPlayer.PlaybackSession;
            double position = session.Position.TotalSeconds;
            double duration = session.NaturalDuration.TotalSeconds;

            if (duration <= 0) return;

            DispatcherQueue.TryEnqueue(() =>
            {
                SongSlider.Maximum = duration;
                SongSlider.Value = position;
            });
        }

        private void SongPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            DispatcherQueue.TryEnqueue(async () =>
            {
                PauseSong();
                SongPlayIndicator(false);
                int selection = await new SelectionPopup(Songs.ToList()).SelectSong();
                SongIndex = selection;
                SongPlayer.Source = MediaSource.CreateFromUri(new Uri(Songs[SongIndex].Path));

                SongPlayIndicator(true);
                PlaySong();
            });            
        }

        private void MainPlay_Click(object sender, RoutedEventArgs e)
        {
            if (SongIndex == -1) return;
            if (SongPlayer.CurrentState == MediaPlayerState.Playing)
            {
                SongPlayIndicator(false);
                PauseSong();
                return;
            }
            SongPlayIndicator(true);
            PlaySong();
        }

        private void MainPrev_Click(object sender, RoutedEventArgs e)
        {
            if (SongIndex == -1) return;
            SongPlayIndicator(false);
            if (SongIndex - 1 >= 0) SongIndex--;
            SongPlayer.Source = MediaSource.CreateFromUri(new Uri(Songs[SongIndex].Path));

            SongPlayIndicator(true);
            PlaySong();
        }

        private void MainNext_Click(object sender, RoutedEventArgs e)
        {
            if (SongIndex == -1) return;
            SongPlayIndicator(false);
            if (SongIndex + 1 < Songs.Count) SongIndex++;
            SongPlayer.Source = MediaSource.CreateFromUri(new Uri(Songs[SongIndex].Path));

            SongPlayIndicator(true);
            PlaySong();
        }

        private void SongPlay_Click(object sender, RoutedEventArgs e)
        {
            Button s = (Button)sender;
            SongItem si = (SongItem)s.DataContext;
            int i = Songs.IndexOf(si);

            if (i == SongIndex)
            {
                FontIcon ico = new();
                if (SongPlayer.CurrentState == MediaPlayerState.Playing)
                {
                    ico.Glyph = "\uE768";
                    s.Content = ico;
                    PauseSong();
                }
                else
                {
                    ico.Glyph = "\uE769";
                    s.Content = ico;
                    PlaySong();
                }
                return;
            }

            SongPlayIndicator(false);

            SongIndex = i;
            SongPlayer.Source = MediaSource.CreateFromUri(new Uri(Songs[SongIndex].Path));

            FontIcon ic = new();
            ic.Glyph = "\uE769";
            s.Content = ic;
            PlaySong();
        }

        private async void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            AppBarButton senderButton = (AppBarButton)sender;
            senderButton.IsEnabled = false;

            FolderPicker openPicker = new();

            nint hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            openPicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            openPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await openPicker.PickSingleFolderAsync();
            
            if (folder != null)
            {
                IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();
                List<string> list = new();
                list.AddRange(
                    files
                        .Where(f => SupportedExtensions.Contains(f.FileType.ToLower()))
                        .Select(f => f.Path)
                );
                foreach (string s in list)
                {
                    if (Songs.Contains(new SongItem(s))) continue;
                    Songs.Add(new SongItem(s));
                }
                if (SongIndex == -1) SongIndex = 0;
            }

            senderButton.IsEnabled = true;
        }

        private async void AddFile_Click(object sender, RoutedEventArgs e)
        {
            AppBarButton senderButton = (AppBarButton)sender;
            senderButton.IsEnabled = false;

            FileOpenPicker openPicker = new();

            nint hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            openPicker.ViewMode = PickerViewMode.List;
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            foreach (string s in SupportedExtensions)
                openPicker.FileTypeFilter.Add(s);

            IReadOnlyList<StorageFile> files = await openPicker.PickMultipleFilesAsync();
            if (files.Count > 0)
            {
                foreach (StorageFile file in files)
                {
                    Songs.Add(new SongItem(file.Path));
                }
                if (SongIndex == -1) SongIndex = 0;
            }

            senderButton.IsEnabled = true;
        }

        private void SongSlider_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Seeking = true;
        }

        private void SongSlider_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            SongPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(SongSlider.Value);
            Seeking = false;
        }
    }
}
