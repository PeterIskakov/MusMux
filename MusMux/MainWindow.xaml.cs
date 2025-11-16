using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Timers;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.WindowManagement;

namespace MusMux
{
    public sealed partial class MainWindow : Window
    {
        private readonly string[] SupportedExtensions = // Only types that I tested, other file types may work too.
            [ 
            ".aac", 
            ".flac", 
            ".m4a", 
            ".mp3", 
            ".ogg", 
            ".wav", 
            ".wma"
            ]; 
        private const string PauseGlyph = "\uE769";
        private const string PlayGlyph = "\uE768";

        private ObservableCollection<SongItem> Songs;
        private MediaPlayer SongPlayer;
        private Timer SliderTimer;

        private int SongIndex;
        private bool Seeking;

        public MainWindow()
        {
            Seeking = false;
            SongIndex = -1;

            Songs = new();

            InitializeComponent();

            LoadSongList();

            SongPlayer = new();
            SongPlayer.MediaEnded += SongPlayer_MediaEnded;

            SliderTimer = new(500);
            SliderTimer.Elapsed += SliderTimer_Elapsed;
            SliderTimer.Start();

            // PointerEvents must be assinged this way or else they won't fire on the SongSlider for some reason...
            SongSlider.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(SongSlider_PointerPressed), true);
            SongSlider.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(SongSlider_PointerReleased), true);
            SongSlider.ThumbToolTipValueConverter = new SliderTimeConverter();

            OverlappedPresenter presenter = OverlappedPresenter.Create();
            presenter.PreferredMinimumWidth = 500;
            presenter.PreferredMinimumHeight = 400;

            AppWindow.SetPresenter(presenter);
            AppWindow.Resize(new(800, 600));
            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        }

        // Helper functions

        private MediaSource CurrentSongSource()
        {
            return MediaSource.CreateFromUri(new Uri(Songs[SongIndex].Path));
        }

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
            FrameworkElement fe = (FrameworkElement)container.ContentTemplateRoot;
            Button btn = (Button)fe.FindName("SongPlay");
            FontIcon ico = new()
            {
                Glyph = PlayGlyph
            };
            if (playing)
            {
                ico.Glyph = PauseGlyph;
            }
            btn.Content = ico;
        }

        private async void SaveSongList()
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("SongList.json", CreationCollisionOption.ReplaceExisting);

            List<string> paths = Songs.Select(i => i.Path).ToList();

            string json = JsonSerializer.Serialize(paths);
            await FileIO.WriteTextAsync(file, json);
        }

        private async void LoadSongList()
        {
            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync("SongList.json");

                string json = await FileIO.ReadTextAsync(file);
                List<string>? songlist = JsonSerializer.Deserialize<List<string>>(json);

                if (songlist == null) throw new Exception();

                foreach (string s in songlist)
                {
                    Songs.Add(new(s));
                }

                if (Songs.Count > 0) 
                { 
                    SongIndex = 0;
                    SongPlayer.Source = CurrentSongSource();
                }
            }
            catch (Exception)
            {
                // First launch
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

                if (LoopToggle.IsChecked == false)
                {
                    int selection = await new SelectionPopup(Songs.ToList()).SelectSong();
                    if (selection == -1) return;
                    SongIndex = selection;
                    SongPlayer.Source = CurrentSongSource();
                }

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
            SongPlayer.Source = CurrentSongSource();

            SongPlayIndicator(true);
            PlaySong();
        }

        private void MainNext_Click(object sender, RoutedEventArgs e)
        {
            if (SongIndex == -1) return;
            SongPlayIndicator(false);
            if (SongIndex + 1 < Songs.Count) SongIndex++;
            SongPlayer.Source = CurrentSongSource();

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
                    ico.Glyph = PlayGlyph;
                    s.Content = ico;
                    PauseSong();
                }
                else
                {
                    ico.Glyph = PauseGlyph;
                    s.Content = ico;
                    PlaySong();
                }
                return;
            }

            SongPlayIndicator(false);

            SongIndex = i;
            SongPlayer.Source = CurrentSongSource();

            FontIcon ic = new()
            {
                Glyph = PauseGlyph
            };
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
                List<string> list = new(files.Where(f => SupportedExtensions.Contains(f.FileType.ToLower())).Select(f => f.Path));
                foreach (string s in list)
                {
                    SongItem song = new(s);
                    if (Songs.Contains(song)) continue;
                    Songs.Add(song);
                }
                if (SongIndex == -1)
                {
                    SongIndex = 0;
                    SongPlayer.Source = CurrentSongSource();
                }

                SaveSongList();
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
            openPicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;

            foreach (string s in SupportedExtensions)
                openPicker.FileTypeFilter.Add(s);

            IReadOnlyList<StorageFile> files = await openPicker.PickMultipleFilesAsync();
            if (files.Count > 0)
            {
                foreach (StorageFile file in files)
                {
                    SongItem song = new(file.Path);
                    if (Songs.Contains(song)) continue;
                    Songs.Add(song);
                }
                if (SongIndex == -1)
                {
                    SongIndex = 0;
                    SongPlayer.Source = CurrentSongSource();
                }

                SaveSongList();
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

        private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (SongPlayer == null) return;
            SongPlayer.Volume = ((double) e.NewValue) / 100d;

            FontIcon ico = new()
            {
                Glyph = "\uE767"
            };

            if (e.NewValue == 0) ico.Glyph = "\uE74F";
            else if (e.NewValue > 0 && e.NewValue <= 33) ico.Glyph = "\uE993";
            else if (e.NewValue > 33 && e.NewValue <= 66) ico.Glyph = "\uE994";

            VolumeButton.Icon = ico;
        }

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            List<SongItem> sel = SongListView.SelectedItems.Cast<SongItem>().ToList();

            foreach (SongItem item in sel)
            {
                if (Songs.IndexOf(item) == SongIndex)
                {
                    PauseSong();
                    SongIndex--;
                    if (SongIndex != -1)
                        SongPlayer.Source = CurrentSongSource();
                }
                Songs.Remove(item);
            }

            if (SongIndex >= Songs.Count) 
            {
                PauseSong();
                SongIndex = Songs.Count - 1;
                if (SongIndex != -1)
                    SongPlayer.Source = CurrentSongSource();
            }

            SaveSongList();
        }

        private void SongListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RemoveSelected.IsEnabled = SongListView.SelectedItems.Any();
        }
    }
}
