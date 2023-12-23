using Microsoft.Extensions.FileProviders;
using TorrentClient.Bencode;
using TorrentClient.Services;
using TorrentClient.Tcp;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IDecoder, Decoder>();
builder.Services.AddScoped<Encoder>();
builder.Services.AddScoped<ITorrentService, TorrentService>();
builder.Services.AddScoped<TcpListener>();

IFileProvider physicalProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory());
builder.Services.AddSingleton(physicalProvider);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();