namespace GreenWoodParking.API.DTO
{
    public class ParkingSlotDto(string id, bool isHaveParkingSlot)
    {
        public string Id { get; set; } = id;
        public bool IsHaveParkingSlot { get; set; } = isHaveParkingSlot;
        public string Img { get; set; }
        public int TotalCount { get; set; } = 0;
        public int ParkingSlotCount { get; set; } = 0;


    }
}
