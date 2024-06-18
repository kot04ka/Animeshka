using System;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace AnimeApp
{
    public partial class AnimeDetails : UserControl
    {
        public AnimeDetails()
        {
            InitializeComponent();
        }

        public void ShowDetails(Anime anime)
        {
            AnimeTitle.Text = anime.Title.Romaji;
            AnimeDescription.Text = anime.Description;
            AnimeCoverImage.Source = new BitmapImage(new Uri(anime.CoverImage.Large));
            EpisodesList.ItemsSource = anime.StreamingEpisodes;
            Visibility = System.Windows.Visibility.Visible;
        }

        public void HideDetails()
        {
            Visibility = System.Windows.Visibility.Collapsed;
        }
    }
}
