    namespace E_Commerce.DTOs.Auth
    {
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = string.Empty; 
        public string Token { get; set; } = string.Empty;        
        public string RefreshToken { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}