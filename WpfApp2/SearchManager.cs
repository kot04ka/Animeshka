using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AnimeApp
{
    public class SearchManager
    {
        private const string apiUrl = "https://graphql.anilist.co";
        private int itemsPerPage;
        private int maxDescriptionLength;

        public SearchManager(int itemsPerPage, int maxDescriptionLength = 0)
        {
            this.itemsPerPage = itemsPerPage;
            this.maxDescriptionLength = maxDescriptionLength;
        }

        public async Task<List<Anime>> GetPopularTitles()
        {
            return await FetchAnimeData("POPULARITY_DESC", null);
        }

        public async Task<List<Anime>> SearchAnime(string searchQuery)
        {
            return await FetchAnimeData(null, searchQuery);
        }

        public async Task<List<Anime>> GetRandomAnimeList(int count)
        {
            var random = new Random();
            var randomAnimeList = new List<Anime>();

            for (int i = 0; i < count; i++)
            {
                int randomPage = random.Next(1, 100); // Random page, e.g., from 1 to 100
                var animeList = await FetchAnimeData("POPULARITY_DESC", null, randomPage);
                if (animeList.Any())
                {
                    randomAnimeList.Add(animeList[random.Next(animeList.Count)]);
                }
            }

            return randomAnimeList;
        }

        public async Task<Anime> GetAnimeDetails(int animeId)
        {
            string query = @"
                query ($id: Int) {
                  Media(id: $id) {
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
                }";

            var variables = new
            {
                id = animeId
            };

            using (var client = new HttpClient())
            {
                var jsonContent = new StringContent(
                    new JObject
                    {
                        ["query"] = query,
                        ["variables"] = JObject.FromObject(variables)
                    }.ToString(), System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync(apiUrl, jsonContent);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(jsonResponse)["data"]["Media"];

                var anime = data.ToObject<Anime>();
                anime.Description = Regex.Replace(anime.Description, "<.*?>", string.Empty).Trim();
                return anime;
            }
        }

        private async Task<List<Anime>> FetchAnimeData(string sortType, string searchQuery, int page = 1)
        {
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

                var response = await client.PostAsync(apiUrl, jsonContent);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(jsonResponse)["data"]["Page"]["media"];

                var animeList = new List<Anime>();
                foreach (var item in data)
                {
                    var anime = item.ToObject<Anime>();
                    anime.Description = Regex.Replace(anime.Description, "<.*?>", string.Empty).Trim();

                    anime.ShortDescription = anime.Description.Length > maxDescriptionLength
                        ? anime.Description.Substring(0, maxDescriptionLength) + "..."
                        : anime.Description;

                    animeList.Add(anime);
                }

                return animeList;
            }
        }
    }
}
