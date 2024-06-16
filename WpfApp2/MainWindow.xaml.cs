using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AnimeApp
{
    public partial class MainWindow : Window
    {
        private const int itemsPerPage = 10; // Количество тайтлов
        private const int maxDescriptionLength = 80; // Максимальная длина описания

        public MainWindow()
        {
            InitializeComponent();
            LoadNewestEpisodes();
        }

        private async void LoadNewestEpisodes()
        {
            try
            {
                var newEpisodes = await GetNewestEpisodes();
                EpisodesControl.ItemsSource = newEpisodes;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки новых эпизодов: {ex.Message}");
            }
        }

        private async Task<List<Anime>> GetNewestEpisodes()
        {
            string url = "https://graphql.anilist.co";
            string query = @"
                query ($page: Int, $perPage: Int) {
                  Page(page: $page, perPage: $perPage) {
                    media(type: ANIME, sort: TRENDING_DESC) {
                      id
                      title {
                        romaji
                      }
                      description(asHtml: false)
                      coverImage {
                        large
                      }
                      streamingEpisodes {
                        title
                        url
                      }
                    }
                  }
                }";

            var variables = new
            {
                page = 1,
                perPage = itemsPerPage
            };

            using (var client = new HttpClient())
            {
                var jsonContent = new StringContent(
                    new JObject
                    {
                        ["query"] = query,
                        ["variables"] = JObject.FromObject(variables)
                    }.ToString(), System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, jsonContent);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(jsonResponse)["data"]["Page"]["media"];

                var newEpisodes = new List<Anime>();
                foreach (var item in data)
                {
                    var anime = item.ToObject<Anime>();
                    anime.Description = Regex.Replace(anime.Description, "<.*?>", string.Empty).Trim();

                    if (anime.Description.Length > maxDescriptionLength)
                    {
                        anime.Description = anime.Description.Substring(0, maxDescriptionLength) + "...";
                    }

                    newEpisodes.Add(anime);
                }

                return newEpisodes;
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var searchQuery = SearchBox.Text == "Введите название аниме..." ? "" : SearchBox.Text;
                var animeList = await GetNewestEpisodes(); // Подразумеваем, что это обновленная версия для поиска
                EpisodesControl.ItemsSource = animeList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска: {ex.Message}");
            }
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text == "Введите название аниме...")
            {
                SearchBox.Text = "";
                SearchBox.Foreground = new SolidColorBrush(Colors.Black);
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

        private void OnAnimeClick(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var anime = border.DataContext as Anime;
            if (anime != null && anime.StreamingEpisodes != null && anime.StreamingEpisodes.Count > 0)
            {
                // Открыть первый эпизод для просмотра
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = anime.StreamingEpisodes[0].Url,
                    UseShellExecute = true
                });
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (MenuPanel.Visibility == Visibility.Collapsed)
            {
                MenuPanel.Visibility = Visibility.Visible;
                MenuPanel.Width = 300;
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
            // Логика для Log/Reg
        }

        private void Favourites_Click(object sender, RoutedEventArgs e)
        {
            // Логика для Favourites
        }

        private void WatchTogether_Click(object sender, RoutedEventArgs e)
        {
            // Логика для Watch Together
        }

        private void Titles_Click(object sender, RoutedEventArgs e)
        {
            // Логика для Titles
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CloseMenuPanel();
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
            MenuButton.Visibility = Visibility.Visible; // Показать кнопку меню
            Grid.SetColumnSpan(SearchPanel, 2); // Вернуть поиск на место
        }
    }

    public class Anime
    {
        public int Id { get; set; }
        public Title Title { get; set; }
        public string Description { get; set; }
        public CoverImage CoverImage { get; set; }
        public List<Episode> StreamingEpisodes { get; set; }
    }

    public class Title
    {
        public string Romaji { get; set; }
    }

    public class CoverImage
    {
        public string Large { get; set; }
    }

    public class Episode
    {
        public string Title { get; set; }
        public string Url { get; set; }
    }
}
