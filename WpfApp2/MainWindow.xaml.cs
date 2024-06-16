using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace WpfApp2
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			LoadAnimeData();
		}

		private async void LoadAnimeData()
		{
			var animeList = await FetchAnimeData();
			AnimeListView.ItemsSource = animeList;
		}

		private async Task<List<Anime>> FetchAnimeData()
		{
			using (HttpClient client = new HttpClient())
			{
				client.BaseAddress = new Uri("https://api.anilibria.tv/v2/");
				HttpResponseMessage response = await client.GetAsync("getTitles?filter=id,names,russian,description,poster");

				if (response.IsSuccessStatusCode)
				{
					string jsonResponse = await response.Content.ReadAsStringAsync();
					var animeList = JsonConvert.DeserializeObject<List<Anime>>(jsonResponse);
					return animeList;
				}
				else
				{
					MessageBox.Show("Ошибка при загрузке данных.");
					return new List<Anime>();
				}
			}
		}
	}

	public class Anime
	{
		public int Id { get; set; }
		public string Russian { get; set; }
		public string Description { get; set; }
		public Poster Poster { get; set; }

		public string Name => Russian;
		public string ImageUrl => Poster?.Url;
	}

	public class Poster
	{
		public string Url { get; set; }
	}
}
