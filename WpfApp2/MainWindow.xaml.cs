using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AnimeApp
{
	public partial class MainWindow : Window
	{
		private int currentPage = 1;
		private const int itemsPerPage = 10;

		public MainWindow()
		{
			InitializeComponent();
			LoadAnime();
		}

		private async void LoadAnime()
		{
			try
			{
				var animeList = await GetAnimeList(currentPage);
				AnimeListView.ItemsSource = animeList;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error loading anime list: {ex.Message}");
			}
		}

		private async Task<List<Anime>> GetAnimeList(int page)
		{
			string url = "https://graphql.anilist.co";
			string query = @"
                query ($page: Int, $perPage: Int) {
                  Page(page: $page, perPage: $perPage) {
                    media(type: ANIME) {
                      id
                      title {
                        romaji
                      }
                      description
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
				page = page,
				perPage = itemsPerPage
			};

			using (var client = new HttpClient())
			{
				var jsonContent = new StringContent(
					new JObject
					{
						["query"] = query,
						["variables"] = JObject.FromObject(variables)
					}.ToString(),
					System.Text.Encoding.UTF8, "application/json");

				var response = await client.PostAsync(url, jsonContent);
				var responseString = await response.Content.ReadAsStringAsync();
				var data = JObject.Parse(responseString)["data"]["Page"]["media"];

				var animeList = new List<Anime>();
				foreach (var item in data)
				{
					var streamingEpisodes = new List<StreamingEpisode>();
					foreach (var episode in item["streamingEpisodes"])
					{
						streamingEpisodes.Add(new StreamingEpisode
						{
							Title = episode["title"].ToString(),
							Url = episode["url"].ToString()
						});
					}

					animeList.Add(new Anime
					{
						Title = item["title"]["romaji"].ToString(),
						Description = item["description"].ToString(),
						Image = item["coverImage"]["large"].ToString(),
						Episodes = streamingEpisodes
					});
				}
				return animeList;
			}
		}

		private void AnimeListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (AnimeListView.SelectedItem is Anime selectedAnime)
			{
				AnimeTitle.Text = selectedAnime.Title;
				AnimeDescription.Text = selectedAnime.Description;
				AnimeImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(selectedAnime.Image));
				EpisodesListBox.ItemsSource = selectedAnime.Episodes;
			}
		}

		private void EpisodesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (EpisodesListBox.SelectedItem is StreamingEpisode selectedEpisode)
			{
				try
				{
					var tabItem = ((TabControl)((Grid)((StackPanel)sender).Parent).Parent).SelectedItem as TabItem;
					var mediaElement = tabItem.Header.ToString() == "Плеер" ? AnimePlayerFull : AnimePlayer;

					mediaElement.Source = new Uri(selectedEpisode.Url);
					mediaElement.Play();
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Error playing episode: {ex.Message}");
				}
			}
		}

		private void PreviousPage_Click(object sender, RoutedEventArgs e)
		{
			if (currentPage > 1)
			{
				currentPage--;
				LoadAnime();
			}
		}

		private void NextPage_Click(object sender, RoutedEventArgs e)
		{
			currentPage++;
			LoadAnime();
		}
	}

	public class Anime
	{
		public string Title { get; set; }
		public string Description { get; set; }
		public string Image { get; set; }
		public List<StreamingEpisode> Episodes { get; set; }
	}

	public class StreamingEpisode
	{
		public string Title { get; set; }
		public string Url { get; set; }
	}
}
