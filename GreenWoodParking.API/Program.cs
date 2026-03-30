
using FFMediaToolkit;
using GreenWoodParking.API.Hubs;
using GreenWoodParking.API.Services;
using Microsoft.Extensions.ML;
using YOLO26.Shared.YOLOWorker.DataStructures;

namespace GreenWoodParking.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("SignalRPolicy", policy =>
                {
                    policy.WithOrigins("http://localhost:4200") // Адрес вашего Angular
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials(); // ОБЯЗАТЕЛЬНО для SignalR
                });
            });

            builder.Services.AddSignalR();

            builder.Services.AddHttpClient("CameraDataClient").AddHttpMessageHandler<RetryHandler>();

            builder.Services.AddScoped<ParkingService>();
            builder.Services.AddScoped<Yolo26Service>();

            builder.Services.AddPredictionEnginePool<Yolo26InputData, Yolo26OutputData>()
                .FromFile(modelName: "Yolo26m", filePath: "Assets/YoloModel/yolo26m.zip", watchForChanges: true);

            // https://github.com/radek-k/FFMediaToolkit#setup
            // https://github.com/GyanD/codexffmpeg/releases?q=7.&expanded=true
            // ffmpeg-7.1.1-full_build-shared  
            var FFmpegPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "ffmpeg");
            FFmpegLoader.FFmpegPath = FFmpegPath;

            builder.Services.AddControllers();

            builder.Services.AddOpenApi();

            var app = builder.Build();

            app.UseCors("SignalRPolicy");

            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.MapHub<ParkingHub>("/parking-hub");

            app.Run();
        }
    }
}
