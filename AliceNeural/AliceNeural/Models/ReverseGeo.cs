using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AliceNeural.Models
{
    // Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
    public class ReverseAddress
    {
        [JsonPropertyName("road")]
        public string Road { get; set; }

        [JsonPropertyName("neighbourhood")]
        public string Neighbourhood { get; set; }

        [JsonPropertyName("village")]
        public string Village { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("county")]
        public string County { get; set; }

        [JsonPropertyName("ISO3166-2-lvl6")]
        public string ISO31662Lvl6 { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("ISO3166-2-lvl4")]
        public string ISO31662Lvl4 { get; set; }

        [JsonPropertyName("postcode")]
        public string Postcode { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("country_code")]
        public string CountryCode { get; set; }
    }

    public class ReverseGeo
    {
        [JsonPropertyName("place_id")]
        public int? PlaceId { get; set; }

        [JsonPropertyName("licence")]
        public string Licence { get; set; }

        [JsonPropertyName("osm_type")]
        public string OsmType { get; set; }

        [JsonPropertyName("osm_id")]
        public int? OsmId { get; set; }

        [JsonPropertyName("lat")]
        public string Lat { get; set; }

        [JsonPropertyName("lon")]
        public string Lon { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("place_rank")]
        public int? PlaceRank { get; set; }

        [JsonPropertyName("importance")]
        public double? Importance { get; set; }

        [JsonPropertyName("addresstype")]
        public string Addresstype { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }

        [JsonPropertyName("address")]
        public ReverseAddress Address { get; set; }

        [JsonPropertyName("boundingbox")]
        public List<string> Boundingbox { get; set; }
    }


}
