using YOLO26.Shared.CvatWorker;

namespace GreenWoodParking.API
{
    public class ParkingSpacesService
    {
        public readonly CvatParser Parser;

        public ParkingSpacesService()
        {
            Parser = new();
            Parser.Load("p31", Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p31.xml"));
            Parser.Load("p30", Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p30.xml"));
            Parser.Load("p29", Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p29.xml"));
            Parser.Load("p28", Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p28.xml"));
            Parser.Load("p22", Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p22.xml"));
            Parser.Load("p21", Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p21.xml"));
            Parser.Load("p15", Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p15.xml"));
            Parser.Load("p16", Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p16.xml"));
            Parser.Load("p13", Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p13.xml"));
            Parser.Load("p14", Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p14.xml"));
        }
    }
}
