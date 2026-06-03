namespace PubSub.models
{
    public class Person
    {
        public string Username { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Phone { get; set; } = string.Empty;

        private readonly HashSet<string> _blockedUsers = new();
        public bool IsWaitingForConfirmation { get; set; } = false;

        public void Block(string username) => _blockedUsers.Add(username);
        public bool IsBlocked(string username) => _blockedUsers.Contains(username);

    }
}
