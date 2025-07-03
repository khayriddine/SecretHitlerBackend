using SecretHitlerBackend.Services;

var builder = WebApplication.CreateBuilder(args);

var corsOrigins = builder.Configuration["AllowedOrigins"]?.Split(';')
                  ?? new[] { "http://localhost:4200" };
// Add CORS (place this BEFORE AddSignalR)
builder.Services.AddCors(options => {
    options.AddPolicy("SecretHitlerFront",
        policy => {
            policy.WithOrigins(corsOrigins) // Your Angular URL
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