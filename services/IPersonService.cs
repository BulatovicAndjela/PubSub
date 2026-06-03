using PubSub.models;

namespace PubSub.services
{
    public interface IPersonService
    {
        bool RegisterPerson(Person person, Action<LoveLetter> onLetterReceived);
        IEnumerable<Person> GetAll();
        Person? FindByUsername(string username);
        bool DeliverLetter(string recipientUsername, LoveLetter letter);
        void ConfirmLetterRead(string username);
    }
}
