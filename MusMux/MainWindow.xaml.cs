using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MusMux
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private List<SongItem> Songs;
        private int CurrentSong;
        private MediaPlayer SongPlayer;
        public MainWindow()
        {
            CurrentSong = 0;
            InitializeComponent();
            Songs = [
                new (@"C:\Users\Peter\Music\tron.mp3"),
                new (@"C:\Users\Peter\Music\clocks.mp3")
                ];
            SongPlayer = new();
            SongPlayer.MediaEnded += SongPlayer_MediaEnded;
            SongPlayer.Source = MediaSource.CreateFromUri(new Uri(Songs[CurrentSong].Path));
            BaseExample.ItemsSource = Songs;
        }

        private void SongPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            MainPlay.Icon = new SymbolIcon(Symbol.Play);
            MainPlay.Label = "Play";
            SongPlayer.Pause();
            SongPlayer.AutoPlay = false;
        }

        private void MainPlay_Click(object sender, RoutedEventArgs e)
        {
            AppBarButton s = (AppBarButton)sender;
            if (SongPlayer.CurrentState == MediaPlayerState.Playing)
            {
                s.Icon = new SymbolIcon(Symbol.Play);
                s.Label = "Play";
                SongPlayer.Pause();
                SongPlayer.AutoPlay = false;
                return;
            }
            s.Icon = new SymbolIcon(Symbol.Pause);
            s.Label = "Pause";
            SongPlayer.Play();
            SongPlayer.AutoPlay = true;
        }

        private void MainPrev_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentSong - 1 >= 0) CurrentSong--;
            SongPlayer.Source = MediaSource.CreateFromUri(new Uri(Songs[CurrentSong].Path));
        }

        private void MainNext_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentSong + 1 < Songs.Count) CurrentSong++;
            SongPlayer.Source = MediaSource.CreateFromUri(new Uri(Songs[CurrentSong].Path));
        }
    }
}
