namespace Passwordium_api.Model
{
    public class Account
    {
        public int Id { get; set; }
        public string Naziv { get; set; }
        public string? Link { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public User User { get; set; }
    }
}
