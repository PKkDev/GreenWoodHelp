using YOLO26.Shared.CvatWorker;

namespace GreenWoodParking.API
{
    public class ParkingSpacesService
    {
        public readonly CvatParser Parser;

        public ParkingSpacesService()
        {
            Parser = new();
            Parser.Load("p31", System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p31.xml"));
            Parser.Load("p30", System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p30.xml"));
            Parser.Load("p29", System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p29.xml"));
            Parser.Load("p28", System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p28.xml"));
            Parser.Load("p22", System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p22.xml"));
            Parser.Load("p21", System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p21.xml"));
            Parser.Load("p15", System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p15.xml"));
            Parser.Load("p16", System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p16.xml"));
            Parser.Load("p13", System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p13.xml"));
            Parser.Load("p14", System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p14.xml"));
        }
    }
}
