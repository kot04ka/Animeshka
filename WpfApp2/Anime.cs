using System.Collections.Generic;

namespace AnimeApp
{
    public class Anime
    {
        public int Id { get; set; }
        public Title Title { get; set; }
        public string Description { get; set; }
        public string ShortDescription { get; set; }
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
