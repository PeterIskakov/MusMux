using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MusMux
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private ObservableCollection<SongItem> Songs;
        private int SongIndex;
        private MediaPlayer SongPlayer;

        public MainWindow()
        {
            SongIndex = -1;
            InitializeComponent();
            Songs = new();
            SongPlayer = new();
            SongPlayer.MediaEnded += SongPlayer_MediaEnded;
            BaseExample.ItemsSource = Songs;

            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
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

        private void SongPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            DispatcherQueue.TryEnqueue(async () =>
            {
                PauseSong();
                int selection = await new SelectionPopup().ShowAsync();
                SongIndex = selection;
                SongPlayer.Source = MediaSource.CreateFromUri(new Uri(Songs[SongIndex].Path));
                PlaySong();
            });            
        }

        private void MainPlay_Click(object sender, RoutedEventArgs e)
        {
            if (SongIndex == -1) return;
            if (SongPlayer.CurrentState == MediaPlayerState.Playing)
            {
                PauseSong();
                return;
            }
            PlaySong();
        }

        private void MainPrev_Click(object sender, RoutedEventArgs e)
        {
            if (SongIndex == -1) return;
            if (SongIndex - 1 >= 0) SongIndex--;
            SongPlayer.Source = MediaSource.CreateFromUri(new Uri(Songs[SongIndex].Path));
            PlaySong();
        }

        private void MainNext_Click(object sender, RoutedEventArgs e)
        {
            if (SongIndex == -1) return;
            if (SongIndex + 1 < Songs.Count) SongIndex++;
            SongPlayer.Source = MediaSource.CreateFromUri(new Uri(Songs[SongIndex].Path));
            PlaySong();
        }

        private void SongPlay_Click(object sender, RoutedEventArgs e)
        {
            Button s = (Button)sender;
            SongItem i = (SongItem)s.DataContext;
            SongIndex = Songs.IndexOf(i);
            SongPlayer.Source = MediaSource.CreateFromUri(new Uri(Songs[SongIndex].Path));

            PlaySong();
        }

        private async void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            string[] supportedExtensions = { ".mp3", ".flac" };

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
                        .Where(f => supportedExtensions.Contains(f.FileType.ToLower()))
                        .Select(f => f.Path)
                );
                foreach (string s in list)
                {
                    Songs.Add(new SongItem(s));
                }
                if (SongIndex == -1) SongIndex = 0;
            }

            //re-enable the button
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
            openPicker.FileTypeFilter.Add(".mp3");
            openPicker.FileTypeFilter.Add(".flac");

            // Open the picker for the user to pick a file
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
    }
}
