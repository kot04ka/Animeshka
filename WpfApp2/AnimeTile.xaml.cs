using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AnimeApp
{
    public partial class AnimeTile : UserControl
    {
        public AnimeTile()
        {
            InitializeComponent();
        }

        private void OnAnimeClick(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var anime = border?.DataContext as Anime;
            if (anime != null)
            {
                ((MainWindow)Application.Current.MainWindow).ShowAnimeDetails(anime);
            }
        }
    }
}
