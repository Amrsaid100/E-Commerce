using System.ComponentModel.DataAnnotations;

public class PromoteUserDto
{
    [Required, EmailAddress]
    public string Email { get; set; }
}

public class DemoteAdminDto
{
    [Required, EmailAddress]
    public string Email { get; set; }
}
