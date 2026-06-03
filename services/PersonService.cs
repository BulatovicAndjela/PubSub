using PubSub.models;
using System.Collections.Concurrent;

namespace PubSub.services
{
    public class PersonService : IPersonService
    {
        private readonly ConcurrentDictionary<string, Person> _persons = new();
        private readonly ConcurrentDictionary<string, Action<LoveLetter>> _callbacks = new();

        public bool RegisterPerson(Person person, Action<LoveLetter> onLetterReceived)
        {
            if (!_persons.TryAdd(person.Username, person))
            {
                return false;
            }
            _callbacks[person.Username] = onLetterReceived;
            return true;
        }

        public IEnumerable<Person> GetAll() => _persons.Values;

        public Person? FindByUsername(string username) =>
            _persons.TryGetValue(username, out var p) ? p : null;

        public bool DeliverLetter(string recipientUsername, LoveLetter letter)
        {
            if (!_persons.TryGetValue(recipientUsername, out var recipient))
                return false;

            if (recipient.IsWaitingForConfirmation)
                return false;

            if (recipient.IsBlocked(letter.Sender.Username))
                return false;

            recipient.IsWaitingForConfirmation = true;
            _callbacks[recipientUsername]?.Invoke(letter);
            return true;
        }

        public void ConfirmLetterRead(string username)
        {
            if (_persons.TryGetValue(username, out var p))
                p.IsWaitingForConfirmation = false;
        }
    }
}
