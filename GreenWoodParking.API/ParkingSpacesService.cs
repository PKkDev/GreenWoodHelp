using YOLO26.Shared.CvatWorker;

namespace GreenWoodParking.API
{
    public class ParkingSpacesService
    {
        public readonly CvatParser Parser;

        public ParkingSpacesService()
        {
            Parser = new();
            Parser.Load("p31", Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "parking", "p31.xml"));
            Parser.Load("p30", Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "parking", "p30.xml"));
            Parser.Load("p29", Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "parking", "p29.xml"));
            Parser.Load("p28", Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "parking", "p28.xml"));
            Parser.Load("p22", Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "parking", "p22.xml"));
            Parser.Load("p21", Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "parking", "p21.xml"));
            Parser.Load("p15", Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "parking", "p15.xml"));
            Parser.Load("p16", Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "parking", "p16.xml"));
            Parser.Load("p13", Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "parking", "p13.xml"));
            Parser.Load("p14", Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "parking", "p14.xml"));
            Parser.Load("p39", Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "parking", "p39.xml"));
            Parser.Load("p40", Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "parking", "p40.xml"));
        }
    }
}
