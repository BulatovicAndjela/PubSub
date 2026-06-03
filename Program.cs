using PubSub.client;
using PubSub.services;
using PubSub.Hubs;

namespace PubSub
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var serverTask = Task.Run(() => RunServer(new string[] { "--urls", "http://localhost:5000" }));

            await Task.Delay(5000);

            var client = new PersonConsoleClient("http://localhost:5000");
            await client.RunAsync();
        }

        private static void RunServer(string[] args)
        {
            try
            {
                var builder = WebApplication.CreateBuilder(args);

                builder.Services.AddControllers();
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();

                builder.Services.AddSingleton<IPersonService, PersonService>();
                builder.Services.AddSingleton<ICupidService, CupidService>();
                builder.Services.AddHostedService<CupidBackgroundService>();

                builder.Services.AddSignalR();

                var app = builder.Build();

                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                // app.UseHttpsRedirection();
                app.UseAuthorization();

                app.MapControllers();
                app.MapHub<CupidHub>("/cupid-hub");

                app.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server error: {ex.Message}");
            }
        }
    }
}
