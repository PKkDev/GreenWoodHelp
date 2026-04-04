using GreenWoodParking.API.DTO;
using GreenWoodParking.API.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.ML;

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
            await parkingService.StartWorkForClient(Context.ConnectionId, Context.ConnectionAborted);
            await Clients.Caller.SendAsync("ReceiveWorkStatus", "Работа завершена");
        }
    }
}
