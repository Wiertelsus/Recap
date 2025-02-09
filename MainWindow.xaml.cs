using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Recap.Helpers;
using Recap.Models;
using Recap.ViewModels;
using Windows.Storage;
using Windows.Web.Syndication;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Recap
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private ArticleViewModel articleViewModel;

        public MainWindow()
        {
            this.InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            articleViewModel = ArticleViewModel.Instance;
        }

        private void MainNavView_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("MainNavView_Loaded called");

            // Update articles when the navigation view is loaded
            articleViewModel.UpdateArticles();

            // Set the default selected item and navigate to the ArticlePage
            MainNavView.SelectedItem = MainNavView.MenuItems[0];
            Frame.Navigate(GetPageType("ArticlePage"));
        }

        private void On_Navigated(object sender, NavigationEventArgs e)
        {
            // Enable or disable the back button based on the navigation stack
            MainNavView.IsBackEnabled = Frame.CanGoBack;
        }

        private void MainNavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            Debug.WriteLine("MainNavView_SelectionChanged called");

            if (args.SelectedItemContainer != null && !args.IsSettingsSelected)
            {
                // Get the page type and filter tag from the selected item
                Type pageType = GetPageType(args.SelectedItemContainer?.Tag.ToString());
                string filterType = NavigationViewItemExtensions.GetFilterTag(args.SelectedItemContainer);

                Debug.WriteLine("pageType is " + pageType.Name);
                Debug.WriteLine("filterType is " + filterType);

                // Set the selected filter tag and navigate to the selected page
                articleViewModel.SelectedFilterTag = filterType;
                MainNavView_Navigate(pageType, args.RecommendedNavigationTransitionInfo);
            }
            else if (args.IsSettingsSelected)
            {
                // Navigate to the SettingsPage if the settings item is selected
                Type pageType = GetPageType("SettingsPage");
                Debug.WriteLine("pageType is SettingsPage");
                MainNavView_Navigate(pageType, args.RecommendedNavigationTransitionInfo);
            }
        }

        private void MainNavView_Navigate(Type pageType, NavigationTransitionInfo transitionInfo)
        {
            Debug.WriteLine("MainNavView_Navigate called");

            Type prePageType = Frame.CurrentSourcePageType;

            if (Frame.BackStack.Count > 0)
            {
                Debug.WriteLine("prePageType is " + prePageType.Name);
            }
            else
            {
                Debug.WriteLine("prePageType is null");
            }

            // Navigate to the new page if it is different from the current page
            if (prePageType is not null && !Type.Equals(prePageType, pageType))
            {
                Frame.Navigate(pageType, null, transitionInfo);
                Debug.WriteLine("navigating to " + pageType);
            }
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Page))]
        public Type GetPageType(string tag)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Get all types that are subclasses of Page and are not abstract
            List<Type> pageTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(Page)) && !t.IsAbstract)
                .ToList();

            // Find the type that matches the given tag
            Type? pageType = pageTypes.FirstOrDefault(t => t.Name.Equals(tag, StringComparison.OrdinalIgnoreCase));

            return pageType;
        }

        private async void AddFeedButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("AddFeedButton_Click called");
            ContentDialog dialog = AddFeedDialog;

            dialog.CloseButtonText = "Cancel";
            dialog.PrimaryButtonText = "Add";
            dialog.Title = "Add Feed?";
            dialog.Content = new TextBox { PlaceholderText = "Feed URL" };
            dialog.DefaultButton = ContentDialogButton.Primary;

            dialog.PrimaryButtonClick -= Dialog_AddButtonClick;
            dialog.PrimaryButtonClick += Dialog_AddButtonClick;

            await dialog.ShowAsync();
        }

        private async void Dialog_AddButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            TextBox feedUrlTextBox = AddFeedDialog.Content as TextBox;
            InfoBar feedInfoBar = MainNavView.FindName("DialogInfoBar") as InfoBar;

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile localFile = await localFolder.CreateFileAsync("Feeds.json", CreationCollisionOption.OpenIfExists);

            try
            {
                if (Uri.IsWellFormedUriString(feedUrlTextBox.Text, UriKind.Absolute) && feedUrlTextBox != null)
                {
                    // Retrieve the syndication feed from the provided URL
                    SyndicationFeed syndicationFeed = await new SyndicationClient().RetrieveFeedAsync(new Uri(feedUrlTextBox.Text));

                    Debug.WriteLine($"icon: {syndicationFeed.IconUri}");

                    Feed feed = new Feed { FeedName = syndicationFeed.Title.Text ?? new Uri(feedUrlTextBox.Text).Host, FeedUri = new Uri(feedUrlTextBox.Text) };

                    Uri feedUri = new Uri(feedUrlTextBox.Text);
                    string json = await FileIO.ReadTextAsync(localFile);

                    List<Feed> feedList = string.IsNullOrEmpty(json) ? new List<Feed>() : JsonSerializer.Deserialize<List<Feed>>(json);

                    Feed existingFeed = feedList.Find(data => data.FeedUri == feedUri);

                    if (existingFeed != null)
                    {
                        // Show a message if the feed already exists
                        feedInfoBar.Content = new TextBlock { Text = "That feed has already been added!" };
                        feedInfoBar.IsOpen = true;
                        Debug.WriteLine($"duplicate uri not added: {feedUrlTextBox.Text}");
                    }
                    else
                    {
                        // Add the new feed to the list and update the cache
                        feedList.Add(feed);
                        feedInfoBar.Content = new TextBlock { Text = $"{feed.FeedName} added successfully!" };
                        feedInfoBar.IsOpen = true;
                        Debug.WriteLine($"feed added: {feedUri}");

                        await FileIO.WriteTextAsync(localFile, JsonSerializer.Serialize(feedList, new JsonSerializerOptions { WriteIndented = true }));

                        await articleViewModel.UpdateCacheAsync(await SyndicationModel.GetArticlesAsync());
                    }
                }
                else
                {
                    // Show a message if the URL is invalid
                    feedInfoBar.Content = new TextBlock { Text = "Uhoh. Couldn't add feed, are you sure that was a valid URL?" };
                    feedInfoBar.IsOpen = true;
                    Debug.WriteLine($"malformed or null uri: {feedUrlTextBox.Text}");
                }
            }
            catch (Exception ex)
            {
                // Show a message if an exception occurs
                feedInfoBar.Content = new TextBlock { Text = $"Eek! Exception thrown! Message: {ex.Message}" };
                feedInfoBar.IsOpen = true;
                Debug.WriteLine(ex.Message);
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs args)
        {
            // Refresh the articles cache
            await articleViewModel.UpdateCacheAsync(await SyndicationModel.GetArticlesAsync());
        }
    }
}
