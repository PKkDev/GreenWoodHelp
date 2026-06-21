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
            await parkingService.StartWorkForClient(Context.ConnectionId, Context.ConnectionAborted);
            await Clients.Caller.SendAsync(SignalMethods.ReceiveWorkStatus, "Работа завершена");
        }
    }

    public static class SignalMethods
    {
        /// <summary>
        /// Send status to client
        /// </summary>
        public static readonly string ReceiveWorkStatus = "ReceiveWorkStatus";

        /// <summary>
        /// Send result data to client
        /// </summary>
        public static readonly string ReceiveParkingData = "ReceiveParkingData";
    }
}
