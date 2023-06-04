using Domain.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace WebApi.Hubs;

public class SeleniumLogHub:Hub
{
    public async Task SendLogNotificationAsync(SeleniumLogDto log)
    {
        try
        {
            await Clients.AllExcept(Context.ConnectionId).SendAsync("NewSeleniumLogAdded", log);
        }
        catch (Exception ex)
        {
            // Hata mesajını ve ayrıntıları konsola veya log dosyasına yazdırabilirsiniz.
            Console.WriteLine("Hata: " + ex.Message);
            Console.WriteLine("Ayrıntılar: " + ex.StackTrace);
        }
    }
}