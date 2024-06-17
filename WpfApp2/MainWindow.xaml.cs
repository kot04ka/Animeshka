using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;

namespace WpfApp2
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient client = new HttpClient();
        private List<Anime> allAnimeTitles = new List<Anime>();
        private DispatcherTimer searchTimer;

        private const string SearchGraphQLQuery = @"
        query ($search: String) {
          Page {
            media(search: $search, type: ANIME) {
              id
              title {
                romaji
                english
                native
              }
              coverImage {
                large
              }
              description
            }
          }
        }";

        private const string EpisodesGraphQLQuery = @"
        query ($id: Int) {
          Media(id: $id) {
            id
            title {
              romaji
              english
              native
            }
            streamingEpisodes {
              title
              thumbnail
              url
            }
          }
        }";

        public MainWindow()
        {
            InitializeComponent();
            searchTimer = new DispatcherTimer();
            searchTimer.Interval = TimeSpan.FromMilliseconds(300);
            searchTimer.Tick += SearchTimer_Tick;
        }

        private void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            searchTimer.Stop();
            searchTimer.Start();
        }

        private async void SearchTimer_Tick(object sender, EventArgs e)
        {
            searchTimer.Stop();
            string searchTerm = SearchTextBox.Text;
            if (!string.IsNullOrWhiteSpace(searchTerm) && searchTerm.Length >= 3)
            {
                await SearchAnime(searchTerm);
                FilterAnimeTitles(searchTerm);
            }
            else
            {
                ResultsListBox.Items.Clear();
            }
        }

        private void FilterAnimeTitles(string searchTerm)
        {
            searchTerm = searchTerm.ToLower();
            var filteredResults = allAnimeTitles
                .Where(a => a.Title.ToLower().Contains(searchTerm))
                .OrderBy(a => a.Title)
                .ToList();
            ResultsListBox.Items.Clear();
            foreach (var anime in filteredResults)
            {
                ResultsListBox.Items.Add(anime);
            }
        }

        private async Task SearchAnime(string searchTerm)
        {
            try
            {
                var variables = new { search = searchTerm };

                var content = new StringContent(
                    Newtonsoft.Json.JsonConvert.SerializeObject(new { query = SearchGraphQLQuery, variables }),
                    Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://graphql.anilist.co", content);
                var responseString = await response.Content.ReadAsStringAsync();

                var jsonResponse = JObject.Parse(responseString);

                var pageData = jsonResponse["data"]?["Page"];
                if (pageData == null || pageData["media"] == null || pageData["media"].Type != JTokenType.Array)
                {
                    throw new Exception("Unexpected JSON structure: 'media' is not an array.");
                }

                var results = (JArray)pageData["media"];
                allAnimeTitles.Clear();

                var tasks = results.Select(async anime =>
                {
                    var animeObj = new Anime
                    {
                        Id = anime["id"]?.ToString() ?? "",
                        Title = anime["title"]?["romaji"]?.ToString() ?? "No Title",
                        CoverImage = anime["coverImage"]?["large"]?.ToString() ?? "",
                        Description = anime["description"]?.ToString() ?? "No Description"
                    };

                    if (await HasEpisodes(animeObj))
                    {
                        allAnimeTitles.Add(animeObj);
                    }
                });

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching anime: {ex.Message}");
                Console.WriteLine("Error searching anime:");
                Console.WriteLine(ex.ToString());
            }
        }

        private async Task<bool> HasEpisodes(Anime anime)
        {
            var episodes = await LoadEpisodes(anime.Id, anime.Title, checkOnly: true);
            return episodes != null && episodes.Count > 0 && episodes[0].Title != "No episodes available";
        }

        private async Task<List<Episode>> LoadEpisodes(string animeId, string animeTitle, bool checkOnly = false)
        {
            try
            {
                var variables = new { id = int.Parse(animeId) };

                var content = new StringContent(
                    Newtonsoft.Json.JsonConvert.SerializeObject(new { query = EpisodesGraphQLQuery, variables }),
                    Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://graphql.anilist.co", content);
                var responseString = await response.Content.ReadAsStringAsync();

                var jsonResponse = JObject.Parse(responseString);

                var mediaData = jsonResponse["data"]?["Media"];
                if (mediaData == null)
                {
                    throw new Exception("Unexpected JSON structure: 'Media' is not present.");
                }

                var episodesToken = mediaData["streamingEpisodes"];
                var episodes = new List<Episode>();

                if (episodesToken != null && episodesToken.Type == JTokenType.Array && episodesToken.Any())
                {
                    episodes = episodesToken.ToObject<List<Episode>>();
                }
                else
                {
                    episodes = await SearchEpisodesJikan(animeTitle);

                    if (episodes.Count == 0)
                    {
                        episodes.Add(new Episode { Title = "No episodes available" });
                    }
                }

                if (checkOnly)
                {
                    return episodes;
                }

                EpisodesListBox.ItemsSource = episodes;
                return episodes;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading episodes:");
                Console.WriteLine(ex.ToString());
                if (!checkOnly)
                {
                    MessageBox.Show($"Error loading episodes: {ex.Message}");
                }
                return new List<Episode> { new Episode { Title = "No episodes available" } };
            }
        }

        private async Task<List<Episode>> SearchEpisodesJikan(string animeTitle)
        {
            var episodes = new List<Episode>();
            try
            {
                var response = await client.GetStringAsync($"https://api.jikan.moe/v3/search/anime?q={animeTitle}&limit=1");
                var jsonResponse = JObject.Parse(response);

                var animeId = jsonResponse["results"]?.First()?["mal_id"]?.ToString();
                if (animeId != null)
                {
                    var episodesResponse = await client.GetStringAsync($"https://api.jikan.moe/v3/anime/{animeId}/episodes");
                    var episodesJson = JObject.Parse(episodesResponse);

                    var episodesArray = episodesJson["episodes"]?.ToObject<JArray>();
                    if (episodesArray != null)
                    {
                        episodes = episodesArray.Select(ep => new Episode
                        {
                            Title = ep["title"]?.ToString() ?? "No Title",
                            Url = ep["video_url"]?.ToString() ?? "",
                            Thumbnail = ep["image_url"]?.ToString() ?? ""
                        }).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading episodes from Jikan API:");
                Console.WriteLine(ex.ToString());
            }
            return episodes;
        }

        private async void ResultsListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ResultsListBox.SelectedItem is Anime selectedAnime)
            {
                ShowDetailView(selectedAnime);
                await LoadEpisodes(selectedAnime.Id, selectedAnime.Title);
            }
        }

        private void ShowDetailView(Anime selectedAnime)
        {
            DetailTitleTextBlock.Text = selectedAnime.Title;
            DetailCoverImage.Source = new BitmapImage(new Uri(selectedAnime.CoverImage));
            DetailDescriptionTextBlock.Text = selectedAnime.Description;

            SearchGrid.Visibility = Visibility.Collapsed;
            DetailGrid.Visibility = Visibility.Visible;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            DetailGrid.Visibility = Visibility.Collapsed;
            SearchGrid.Visibility = Visibility.Visible;
        }
    }

    public class Anime
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string CoverImage { get; set; }
        public string Description { get; set; }
    }

    public class Episode
    {
        public string Title { get; set; }
        public string Thumbnail { get; set; }
        public string Url { get; set; }
    }
}
