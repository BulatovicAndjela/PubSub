using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using PubSub.models;

namespace PubSub.client
{
    public class PersonConsoleClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private HubConnection? _hubConnection;
        private string? _currentUsername;
        private bool _isWaitingForConfirmation = false;
        private LoveLetter? _pendingLetter;

        public PersonConsoleClient(string baseUrl = "https://localhost:5001")
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        public async Task RunAsync()
        {
            Console.WriteLine("=== Haotični kupidon - Aplikacija za pronalaženje partnera ===\n");

            while (true)
            {
                if (_currentUsername == null)
                {
                    await RegisterUserAsync();
                }
                else
                {
                    await HandleUserMenuAsync();
                }
            }
        }

        private async Task RegisterUserAsync()
        {
            Console.WriteLine("--- Registracija ---");
            Console.Write("Username: ");
            var username = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(username))
            {
                Console.WriteLine("⚠️ Username ne može biti prazan.\n");
                return;
            }

            Console.Write("Grad: ");
            var city = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(city))
            {
                Console.WriteLine("⚠️ Grad ne može biti prazan.\n");
                return;
            }

            Console.Write("Godine: ");
            if (!int.TryParse(Console.ReadLine(), out int age) || age < 0)
            {
                Console.WriteLine("⚠️ Godine moraju biti pozitivan broj.\n");
                return;
            }

            Console.Write("Broj telefona: ");
            var phone = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(phone) || !phone.All(char.IsDigit))
            {
                Console.WriteLine("⚠️ Broj telefona mora biti validan broj.\n");
                return;
            }

            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/person/register", new
                {
                    username,
                    city,
                    age,
                    phone
                });

                if (response.IsSuccessStatusCode)
                {
                    _currentUsername = username;
                    Console.WriteLine($"✓ Registracija uspešna! Dobrodošli, {username}!\n");
                    await StartListeningForLettersAsync();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"✗ Greška: {error}\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Greška pri registraciji: {ex.Message}\n");
            }
        }

        private async Task HandleUserMenuAsync()
        {
            await StartListeningForLettersAsync();
        }

        private async Task StartListeningForLettersAsync()
        {
            Console.WriteLine("Čekam pisma... (Kupidon šalje pisma svakih minut)");
            Console.WriteLine("Komande: /block username, /logout, /exit\n");

            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{_baseUrl}/cupid-hub")
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<LoveLetter>("ReceiveLetter", (letter) =>
            {
                ReceiveLetter(letter);
            });

            try
            {
                await _hubConnection.StartAsync();
                await _hubConnection.InvokeAsync("JoinAsync", _currentUsername);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Greška pri konekciji na hub: {ex.Message}");
            }

            var commandTask = ProcessCommandsAsync();
            await commandTask;
        }

        private async Task ProcessCommandsAsync()
        {
            while (_currentUsername != null)
            {
                if (_isWaitingForConfirmation && _pendingLetter != null)
                {
                    Console.Write("\nPotvrdi da si video/la pismo (da/ne): ");
                    var input = Console.ReadLine()?.Trim().ToLower();

                    if (input == "da" || input == "yes")
                    {
                        await ConfirmLetterAsync();
                        _isWaitingForConfirmation = false;
                        _pendingLetter = null;
                        Console.WriteLine("✓ Pismo je potvrđeno. Slobodan/na si za nova pisma.\n");
                    }
                }
                else
                {
                    Console.Write("Čekam pismo... (unesi /block, /logout, /exit): ");
                    var command = Console.ReadLine()?.Trim();

                    if (command == "/exit" || command == "/logout")
                    {
                        _currentUsername = null;
                        if (_hubConnection != null)
                        {
                            await _hubConnection.StopAsync();
                        }
                        Console.WriteLine("\n--- Do viđenja! ---\n");
                        break;
                    }
                    else if (command?.StartsWith("/block ") == true)
                    {
                        var blockedUser = command.Substring(7).Trim();
                        await BlockUserAsync(blockedUser);
                    }
                    else if (!string.IsNullOrWhiteSpace(command))
                    {
                        Console.WriteLine("⚠️ Nepoznata komanda.\n");
                    }

                    await Task.Delay(500);
                }
            }
        }

        public void ReceiveLetter(LoveLetter letter)
        {
            if (_isWaitingForConfirmation)
                return;

            _isWaitingForConfirmation = true;
            _pendingLetter = letter;

            var randomMessage = GetRandomMessage();

            Console.WriteLine("\n" + new string('=', 50));
            Console.WriteLine("💌 NOVO PISMO!");
            Console.WriteLine($"Od: {letter.Sender.Username}");
            Console.WriteLine($"Grad: {letter.Sender.City}");
            Console.WriteLine($"Godine: {letter.Sender.Age}");

            // Ako poruka NIJE "nisam zainteresovan/a" prikaži broj telefona
            if (letter.Message != "nisam zainteresovan/a za upoznavanje.")
            {
                Console.WriteLine($"Broj telefona: {letter.Sender.Phone}");
            }

            Console.WriteLine($"Poruka: \"{letter.Message}\"");
            Console.WriteLine(new string('=', 50) + "\n");
        }

        private string GetRandomMessage()
        {
            var messages = new[]
            {
                "Radujem se našem susretu!",
                "Želim da se upoznamo.",
                "Nisam zainteresovan/a za upoznavanje."
            };

            return messages[Random.Shared.Next(messages.Length)];
        }

        private async Task ConfirmLetterAsync()
        {
            try
            {
                if (_currentUsername != null)
                {
                    await _httpClient.PostAsync($"/api/person/confirm/{_currentUsername}", null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Greška pri potvrdi: {ex.Message}");
            }
        }

        private async Task BlockUserAsync(string blockedUsername)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/person/block", new
                {
                    blockerUsername = _currentUsername,
                    blockedUsername
                });

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✓ Korisnik {blockedUsername} je blokiran.\n");
                }
                else
                {
                    Console.WriteLine($"✗ Greška pri blokiranju.\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Greška: {ex.Message}\n");
            }
        }
    }
}
