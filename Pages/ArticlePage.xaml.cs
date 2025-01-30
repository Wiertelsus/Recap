using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Recap.ViewModels;
using WinRT;

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
                        MarkAsReadAsync(article);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Exception in ArticlePageListView_SelectionChanged: {ex} with message: {ex.Message}");
                }
            }
            else if (listView.Items.Count == 0)
            {
                // Show the welcome panel if there are no items in the list
                WelcomeFeedPanel.Visibility = Visibility.Visible;
            }
            else
            {
                Debug.WriteLine("ArticlePageListView_SelectionChanged: listView is null");
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

        private async void PinButton_Click(object sender, RoutedEventArgs args)
        {

            Button pinButton = (Button)sender;

            if (pinButton.DataContext is Article article)
            {
                MarkAsSavedAsync(article);
            }
        }

        private async void MarkAsReadButton_Click(object sender, RoutedEventArgs args)
        {
            Button markAsReadButton = (Button)sender;
            if (markAsReadButton.DataContext is Article article)
            {
                await MarkAsReadAsync(article);
                
            }
        }

        private async Task MarkAsReadAsync(Article article)
        {
            Debug.WriteLine($"MarkAsReadAsync: Marking as read - {article}");
            article.IsRead = true;
        }

        private async Task MarkAsSavedAsync(Article article)
        {
            Debug.WriteLine($"MarkAsSavedAsync: Marking as saved - {article}");
            article.IsSaved = true;
        }

        public ArticleViewModel ViewModel { get; set; }
    }
}

