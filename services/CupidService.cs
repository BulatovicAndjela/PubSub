using PubSub.models;
using PubSub.Hubs;
using System.Security.Cryptography;
using Microsoft.AspNetCore.SignalR;

namespace PubSub.services
{
    public class CupidService : ICupidService
    {
        public static readonly string[] Messages =
    {
        "Radujem se našem susretu!",
        "Želim da se upoznamo.",
        "Nisam zainteresovan/a za upoznavanje."
    };

        public const string DisinterestedMessage = "Nisam zainteresovan/a za upoznavanje.";

        private readonly IPersonService _personService;
        private readonly IHubContext<CupidHub> _hubContext;

        public CupidService(IPersonService personService, IHubContext<CupidHub> hubContext)
        {
            _personService = personService;
            _hubContext = hubContext;
        }

        public async Task SendLettersAsync()
        {
            var all = _personService.GetAll().ToList();
            if (all.Count < 2) return;

            foreach (var recipient in all)
            {
                var best = FindBestMatch(recipient, all);
                if (best is null) continue;

                var msg = Messages[RandomNumberGenerator.GetInt32(Messages.Length)];
                var letter = new LoveLetter
                {
                    Sender = best,
                    SenderUsername = best.Username,
                    SenderCity = best.City,
                    SenderAge = best.Age,
                    SenderPhone = best.Phone,
                    Message = msg
                };

                bool delivered = _personService.DeliverLetter(recipient.Username, letter);

                // Ako je pismo dostavljeno, pošalji preko SignalR-a
                if (delivered)
                {
                    await CupidHub.SendLetterToUserAsync(_hubContext, recipient.Username, letter);
                }
            }
        }

        public void SendLetters()
        {
            SendLettersAsync().ConfigureAwait(false);
        }

        private static Person? FindBestMatch(Person recipient, List<Person> all)
        {
            Person? best = null;
            var bestScore = int.MinValue;

            foreach (var candidate in all)
            {
                if (candidate.Username == recipient.Username) continue;
                if (recipient.IsBlocked(candidate.Username)) continue;

                int score = CalculateScore(recipient, candidate);
                if (score <= bestScore) continue;
                bestScore = score;
                best = candidate;
            }

            return best;
        }

        private static int CalculateScore(Person recipient, Person candidate)
        {
            var score = 0;

            if (string.Equals(recipient.City, candidate.City, StringComparison.OrdinalIgnoreCase))
                score += 30;

            if (Math.Abs(recipient.Age - candidate.Age) <= 2)
                score += 20;

            // Nasumični faktor 0-100 (specifikacija: RNGCryptoServiceProvider)
            score += GetRandomFactor();

            return score;
        }

        private static int GetRandomFactor()
        {
#pragma warning disable SYSLIB0023
            using var rng = new RNGCryptoServiceProvider();
#pragma warning restore SYSLIB0023
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            int value = BitConverter.ToInt32(bytes, 0) & int.MaxValue;
            return value % 101; // 0-100 uključivo
        }
    }
}
