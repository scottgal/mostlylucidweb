namespace Mostlylucid.Umami.Models;

public class AuthResponse
{
    public string Token { get; set; }
    public UserResponse User { get; set; }
}

public class UserResponse
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsAdmin { get; set; }
}