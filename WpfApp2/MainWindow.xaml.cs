using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AnimeApp
{
	public partial class MainWindow : Window
	{
		private const int itemsPerPage = 10; // Количество тайтлов
		private const int maxDescriptionLength = 80; // Максимальная длина описания
		private Stack<UIElement> navigationStack = new Stack<UIElement>();

		// Коллекция для хранения ID аниме, отображенных на главной странице
		private HashSet<int> mainPageAnimeIds = new HashSet<int>();

		public MainWindow()
		{
			InitializeComponent();
			LoadPopularTitles();
		}

		private async void LoadPopularTitles()
		{
			try
			{
				var popularTitles = await GetPopularTitles();
				EpisodesControl.ItemsSource = popularTitles;

				// Добавляем ID аниме в коллекцию mainPageAnimeIds
				foreach (var anime in popularTitles)
				{
					mainPageAnimeIds.Add(anime.Id);
				}

				ShowInitialPanel();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка загрузки популярных тайтлов: {ex.Message}");
			}
		}

		private async Task<List<Anime>> GetPopularTitles()
		{
			return await FetchAnimeData("POPULARITY_DESC", null);
		}

		private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				if (string.IsNullOrWhiteSpace(SearchBox.Text) || SearchBox.Text == "Введите название аниме...")
				{
					// Если запрос пустой, возвращаем пользователя на главную страницу
					LoadPopularTitles();
					return;
				}

				try
				{
					var searchQuery = SearchBox.Text;
					var animeList = await SearchAnime(searchQuery);

					// Фильтруем результаты поиска, исключая аниме, которые есть на главной странице
					var filteredAnimeList = animeList.Where(anime => !mainPageAnimeIds.Contains(anime.Id)).ToList();

					// Заменяем содержимое EpisodesControl результатами поиска
					EpisodesControl.ItemsSource = filteredAnimeList;
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Ошибка поиска: {ex.Message}");
				}
			}
		}

		private async Task<List<Anime>> SearchAnime(string searchQuery)
		{
			return await FetchAnimeData(null, searchQuery);
		}

		private async Task<List<Anime>> FetchAnimeData(string sortType, string searchQuery)
		{
			string url = "https://graphql.anilist.co";
			string query = @"
                    query ($page: Int, $perPage: Int, $sort: [MediaSort], $search: String) {
                      Page(page: $page, perPage: $perPage) {
                        media(type: ANIME, sort: $sort, search: $search) {
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
				perPage = itemsPerPage,
				sort = sortType != null ? new[] { sortType } : null,
				search = searchQuery
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

				var animeList = new List<Anime>();
				foreach (var item in data)
				{
					var anime = item.ToObject<Anime>();
					anime.Description = Regex.Replace(anime.Description, "<.*?>", string.Empty).Trim();

					if (anime.Description.Length > maxDescriptionLength)
					{
						anime.Description = anime.Description.Substring(0, maxDescriptionLength) + "...";
					}

					animeList.Add(anime);
				}

				return animeList;
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
			if (anime != null)
			{
				ShowAnimeDetails(anime);
			}
		}

		private void ShowAnimeDetails(Anime anime)
		{
			DetailsPanel.Visibility = Visibility.Visible;
			SearchResultsPanel.Visibility = Visibility.Collapsed;
			MenuPanel.Visibility = Visibility.Collapsed;
			navigationStack.Push(DetailsPanel);

			AnimeTitle.Text = anime.Title.Romaji;
			AnimeDescription.Text = anime.Description;
			AnimeCoverImage.Source = new BitmapImage(new Uri(anime.CoverImage.Large));

			EpisodesList.ItemsSource = anime.StreamingEpisodes;
		}

		private void ShowInitialPanel()
		{
			DetailsPanel.Visibility = Visibility.Collapsed;
			SearchResultsPanel.Visibility = Visibility.Visible;
			MenuPanel.Visibility = Visibility.Visible; // Оставляем кнопку меню видимой
			navigationStack.Push(SearchResultsPanel);
		}

		private void HomeButton_Click(object sender, RoutedEventArgs e)
		{
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
				LoadPopularTitles(); // Возвращаемся на главную страницу, если больше некуда возвращаться
			}
		}

		private void MenuButton_Click(object sender, RoutedEventArgs e)
		{
			if (MenuPanel.Visibility == Visibility.Collapsed)
			{
				MenuPanel.Visibility = Visibility.Visible;
				MenuPanel.Width = 300;
				MenuButton.Visibility = Visibility.Visible; // Оставляем кнопку меню видимой
				Grid.SetColumnSpan(SearchPanel, 1); // Сдвиг поиска влево
			}
			else
			{
				MenuPanel.Visibility = Visibility.Collapsed;
				MenuPanel.Width = 0;
				MenuButton.Visibility = Visibility.Visible; // Оставляем кнопку меню видимой
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
			MenuButton.Visibility = Visibility.Visible; // Показать кнопку меню
			Grid.SetColumnSpan(SearchPanel, 2); // Вернуть поиск на место
		}

		// Обработчик для кнопки случайного аниме
		private async void RandomButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var randomAnime = await GetRandomAnime();
				if (randomAnime != null)
				{
					EpisodesControl.ItemsSource = new List<Anime> { randomAnime };
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка загрузки случайного тайтла: {ex.Message}");
			}
		}

		private async Task<Anime> GetRandomAnime()
		{
			var random = new Random();
			int randomPage = random.Next(1, 100); // Случайная страница, например от 1 до 100
			var animeList = await FetchAnimeData("POPULARITY_DESC", null, randomPage);
			return animeList[random.Next(animeList.Count)];
		}

		private async Task<List<Anime>> FetchAnimeData(string sortType, string searchQuery, int page = 1)
		{
			string url = "https://graphql.anilist.co";
			string query = @"
                    query ($page: Int, $perPage: Int, $sort: [MediaSort], $search: String) {
                      Page(page: $page, perPage: $perPage) {
                        media(type: ANIME, sort: $sort, search: $search) {
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
				page,
				perPage = itemsPerPage,
				sort = sortType != null ? new[] { sortType } : null,
				search = searchQuery
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

				var animeList = new List<Anime>();
				foreach (var item in data)
				{
					var anime = item.ToObject<Anime>();
					anime.Description = Regex.Replace(anime.Description, "<.*?>", string.Empty).Trim();

					if (anime.Description.Length > maxDescriptionLength)
					{
						anime.Description = anime.Description.Substring(0, maxDescriptionLength) + "...";
					}

					animeList.Add(anime);
				}

				return animeList;
			}
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
