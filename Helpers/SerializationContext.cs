using System.Collections.Generic;
using System.Text.Json.Serialization;
using Recap.ViewModels;

namespace Recap
{
    [JsonSerializable(typeof(List<Feed>))]
    public partial class SerializationContext : JsonSerializerContext
    {
    }
}
