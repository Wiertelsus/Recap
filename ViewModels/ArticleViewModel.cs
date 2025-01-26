using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Recap.Models;
using Windows.Storage;

namespace Recap.ViewModels
{
    public class Article : INotifyPropertyChanged
    {
        public string AuthorName { get; set; }
        public string ArticleTitle { get; set; }
        public DateTimeOffset PublishedDate { get; set; }
        public Uri ArticleUri { get; set; }
        public string ArticleSummary { get; set; }
        public bool IsToday { get { return DateTime.Today == PublishedDate; } }
        public bool IsSaved { get; set; }
        public bool IsRead { get; set; }
        public Feed ArticleFeed { get; set; }
        public string DisplayablePublishedDate { get { return PublishedDate.ToString("dd.MM.yy, HH:mm"); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Feed
    {
        public Uri FeedUri { get; set; }
        public string FeedName { get; set; }
        public string FavIconUri { get { return $"https://{FeedUri.Host}/favicon.ico"; } }
    }

    public class ArticleViewModel : INotifyPropertyChanged
    {
        private static readonly Lazy<ArticleViewModel> lazyInstance = new Lazy<ArticleViewModel>(() => new ArticleViewModel());
        public static ArticleViewModel Instance => lazyInstance.Value;

        private ObservableCollection<Article> articles = new ObservableCollection<Article>();
        private const string CacheFileName = "CachedArticles.json";
        private string selectedFilterTag;
        private List<Article> allArticles = new List<Article>();

        public ObservableCollection<Article> Articles
        {
            get => articles;
            set
            {
                if (articles != value)
                {
                    articles = value;
                    OnPropertyChanged(nameof(Articles));
                }
            }
        }

        public string SelectedFilterTag
        {
            get => selectedFilterTag;
            set
            {
                if (selectedFilterTag != value)
                {
                    selectedFilterTag = value;
                    OnPropertyChanged(nameof(SelectedFilterTag));
                    ApplyFilter();
                }
            }
        }

        private ArticleViewModel()
        {
            LoadArticles();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task UpdateArticles()
        {
            try
            {
                Debug.WriteLine("UpdateArticles: Starting to retrieve articles.");
                List<Article> retrievedArticles = await SyndicationModel.GetArticlesAsync();
                Debug.WriteLine("UpdateArticles: Retrieved articles successfully.");

                await CacheArticlesAsync(retrievedArticles);
                Debug.WriteLine("UpdateArticles: Cached articles successfully.");

                RefreshArticles(retrievedArticles);
                Debug.WriteLine("UpdateArticles: Refreshed articles successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateArticles: Exception occurred - {ex.Message}");
                // Optionally, handle the exception (e.g., show a message to the user)
            }
        }

        private async void LoadArticles()
        {
            List<Article> cachedArticles = await GetCachedArticlesAsync();
            if (cachedArticles != null && cachedArticles.Any())
            {
                RefreshArticles(cachedArticles);
            }
            else
            {
                await UpdateArticles();
            }
        }

        private void RefreshArticles(List<Article> articlesList)
        {
            allArticles = articlesList;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            Articles.Clear();
            var filteredArticles = allArticles.Where(article =>
            {
                switch (selectedFilterTag)
                {
                    case "TodayFilter":
                        return article.PublishedDate.Date == DateTime.Today;
                    case "UnreadFilter":
                        return !article.IsRead;
                    case "SavedFilter":
                        return article.IsSaved;
                    case "NoneFilter":
                        return true;
                    default:
                        return true;
                }
            });

            foreach (var article in filteredArticles)
            {
                Articles.Add(article);
            }
        }

        private async Task CacheArticlesAsync(List<Article> articles)
        {
            StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
            StorageFile cacheFile = await tempFolder.CreateFileAsync(CacheFileName, CreationCollisionOption.ReplaceExisting);
            string json = JsonSerializer.Serialize(articles);
            await FileIO.WriteTextAsync(cacheFile, json);
        }

        private async Task<List<Article>> GetCachedArticlesAsync()
        {
            try
            {
                StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
                StorageFile cacheFile = await tempFolder.GetFileAsync(CacheFileName);
                string json = await FileIO.ReadTextAsync(cacheFile);
                return JsonSerializer.Deserialize<List<Article>>(json);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        public async Task UpdateCacheAsync(List<Article> articles)
        {
            await CacheArticlesAsync(articles);
            RefreshArticles(articles);
        }
    }



}
