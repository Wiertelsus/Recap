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
            List<Article> articles = new([]);

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile localFile = await localFolder.CreateFileAsync("Feeds.json", CreationCollisionOption.OpenIfExists);

            try
            {

                string json = await FileIO.ReadTextAsync(localFile);
                List<Feed> feeds = new List<Feed>(JsonSerializer.Deserialize<List<Feed>>(json)); //Create a new feed list from data saved in local app storage.

                foreach(Feed feed in feeds)
                {
                    Uri uri = feed.FeedUri; //Assign URI to that of each of the feeds in the "feeds" list

                    Debug.WriteLine($"Retrieving feed from: {uri}");
                    SyndicationFeed syndicationFeed = await new SyndicationClient().RetrieveFeedAsync(uri); // Define feed and retrieve asynchronously from URI


                    if (syndicationFeed == null)
                    {
                        Debug.WriteLine("Syndication feed is null.");
                        return articles;
                    }

                    foreach (SyndicationItem item in syndicationFeed.Items)
                    {
                        try
                        {
                            Article article = new() // Create articles from data retrieved
                            {
                                ArticleTitle = item.Title?.Text ?? "No Title",
                                AuthorName = item.Authors.FirstOrDefault()?.Name.ToString() ?? "Unknown Author",
                                PublishedDate = item.PublishedDate.DateTime,
                                ArticleSummary = item.Summary?.Text ?? "No Description",
                                ArticleUri = item.Links.FirstOrDefault()?.Uri ?? new Uri("about:blank"),
                                ArticleFeed = new Feed { FeedName = feed.FeedName, FeedUri = feed.FeedUri },
                                IsSaved = false,
                                IsRead = false
                            };

                            articles.Add(article); // Pack articles into a list
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
