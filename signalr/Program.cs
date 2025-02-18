using signalr.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor
builder.Services.AddRazorPages();
builder.Services.AddSignalR(); // Agregar SignalR

// Configurar CORS para permitir conexiones desde cualquier origen
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder.AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials()
               .SetIsOriginAllowed(_ => true); // Permite cualquier origen
    });
});

var app = builder.Build();

// Configuración del pipeline de solicitudes HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Habilitar CORS
app.UseCors("CorsPolicy");

app.UseAuthorization();

app.MapRazorPages();

// Mapear SignalR
app.MapHub<Pedidos>("/pedidoHub"); // Asegúrate de haber creado PedidoHub.cs

app.Run();

