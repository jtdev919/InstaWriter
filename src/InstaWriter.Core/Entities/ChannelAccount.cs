using System.Text.Json.Serialization;

namespace InstaWriter.Core.Entities;

public class ChannelAccount
{
    public Guid Id { get; set; }
    public PlatformType PlatformType { get; set; } = PlatformType.Instagram;
    public string AccountName { get; set; } = string.Empty;
    public string? ExternalAccountId { get; set; }
    public string? AccessToken { get; set; }
    public DateTime? TokenExpiry { get; set; }
    public AuthStatus AuthStatus { get; set; } = AuthStatus.Pending;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PlatformType
{
    Instagram
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AuthStatus
{
    Pending,
    Connected,
    Expired,
    Revoked
}
