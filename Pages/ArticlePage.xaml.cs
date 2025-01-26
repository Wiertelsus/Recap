
using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Recap.ViewModels;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Recap
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ArticlePage : Page
    {
        public ArticlePage()
        {
            ViewModel = ArticleViewModel.Instance;
            this.InitializeComponent();
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
                    Article article = listView.SelectedItem as Article;

                    if (article != null)
                    {
                        Debug.WriteLine(article.ArticleTitle);

                        ArticleWebView.Source = article.ArticleUri;
                        ArticleNotOpenPanel.Visibility = Visibility.Collapsed;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Exception: {ex} with message: {ex.Message}");
                }
            }
            else
            {
                Debug.WriteLine("listView is null");
            }
        }

        public ArticleViewModel ViewModel { get; set; }
    }
}
