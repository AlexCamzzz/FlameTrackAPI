using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FlameTrack.API.Models.Entities;

public class UserEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? Avatar { get; set; } // Base64 string
    public string PasswordHash { get; set; } = string.Empty;
    public bool HasAcceptedTerms { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
