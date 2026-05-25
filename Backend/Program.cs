using EldenRingSquire.Backend.Services.Interfaces;
using EldenRingSquire.Backend.Services.WikiScraper;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();
builder.Services.AddControllers();
builder.Services.AddHttpClient<IChecklistDataService, FextraLifeWikiScraperService>(x => {
	x.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36");
	x.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
	x.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
});

var app = builder.Build();

app.MapControllers();
app.UseStaticFiles();
app.MapFallback("/api/{**_}", (string _) => Results.NotFound());
app.MapFallbackToFile("index.html");
app.MapGet("/favicon.ico", () => Results.NoContent());

app.Run();
