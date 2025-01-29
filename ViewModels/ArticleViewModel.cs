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


        //TODO: make IsSaved and IsRead be able to get read and saved to a file for future reference in filters.
        private bool isSaved;
        public bool IsSaved
        {
            get => isSaved;
            set
            {
                if (isSaved != value)
                {
                    isSaved = value;
                    OnPropertyChanged(nameof(IsSaved));
                }
            }
        }

        private bool isRead;
        public bool IsRead
        {
            get => isRead;
            set
            {
                if (isRead != value)
                {
                    isRead = value;
                    OnPropertyChanged(nameof(IsRead));
                }
            }
        }

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
            }
        }

        private async void LoadArticles()
        {
            // Load articles from cache or update if cache is empty
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
            // Refresh the articles list and apply the current filter
            allArticles = articlesList;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            Articles.Clear();
            if (allArticles == null)
            {
                Debug.WriteLine("allArticles is null.");
                return;
            }

            // Filter articles based on the selected filter tag
            var filteredArticles = allArticles.Where(article =>
            {
                if (article == null)
                {
                    Debug.WriteLine("Null article found in allArticles.");
                    return false;
                }

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

            // Add filtered articles to the observable collection
            foreach (var article in filteredArticles)
            {
                Articles.Add(article);
            }
        }

        private async Task CacheArticlesAsync(List<Article> articles)
        {
            // Cache articles to a temporary file
            StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
            StorageFile cacheFile = await tempFolder.CreateFileAsync(CacheFileName, CreationCollisionOption.ReplaceExisting);
            string json = JsonSerializer.Serialize(articles, SerializationContext.Default.ListArticle);
            await FileIO.WriteTextAsync(cacheFile, json);
        }

        private async Task<List<Article>> GetCachedArticlesAsync()
        {
            try
            {
                // Retrieve cached articles from the temporary file
                StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
                StorageFile cacheFile = await tempFolder.GetFileAsync(CacheFileName);
                string json = await FileIO.ReadTextAsync(cacheFile);
                
                
                return JsonSerializer.Deserialize(json, SerializationContext.Default.ListArticle);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        public async Task UpdateCacheAsync(List<Article> articles)
        {
            // Update the cache and refresh the articles list
            await CacheArticlesAsync(articles);
            RefreshArticles(articles);
        }
    }



}
