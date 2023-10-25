namespace Passwordium_api.Model
{
    public class User
    {

        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public ICollection<Account>? Passwords { get; set; }
    }
}
