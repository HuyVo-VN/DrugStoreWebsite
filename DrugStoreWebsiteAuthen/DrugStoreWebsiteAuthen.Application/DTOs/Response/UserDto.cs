using System.Text.Json.Serialization;

namespace DrugStoreWebsiteAuthen.Application.DTOs.Response
{
    public class UserDto
    {
        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        [JsonPropertyName("Gender")]
        public string Gender { get; set; }

        [JsonPropertyName("FullName")]
        public string FullName { get; set; }

        [JsonPropertyName("DateOfBirth")]
        public DateTime DateOfBirth { get; set; }
    }
}
