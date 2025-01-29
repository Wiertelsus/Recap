using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Recap.ViewModels;
using Windows.Services.Maps;
using Windows.Devices.PointOfService;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Windows.ApplicationModel;

namespace Recap
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public string AppVersion { get; }
        public ObservableCollection<Feed> Feeds { get; set; }

        private ArticleViewModel articleViewModel;

        public SettingsPage()
        {
            this.InitializeComponent();
            AppVersion = $"Version {GetAppVersion()}";
            LoadFeedsAsync();
            articleViewModel = ArticleViewModel.Instance;
        }

        private string GetAppVersion()
        {
            // Get the package version of the app
            PackageVersion version = Package.Current.Id.Version;
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        private async Task LoadFeedsAsync()
        {
            // Load feeds from the local storage
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile feedsFile = await localFolder.CreateFileAsync("Feeds.json", CreationCollisionOption.OpenIfExists);

            if (feedsFile != null)
            {
                string json = await FileIO.ReadTextAsync(feedsFile);
                Feeds = JsonSerializer.Deserialize<ObservableCollection<Feed>>(json, SerializationContext.Default.ObservableCollectionFeed) ?? new ObservableCollection<Feed>();
            }
            else
            {
                Feeds = new ObservableCollection<Feed>();
            }
        }

        private async Task SaveFeedsAsync()
        {
            // Save feeds to the local storage
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile feedsFile = await localFolder.CreateFileAsync("feeds.json", CreationCollisionOption.ReplaceExisting);
            string json = JsonSerializer.Serialize(Feeds, SerializationContext.Default.ObservableCollectionFeed);
            await FileIO.WriteTextAsync(feedsFile, json);
        }

        private async void ManageFeedsDialog_Click(object sender, RoutedEventArgs args)
        {
            ContentDialog dialog = ManageFeedsDialog; // Define the dialog

            ListView feedsListView = FeedsListView;

            feedsListView.ItemsSource = Feeds; // Set ItemsSource of FeedsListView
            feedsListView.SelectionMode = ListViewSelectionMode.Multiple;

            dialog.PrimaryButtonText = "Close";
            dialog.SecondaryButtonText = "Delete";
            dialog.IsPrimaryButtonEnabled = true;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Secondary)
            {
                // Remove selected feeds and save the updated list
                var selectedFeeds = feedsListView.SelectedItems.Cast<Feed>().ToList();
                foreach (var feed in selectedFeeds)
                {
                    Feeds.Remove(feed);
                }
                await SaveFeedsAsync();
                await articleViewModel.UpdateArticles();
            }
        }
    }
}
