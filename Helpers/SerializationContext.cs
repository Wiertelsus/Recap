using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Recap.ViewModels;

namespace Recap
{
    [JsonSerializable(typeof(List<Feed>))]
    [JsonSerializable(typeof(List<Article>))]
    [JsonSerializable(typeof(ObservableCollection<Article>))]
    [JsonSerializable(typeof(ObservableCollection<Feed>))]
    [JsonSerializable(typeof(Feed))]
    [JsonSerializable(typeof(Article))]
    public partial class SerializationContext : JsonSerializerContext
    {
    }
}
