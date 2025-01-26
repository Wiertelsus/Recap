using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        }

        private void MainNavView_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("MNV_Loaded called");

            articleViewModel = ArticleViewModel.Instance;
            articleViewModel.UpdateArticles();

            Frame.Navigated += On_Navigated; // Add handler for the frame navigation event

            MainNavView.SelectedItem = MainNavView.MenuItems[0];
            Frame.Navigate(GetPageType("ArticlePage"));
        }

        private void On_Navigated(object sender, NavigationEventArgs e)
        {
            MainNavView.IsBackEnabled = Frame.CanGoBack;
        }

        private void MainNavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            Debug.WriteLine("MNV_SelectionChanged called");

            if (args.SelectedItemContainer != null && args.IsSettingsSelected == false) 
            {
                Type pageType = GetPageType(args.SelectedItemContainer?.Tag.ToString()); //Get the page type from the tag

                String filterType = NavigationViewItemExtensions.GetFilterTag(args.SelectedItemContainer); //Get the filter type from the attached property

                Debug.WriteLine("pageType is " + pageType.Name);
                Debug.WriteLine("filterType is " + filterType);

                articleViewModel.SelectedFilterTag = filterType;

                MainNavView_Navigate(pageType, args.RecommendedNavigationTransitionInfo);
            }
            else if (args.IsSettingsSelected)
            {
                Type pageType = GetPageType("SettingsPage");

                Debug.WriteLine("pageType is SettingsPage");

                MainNavView_Navigate(pageType, args.RecommendedNavigationTransitionInfo);
            }
        }

        private void MainNavView_Navigate(Type pageType, NavigationTransitionInfo transitionInfo)
        {
            Debug.WriteLine("MNV_Navigate called");

            Type prePageType = Frame.CurrentSourcePageType; //Get page type before navigation as to prevent duplication

            if (Frame.BackStack.Count > 0)
            {
                Debug.WriteLine("prePageType is " + prePageType.Name);
            }
            else
            {
                Debug.WriteLine("prePageType is null");
            }

            if (prePageType is not null && !Type.Equals(prePageType, pageType)) //Check if the page type is the same as the current page
            {
                Frame.Navigate(pageType, null, transitionInfo);
                Debug.WriteLine("navigating to " + pageType);
            }
        }

        public Type GetPageType(string Tag)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Look for types that are solely pages
            List<Type> pageTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(Page)) && !t.IsAbstract)
                .ToList();

            // Find the type by tag
            Type? pageType = pageTypes.FirstOrDefault(t => t.Name.Equals(Tag, StringComparison.OrdinalIgnoreCase)); //Explicitly nullable Type because the page type CAN be null

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
            //Define UI elements
            TextBox feedUrlTextBox = AddFeedDialog.Content as TextBox;
            InfoBar feedInfoBar = (MainNavView.FindName("DialogInfoBar") as InfoBar);

            //Define app file storage 
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile localFile = await localFolder.CreateFileAsync("Feeds.json", CreationCollisionOption.OpenIfExists);

            try
            {
                if (Uri.IsWellFormedUriString(feedUrlTextBox.Text, UriKind.Absolute) && feedUrlTextBox != null)
                {
                    SyndicationFeed syndicationFeed = await new SyndicationClient().RetrieveFeedAsync(new Uri(feedUrlTextBox.Text));

                    Debug.WriteLine($"icon: {syndicationFeed.IconUri}");

                    Feed feed = new Feed { FeedName = $"{syndicationFeed.Title.Text}" ?? new Uri(feedUrlTextBox.Text).Host, FeedUri = new Uri(feedUrlTextBox.Text) };

                    Uri feedUri = new Uri(feedUrlTextBox.Text);
                    string json = await FileIO.ReadTextAsync(localFile);

                    List<Feed> feedList = string.IsNullOrEmpty(json) ? new List<Feed>() : JsonSerializer.Deserialize<List<Feed>>(json);

                    Feed existingFeed = feedList.Find(data => data.FeedUri == feedUri);

                    if (existingFeed != null)
                    {
                        feedInfoBar.Content = new TextBlock { Text = $"That feed has already been added!" };
                        feedInfoBar.IsOpen = true;
                        Debug.WriteLine($"duplicate uri not added: {feedUrlTextBox.Text}");
                    }
                    else
                    {
                        feedList.Add(feed);
                        feedInfoBar.Content = new TextBlock { Text = $"{feed.FeedName} added successfully! " };
                        feedInfoBar.IsOpen = true;
                        Debug.WriteLine($"feed added: {feedUri}");

                        await FileIO.WriteTextAsync(localFile, JsonSerializer.Serialize(feedList, new JsonSerializerOptions { WriteIndented = true }));

                        // Update the cache with the new feed
                        await articleViewModel.UpdateCacheAsync(await SyndicationModel.GetArticlesAsync());
                    }
                }
                else
                {
                    feedInfoBar.Content = new TextBlock { Text = $"Uhoh. Couldn't add feed, are you sure that was a valid URL?" };
                    feedInfoBar.IsOpen = true;
                    Debug.WriteLine($"malformed or null uri: {feedUrlTextBox.Text}");
                }
            }
            catch (Exception ex)
            {
                feedInfoBar.Content = new TextBlock { Text = $"Eek! Exception thrown! Message: {ex.Message}" };
                feedInfoBar.IsOpen = true;
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs args)
        {
            await articleViewModel.UpdateCacheAsync(await SyndicationModel.GetArticlesAsync());
        }
    }
}
