using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics;

namespace MusMux
{
    public sealed partial class SelectionPopup : Window
    {
        private const int WindowWidth = 500;
        private const int WindowHeight = 390;
        private const int EdgeOffset = 12;

        private ObservableCollection<SongItem> SongList;
        private List<SongItem> OriginalSongList;
        private TaskCompletionSource<int> _tcs = new();

        public SelectionPopup(List<SongItem> songs)
        {
            InitializeComponent();

            Random rnd = new();

            OriginalSongList = songs;
            SongList = new(OriginalSongList.OrderBy(x => rnd.Next()).Take(6));
            SongListView.ItemsSource = SongList;

            AppWindow.Resize(new SizeInt32(WindowWidth, WindowHeight));

            RectInt32? area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest)?.WorkArea;
            if (area != null)
            {
                AppWindow.Move(new PointInt32(
                    area.Value.Width - WindowWidth - EdgeOffset, 
                    area.Value.Height - WindowHeight - EdgeOffset));
            } else
            {
                AppWindow.Move(new PointInt32(0, 0));
            }

            OverlappedPresenter presenter = OverlappedPresenter.Create();

            presenter.IsAlwaysOnTop = true;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;
            presenter.SetBorderAndTitleBar(true, false);

            AppWindow.IsShownInSwitchers = false;
            AppWindow.SetPresenter(presenter);
        }

        /// <summary>
        /// Opens a selection window where the user clicks the next song they want.
        /// </summary>
        /// <returns>Index of chosen song or -1 if none is chosen.</returns>
        public async Task<int> SelectSong()
        {
            AppWindow.Show();
            return await _tcs.Task;
        }

        private void SongListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            SongItem s = (SongItem)e.ClickedItem;
            int i = OriginalSongList.IndexOf(s);

            _tcs.TrySetResult(i);
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            _tcs.TrySetResult(-1);
            Close();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            Random rnd = new();
            SongList.Clear();
            foreach (SongItem s in OriginalSongList.OrderBy(x => rnd.Next()).Take(6))
            {
                SongList.Add(s);
            }
        }
    }
}
