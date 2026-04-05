using FFMediaToolkit;
using GreenWoodParking.API.Hubs;
using GreenWoodParking.API.Services;
using Microsoft.Extensions.ML;
using Scalar.AspNetCore;
using System.Runtime.InteropServices;
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
                    policy.WithOrigins("http://localhost:4200", "https://custplace.ru")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            builder.Services.AddSignalR();

            builder.Services.AddScoped<RetryHandler>();
            builder.Services.AddHttpClient("CameraDataClient")
                .AddHttpMessageHandler(provider => provider.GetRequiredService<RetryHandler>());

            builder.Services.AddSingleton<ParkingSpacesService>();
            builder.Services.AddScoped<ParkingService>();
            builder.Services.AddScoped<Yolo26Service>();

            builder.Services
                .AddPredictionEnginePool<Yolo26InputData, Yolo26OutputData>()
                .FromFile(modelName: "Yolo26m", filePath: "Assets/YoloModel/yolo26m.zip", watchForChanges: true);

            // https://github.com/radek-k/FFMediaToolkit#setup
            // https://github.com/GyanD/codexffmpeg/releases?q=7.&expanded=true
            // ffmpeg-7.1.1-full_build-shared  

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Console.WriteLine("Use FFmpeg for linux");
                //FFmpegLoader.FFmpegPath = "/usr/bin";
                FFmpegLoader.FFmpegPath = "/usr/lib/*-linux-gnu/";
            }
            else
            {
                Console.WriteLine("Use FFmpeg for windows");
                var FFmpegPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "ffmpeg");
                FFmpegLoader.FFmpegPath = FFmpegPath;
            }

            builder.Services.AddControllers();

            builder.Services.AddOpenApi();

            var app = builder.Build();

            app.UseCors("SignalRPolicy");

            app.UseDefaultFiles();
            app.UseStaticFiles();

            if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
            {
                app.Use(async (context, next) =>
                {
                    var path = context.Request.Path;
                    if (path.StartsWithSegments("/scalar") || path.StartsWithSegments("/openapi"))
                    {
                        if (context.Request.Headers.Authorization != "Basic YWRtaW46YWRtaW4=")
                        {
                            context.Response.Headers.WWWAuthenticate = "Basic realm=\"Scalar UI\"";
                            context.Response.StatusCode = 401;
                            return;
                        }
                    }
                    await next();
                });

                app.MapOpenApi();

                // https://learn.microsoft.com/ru-ru/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0&preserve-view=true
                // https://www.roundthecode.com/dotnet-tutorials/swagger-missing-dotnet-10-how-to-add-it-back
                // admin:admin
                app.MapScalarApiReference();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.MapHub<ParkingHub>("/parking-hub");

            app.Run();
        }
    }
}
