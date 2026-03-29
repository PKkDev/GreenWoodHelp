using GreenWoodParking.API.DTO;
using GreenWoodParking.API.Services;
using Microsoft.AspNetCore.SignalR;

namespace GreenWoodParking.API.Hubs
{
    public class ParkingHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            return base.OnDisconnectedAsync(exception);
        }

        public async Task GetParkingData(ParkingService parkingService)
        {
            parkingService.StartWorkForuser(Context.ConnectionId);
            await Clients.Caller.SendAsync("ReceiveWorkStatus", "Работа завершена");
        }
    }
}
