using System.Text.Json.Serialization;
using Plant_API.Clients;
using Plant_API.DB;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddScoped<PlantNetClient>();
builder.Services.AddScoped<PlantHealthAnalyzer>();
builder.Services.AddScoped<WikiClient>();
builder.Services.AddSingleton<PlantDB>();

builder.Services.AddControllers().AddJsonOptions(options =>

{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});


builder.Services.AddControllers().AddJsonOptions(opts =>
{
    opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();