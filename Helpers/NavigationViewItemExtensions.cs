using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Recap.Helpers
{
    public static class NavigationViewItemExtensions
    {
        // Register the FilterTag attached property
        public static readonly DependencyProperty FilterTagProperty =
            DependencyProperty.RegisterAttached(
                "FilterTag",
                typeof(string),
                typeof(NavigationViewItemExtensions),
                new PropertyMetadata(default(string))
            );

        // Get the FilterTag xaml property
        public static string GetFilterTag(DependencyObject obj)
        {
            return (string)obj.GetValue(FilterTagProperty);
        }

        // Set the FilterTag xaml property
        public static void SetFilterTag(DependencyObject obj, string value)
        {
            obj.SetValue(FilterTagProperty, value);
        }
    }
}