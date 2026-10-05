namespace ContactManagmentAPI.Models.ResponseModels;

public class AuthResponse
{
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string TokenType { get; set; }
}
