using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace GeoMarker.Frontiers.Core.Models.Request
{
    public class DeGaussGeocodedJsonRequest
    {
        [Required]
        public List<DeGaussGeocodedJsonRecord> Records { get; set; } = new List<DeGaussGeocodedJsonRecord> ();
    }

    public class DeGaussGeocodedJsonRecord
    {
        [Required]
        public string lat { get; set; } = string.Empty;
        [Required]
        public string lon { get; set; } = string.Empty;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? id { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? zip { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? city { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? state { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? fips_county { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? score { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? precision { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? drive_time { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? distance { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? census_tract_id { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? fraction_assisted_income { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? fraction_high_school_edu { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? median_income { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? fraction_no_health_ins { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? fraction_poverty { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? fraction_vacant_housing { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? dep_index { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? census_block_group_id_1990 { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? census_block_group_id_2000 { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? census_block_group_id_2010 { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? census_block_group_id_2020 { get; set; }
    }
}
