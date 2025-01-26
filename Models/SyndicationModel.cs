using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Recap.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Web.Syndication;

namespace Recap.Models
{
    public class SyndicationModel
    {
        public static async Task<List<Article>> GetArticlesAsync()
        {
            List<Article> articles = new();

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile localFile = await localFolder.CreateFileAsync("Feeds.json", CreationCollisionOption.OpenIfExists);

            try
            {

                string json = await FileIO.ReadTextAsync(localFile);
                List<Feed> feeds = JsonSerializer.Deserialize<List<Feed>>(json) ?? new List<Feed>();

                foreach (Feed feed in feeds)
                {
                    Uri uri = feed.FeedUri;

                    Debug.WriteLine($"Retrieving feed from: {uri}");
                    SyndicationFeed syndicationFeed = null;
                    try
                    {
                        syndicationFeed = await new SyndicationClient().RetrieveFeedAsync(uri);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error retrieving syndication feed: {ex.Message}");
                        continue;
                    }

                    if (syndicationFeed == null)
                    {
                        Debug.WriteLine("Syndication feed is null.");
                        continue;
                    }

                    foreach (SyndicationItem item in syndicationFeed.Items)
                    {
                        try
                        {
                            Article article = new()
                            {
                                ArticleTitle = item.Title?.Text ?? "No Title",
                                AuthorName = item.Authors.FirstOrDefault()?.Name ?? "Unknown Author",
                                PublishedDate = item.PublishedDate.DateTime,
                                ArticleSummary = item.Summary?.Text ?? "No Description",
                                ArticleUri = item.Links.FirstOrDefault()?.Uri ?? new Uri("about:blank"),
                                ArticleFeed = new Feed { FeedName = feed.FeedName, FeedUri = feed.FeedUri },
                                IsSaved = false,
                                IsRead = false
                            };

                            articles.Add(article);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error processing item: {ex.Message}");
                        }
                    }
                }
                Debug.WriteLine($"Retrieved {articles.Count} articles.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving feed: {ex.Message}");
            }
            return articles.OrderByDescending(article => article.PublishedDate).ToList();
        }
    }
}
