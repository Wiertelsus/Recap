using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Recap.ViewModels;
using Windows.Storage;

namespace Recap.Helpers
{
    public class CacheHelper
    {

        //Define the cache file name
        private static readonly string cacheFileString = "CachedArticles.json";


        //Caches articles in temporary "CachedArticles.json" for later retrieval
        public async Task CacheArticlesAsync(List<Article> articles)
        {
            
            //Define the folder and file
            StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
            StorageFile cacheFile = await tempFolder.CreateFileAsync(cacheFileString, CreationCollisionOption.ReplaceExisting);

            //Serialise using Json
            string json = JsonSerializer.Serialize(articles, SerializationContext.Default.ListArticle);
            
            await FileIO.WriteTextAsync(cacheFile, json);

            Debug.WriteLine("CacheArticlesAsync: Caching articles");
        }


        // Retrieves cached articles from the temporary file
        public async Task<List<Article>> GetCachedArticlesAsync()
        {
            try
            {
                StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
                StorageFile cacheFile = await tempFolder.GetFileAsync(cacheFileString);
                string json = await FileIO.ReadTextAsync(cacheFile);

                Debug.WriteLine("GetCachedArticlesAsync: Retrieving from cache");

                return JsonSerializer.Deserialize(json, SerializationContext.Default.ListArticle);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }


    }
}
