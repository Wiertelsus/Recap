using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Recap.ViewModels;

namespace Recap
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public partial class ArticlePage : Page
    {
        public ArticlePage()
        {
            this.InitializeComponent();
            ViewModel = ArticleViewModel.Instance;
            this.DataContext = ViewModel; // Set the DataContext to the ViewModel
        }

        private void ArticlePageListView_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            Debug.WriteLine("ArtPageLisView_SelectionChanged called");

            ListView listView = sender as ListView;
            if (listView != null)
            {
                try
                {
                    if (listView.SelectedItem is Article article)
                    {
                        // Display the selected article in the WebView
                        Debug.WriteLine(article.ArticleTitle);

                        ArticleWebView.Source = article.ArticleUri;
                        WelcomeWebViewPanel.Visibility = Visibility.Collapsed;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Exception: {ex} with message: {ex.Message}");
                }
            }
            else if (listView.Items.Count == 0)
            {
                // Show the welcome panel if there are no items in the list
                WelcomeFeedPanel.Visibility = Visibility.Visible;
            }
            else
            {
                Debug.WriteLine("listView is null");
            }
        }

        private async void RefreshContainer_RefreshRequested(object sender, RefreshRequestedEventArgs args)
        {
            if (ViewModel != null)
            {
                // Update articles when refresh is requested
                await ViewModel.UpdateArticles();
            }
        }

        public ArticleViewModel ViewModel { get; set; }
    }
}

