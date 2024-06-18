using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AnimeApp
{
    public partial class MainWindow : Window
    {
        private const int itemsPerPage = 10; // Number of titles per page
        private const int maxDescriptionLength = 80; // Maximum description length for preview
        private Stack<UIElement> navigationStack = new Stack<UIElement>();
        private HashSet<int> mainPageAnimeIds = new HashSet<int>();

        private SearchManager searchManager;

        public MainWindow()
        {
            InitializeComponent();
            searchManager = new SearchManager(itemsPerPage, maxDescriptionLength);
            LoadPopularTitles();
        }

        private async void LoadPopularTitles()
        {
            try
            {
                var popularTitles = await searchManager.GetPopularTitles();
                EpisodesControl.ItemsSource = popularTitles;

                foreach (var anime in popularTitles)
                {
                    mainPageAnimeIds.Add(anime.Id);
                }

                ShowInitialPanel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading popular titles: {ex.Message}");
            }
        }

        private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await PerformSearch();
            }
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text == "Введите название аниме...")
            {
                SearchBox.Text = "";
                SearchBox.Foreground = new SolidColorBrush(Colors.White);
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchBox.Text = "Введите название аниме...";
                SearchBox.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await PerformSearch();
        }

        private async Task PerformSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text) || SearchBox.Text == "Введите название аниме...")
            {
                LoadPopularTitles();
                return;
            }

            try
            {
                var searchQuery = SearchBox.Text;
                var animeList = await searchManager.SearchAnime(searchQuery);

                var filteredAnimeList = animeList.Where(anime => !mainPageAnimeIds.Contains(anime.Id)).ToList();

                EpisodesControl.ItemsSource = filteredAnimeList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Search error: {ex.Message}");
            }
        }

        private void OnAnimeClick(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var anime = border.DataContext as Anime;
            if (anime != null)
            {
                ShowAnimeDetails(anime);
            }
        }

        public async void ShowAnimeDetails(Anime anime)
        {
            try
            {
                var fullAnime = await searchManager.GetAnimeDetails(anime.Id);

                DetailsPanel.Visibility = Visibility.Visible;
                SearchResultsPanel.Visibility = Visibility.Collapsed;
                RefreshButton.Visibility = Visibility.Collapsed; // Hide refresh button

                AnimeTitle.Text = fullAnime.Title.Romaji;
                AnimeDescription.Text = Regex.Replace(fullAnime.Description, "<.*?>", string.Empty);
                AnimeCoverImage.Source = new BitmapImage(new Uri(fullAnime.CoverImage.Large));

                var episodes = fullAnime.StreamingEpisodes.Select((ep, index) => new Episode
                {
                    Title = (index + 1).ToString(),
                    Url = ep.Url
                }).ToList();
                EpisodesList.ItemsSource = episodes;

                MenuButton.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading anime details: {ex.Message}");
            }
        }

        private void ShowInitialPanel()
        {
            DetailsPanel.Visibility = Visibility.Collapsed;
            SearchResultsPanel.Visibility = Visibility.Visible;
            RefreshButton.Visibility = Visibility.Visible; // Show refresh button
            MenuButton.Visibility = Visibility.Visible; // Ensure menu button is always visible
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            // Ensure menu button and panel states are reset
            CloseMenuPanel();
            LoadPopularTitles();
        }

        private void BackToPreviousPage()
        {
            if (navigationStack.Count > 1)
            {
                var currentPage = navigationStack.Pop();
                currentPage.Visibility = Visibility.Collapsed;
                var previousPage = navigationStack.Peek();
                previousPage.Visibility = Visibility.Visible;
            }
            else
            {
                LoadPopularTitles(); // Return to the main page if there's nowhere else to go back to
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (MenuPanel.Visibility == Visibility.Collapsed)
            {
                MenuPanel.Visibility = Visibility.Visible;
                MenuPanel.Width = 210;
                MenuButton.Visibility = Visibility.Collapsed; // Скрыть кнопку меню
                Grid.SetColumnSpan(SearchPanel, 1); // Сдвиг поиска влево
            }
            else
            {
                MenuPanel.Visibility = Visibility.Collapsed;
                MenuPanel.Width = 0;
                MenuButton.Visibility = Visibility.Visible; // Показать кнопку меню
                Grid.SetColumnSpan(SearchPanel, 2); // Вернуть поиск на место
            }
        }

        private void LogReg_Click(object sender, RoutedEventArgs e)
        {
            // Logic for Log/Reg
        }

        private void Favourites_Click(object sender, RoutedEventArgs e)
        {
            // Logic for Favourites
        }

        private void WatchTogether_Click(object sender, RoutedEventArgs e)
        {
            // Logic for Watch Together
        }

        private void Titles_Click(object sender, RoutedEventArgs e)
        {
            // Logic for Titles
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                BackToPreviousPage();
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == this)
            {
                CloseMenuPanel();
            }
        }

        private void CloseMenuPanel()
        {
            MenuPanel.Visibility = Visibility.Collapsed;
            MenuPanel.Width = 0;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var randomAnimeList = await searchManager.GetRandomAnimeList(itemsPerPage);
                if (randomAnimeList != null && randomAnimeList.Any())
                {
                    EpisodesControl.ItemsSource = randomAnimeList;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading random titles: {ex.Message}");
            }
        }
    }
}
