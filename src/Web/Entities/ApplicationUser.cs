namespace Forum.Entities;

public class ApplicationUser
{
    public int Id { get; set; }
    public long CreatedTS { get; set; }
    public string Username { get; set; }
    public byte[] PasswordHash { get; set; }
    public byte[] PasswordSalt { get; set; }
    public string Role { get; set; }
    public bool ShowModControls { get; set; } // Used for setting if Add/Delete options are displayed
}