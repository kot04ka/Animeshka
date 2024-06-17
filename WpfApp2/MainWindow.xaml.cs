using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AnimeApp
{
	public partial class MainWindow : Window
	{
		private const int itemsPerPage = 10; // Number of titles per page
		private const int maxDescriptionLength = 80; // Maximum description length
		private Stack<UIElement> navigationStack = new Stack<UIElement>();

		// Collection to store IDs of anime displayed on the main page
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

				// Add anime IDs to the mainPageAnimeIds collection
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
				if (string.IsNullOrWhiteSpace(SearchBox.Text) || SearchBox.Text == "Введите название аниме...")
				{
					// If the query is empty, return the user to the main page
					LoadPopularTitles();
					return;
				}

				try
				{
					var searchQuery = SearchBox.Text;
					var animeList = await searchManager.SearchAnime(searchQuery);

					// Filter search results, excluding anime that are on the main page
					var filteredAnimeList = animeList.Where(anime => !mainPageAnimeIds.Contains(anime.Id)).ToList();

					// Replace the content of EpisodesControl with search results
					EpisodesControl.ItemsSource = filteredAnimeList;
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Search error: {ex.Message}");
				}
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
			MenuPanel.Visibility = Visibility.Visible; // Keep the menu button visible
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
				LoadPopularTitles(); // Return to the main page if there's nowhere else to go back to
			}
		}

		private void MenuButton_Click(object sender, RoutedEventArgs e)
		{
			if (MenuPanel.Visibility == Visibility.Collapsed)
			{
				MenuPanel.Visibility = Visibility.Visible;
				MenuPanel.Width = 300;
				MenuButton.Visibility = Visibility.Visible; // Keep the menu button visible
				Grid.SetColumnSpan(SearchPanel, 1); // Shift search to the left
			}
			else
			{
				MenuPanel.Visibility = Visibility.Collapsed;
				MenuPanel.Width = 0;
				MenuButton.Visibility = Visibility.Visible; // Keep the menu button visible
				Grid.SetColumnSpan(SearchPanel, 2); // Return search to its place
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
			MenuButton.Visibility = Visibility.Visible; // Show the menu button
			Grid.SetColumnSpan(SearchPanel, 2); // Return search to its place
		}

		private async void RandomButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var randomAnime = await searchManager.GetRandomAnime();
				if (randomAnime != null)
				{
					EpisodesControl.ItemsSource = new List<Anime> { randomAnime };
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error loading random title: {ex.Message}");
			}
		}
	}
}
