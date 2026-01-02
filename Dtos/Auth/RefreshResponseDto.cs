namespace E_Commerce.DTOs.Auth
{
    public class RefreshResponseDto
    {
        public string Token { get; set; } = default!;
        public string RefreshToken { get; set; } = default!;

        public RefreshResponseDto(string token, string refreshToken)
        {
            Token = token;
            RefreshToken = refreshToken;
        }
    }
}
