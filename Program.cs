using SecretHitlerBackend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add CORS (place this BEFORE AddSignalR)
builder.Services.AddCors(options => {
    options.AddPolicy("SecretHitlerFront",
        policy => {
            policy.WithOrigins("http://localhost:4200") // Your Angular URL
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Required for SignalR
        });
});

// Add SignalR
builder.Services.AddSignalR();
builder.Services.AddSingleton<GameService>();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();


app.UseCors("SecretHitlerFront");
// Map SignalR Hub
app.MapHub<GameHub>("/gamehub");

app.Run();