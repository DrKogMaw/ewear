using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<StoreDb>(o => o.UseSqlite(builder.Configuration.GetConnectionString("Store") ?? "Data Source=ewear.db"));
builder.Services.AddHttpClient();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(builder.Configuration["Frontend:Origin"] ?? "http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));
var key = builder.Configuration["Auth:SigningKey"];
if (string.IsNullOrWhiteSpace(key))
{
    if (!builder.Environment.IsDevelopment()) throw new InvalidOperationException("Set Auth:SigningKey in production.");
    key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
}
if (Encoding.UTF8.GetByteCount(key) < 32) throw new InvalidOperationException("Auth:SigningKey must be at least 32 bytes.");

var app = builder.Build();
app.Use(async (context, next) => { context.Response.Headers["X-Content-Type-Options"] = "nosniff"; await next(); });
app.UseCors();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StoreDb>();
    db.Database.EnsureCreated();
    if (!db.Products.Any())
    {
        db.Products.AddRange(
            new Product("The Everyday Shirt", "Shirts", "Relaxed cotton poplin, made for every day.", 68, "/assets/shirt.png", "Ivory", 24, true),
            new Product("The Studio Shirt", "Shirts", "A clean, easy layer with room to move.", 74, "/assets/shirt-blue.png", "Sky", 16, false),
            new Product("The Chelsea Boot", "Boots", "Polished leather with an easy pull-on fit.", 168, "/assets/boots.png", "Black", 12, true),
            new Product("The City Boot", "Boots", "A grounded silhouette for long days out.", 182, "/assets/boots-brown.png", "Chestnut", 9, false),
            new Product("The Relaxed Trouser", "Pants", "Soft tailoring with a fluid, modern drape.", 96, "/assets/pants.png", "Charcoal", 18, true),
            new Product("The Pleated Trouser", "Pants", "A sharp line with an unhurried feel.", 104, "/assets/pants-sand.png", "Sand", 15, false));
        db.SaveChanges();
    }
}

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/api/products", async (StoreDb db, string? category) =>
{
    var products = db.Products.AsNoTracking().Where(p => p.Active);
    if (!string.IsNullOrEmpty(category) && category != "All") products = products.Where(p => p.Category == category);
    return Results.Ok(await products.OrderBy(p => p.Id).ToListAsync());
});

app.MapPost("/api/auth/request-code", async (PhoneRequest input, HttpRequest request, StoreDb db, IHttpClientFactory clients, IConfiguration config, IWebHostEnvironment env) =>
{
    var phone = input.Phone?.Trim() ?? "";
    if (!Regex.IsMatch(phone, @"^\+[1-9]\d{7,14}$")) return Results.BadRequest(new ApiError("Enter an international number such as +15551234567."));
    var now = DateTimeOffset.UtcNow;
    var challenge = await db.Challenges.SingleOrDefaultAsync(c => c.Phone == phone);
    if (challenge != null && now - challenge.LastSentAt < TimeSpan.FromMinutes(1)) return Results.Json(new ApiError("Wait one minute before requesting another code."), statusCode: 429);
    if (challenge != null && now - challenge.WindowStartedAt < TimeSpan.FromHours(1) && challenge.SendCount >= 5) return Results.Json(new ApiError("Too many codes requested. Try again in an hour."), statusCode: 429);
    var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
    var localDevelopment = env.IsDevelopment() && IPAddress.IsLoopback(request.HttpContext.Connection.RemoteIpAddress ?? IPAddress.None);
    if (!localDevelopment)
    {
        var sid = config["Sms:AccountSid"]; var token = config["Sms:AuthToken"]; var from = config["Sms:FromNumber"];
        if (string.IsNullOrEmpty(sid) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(from)) return Results.Json(new ApiError("SMS delivery is not configured."), statusCode: 503);
        using var client = clients.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{sid}:{token}")));
        var sent = await client.PostAsync($"https://api.twilio.com/2010-04-01/Accounts/{Uri.EscapeDataString(sid)}/Messages.json",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["To"] = phone, ["From"] = from, ["Body"] = $"Your Ewear code is {code}. It expires in 10 minutes." }));
        if (!sent.IsSuccessStatusCode) return Results.Json(new ApiError("Could not send a code. Please try again."), statusCode: 502);
    }
    if (challenge == null) { challenge = new VerificationChallenge { Phone = phone, WindowStartedAt = now }; db.Challenges.Add(challenge); }
    if (now - challenge.WindowStartedAt >= TimeSpan.FromHours(1)) { challenge.WindowStartedAt = now; challenge.SendCount = 0; }
    challenge.SendCount++; challenge.LastSentAt = now; challenge.ExpiresAt = now.AddMinutes(10); challenge.Attempts = 0; challenge.CodeHash = CodeHash(phone, code, key!);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Verification code sent.", developmentCode = localDevelopment ? code : null });
});

app.MapPost("/api/auth/verify", async (VerifyRequest input, StoreDb db, IConfiguration config) =>
{
    var phone = input.Phone?.Trim() ?? "";
    var submittedCode = input.Code ?? "";
    var challenge = await db.Challenges.SingleOrDefaultAsync(c => c.Phone == phone);
    if (challenge == null || challenge.ExpiresAt < DateTimeOffset.UtcNow || challenge.Attempts >= 5) return Results.BadRequest(new ApiError("The code expired. Request a new one."));
    challenge.Attempts++;
    if (submittedCode.Length != 6 || !CryptographicOperations.FixedTimeEquals(Convert.FromHexString(challenge.CodeHash), Convert.FromHexString(CodeHash(phone, submittedCode, key!))))
    { await db.SaveChangesAsync(); return Results.BadRequest(new ApiError("That code is incorrect. Please try again.")); }
    db.Challenges.Remove(challenge);
    var user = await db.Users.SingleOrDefaultAsync(u => u.Phone == phone);
    var isNew = user == null;
    var staffPhones = (config["Staff:PhoneNumbers"] ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    var role = staffPhones.Contains(phone, StringComparer.Ordinal) ? "staff" : "customer";
    if (user == null) { user = new StoreUser { Phone = phone, Role = role }; db.Users.Add(user); } else user.Role = role;
    await db.SaveChangesAsync();
    return Results.Ok(new { token = CreateToken(user, key!), user = new { user.Id, user.Phone, user.Role }, isNew });
});

app.MapGet("/api/auth/me", async (HttpRequest request, StoreDb db) =>
{
    var user = await CurrentUser(request, db, key!, builder.Configuration);
    return user == null ? Results.Unauthorized() : Results.Ok(new { user.Id, user.Phone, user.Role });
});

app.MapGet("/api/uploads/{fileName}", (string fileName, IWebHostEnvironment env) =>
{
    if (!Regex.IsMatch(fileName, @"^[a-f0-9]{32}\.(png|jpg|webp)$")) return Results.NotFound();
    var path = Path.Combine(env.ContentRootPath, "uploads", fileName);
    if (!File.Exists(path)) return Results.NotFound();
    var type = fileName.EndsWith(".png") ? "image/png" : fileName.EndsWith(".jpg") ? "image/jpeg" : "image/webp";
    return Results.File(path, type);
});

app.MapPost("/api/staff/uploads", async (HttpRequest request, StoreDb db, IWebHostEnvironment env) =>
{
    if ((await CurrentUser(request, db, key!, builder.Configuration))?.Role != "staff") return Results.Json(new ApiError("Staff access required."), statusCode: 403);
    if (!request.HasFormContentType) return Results.BadRequest(new ApiError("Choose a PNG, JPEG, or WebP image."));
    var file = (await request.ReadFormAsync()).Files.GetFile("file");
    if (file == null || file.Length is <= 0 or > 5_000_000) return Results.BadRequest(new ApiError("Choose an image under 5 MB."));
    var extension = file.ContentType switch { "image/png" => ".png", "image/jpeg" => ".jpg", "image/webp" => ".webp", _ => null };
    if (extension == null) return Results.BadRequest(new ApiError("Choose a PNG, JPEG, or WebP image."));
    var directory = Path.Combine(env.ContentRootPath, "uploads"); Directory.CreateDirectory(directory);
    var name = $"{Guid.NewGuid():N}{extension}";
    await using (var output = File.Create(Path.Combine(directory, name))) await file.CopyToAsync(output);
    return Results.Ok(new { imageUrl = $"/api/uploads/{name}" });
});

app.MapPost("/api/staff/products", async (ProductInput input, HttpRequest request, StoreDb db) =>
{
    if ((await CurrentUser(request, db, key!, builder.Configuration))?.Role != "staff") return Results.Json(new ApiError("Staff access required."), statusCode: 403);
    var error = Validate(input); if (error != null) return Results.BadRequest(new ApiError(error));
    var product = new Product(); Apply(product, input); db.Products.Add(product); await db.SaveChangesAsync();
    return Results.Created($"/api/products/{product.Id}", product);
});
app.MapPut("/api/staff/products/{id:int}", async (int id, ProductInput input, HttpRequest request, StoreDb db) =>
{
    if ((await CurrentUser(request, db, key!, builder.Configuration))?.Role != "staff") return Results.Json(new ApiError("Staff access required."), statusCode: 403);
    var product = await db.Products.FindAsync(id); if (product == null) return Results.NotFound();
    var error = Validate(input); if (error != null) return Results.BadRequest(new ApiError(error));
    Apply(product, input); await db.SaveChangesAsync(); return Results.Ok(product);
});
app.MapDelete("/api/staff/products/{id:int}", async (int id, HttpRequest request, StoreDb db) =>
{
    if ((await CurrentUser(request, db, key!, builder.Configuration))?.Role != "staff") return Results.Json(new ApiError("Staff access required."), statusCode: 403);
    var product = await db.Products.FindAsync(id); if (product == null) return Results.NotFound();
    product.Active = false; await db.SaveChangesAsync(); return Results.NoContent();
});
app.Run();

static string? Validate(ProductInput p) =>
    string.IsNullOrWhiteSpace(p.Name) || p.Name.Length > 120 ? "Enter a product name under 120 characters." :
    p.Category is not ("Shirts" or "Boots" or "Pants") ? "Choose Shirts, Boots, or Pants." :
    p.Price <= 0 || p.Price > 100000 ? "Enter a valid price." :
    p.Stock < 0 ? "Stock cannot be negative." :
    string.IsNullOrWhiteSpace(p.ImageUrl) || !(p.ImageUrl.StartsWith("/assets/") || p.ImageUrl.StartsWith("/api/uploads/") || Uri.TryCreate(p.ImageUrl, UriKind.Absolute, out var u) && u.Scheme == "https") ? "Upload an image or use an HTTPS image URL." : null;
static void Apply(Product p, ProductInput i) { p.Name = i.Name.Trim(); p.Category = i.Category; p.Description = i.Description.Trim(); p.Price = i.Price; p.ImageUrl = i.ImageUrl.Trim(); p.Color = i.Color.Trim(); p.Stock = i.Stock; p.Featured = i.Featured; p.Active = true; }
static string CodeHash(string phone, string code, string key) => Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes($"{phone}:{code}")));
static string B64(byte[] data) => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
static byte[] UnB64(string data) => Convert.FromBase64String(data.Replace('-', '+').Replace('_', '/') + new string('=', (4 - data.Length % 4) % 4));
static string CreateToken(StoreUser user, string key)
{
    var header = B64(JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS256", typ = "JWT" }));
    var payload = B64(JsonSerializer.SerializeToUtf8Bytes(new { sub = user.Id, exp = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds() }));
    var content = $"{header}.{payload}";
    return $"{content}.{B64(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(content)))}";
}
static async Task<StoreUser?> CurrentUser(HttpRequest request, StoreDb db, string key, IConfiguration config)
{
    var auth = request.Headers.Authorization.ToString(); if (!auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return null;
    var parts = auth[7..].Split('.'); if (parts.Length != 3) return null;
    try
    {
        var content = $"{parts[0]}.{parts[1]}";
        if (!CryptographicOperations.FixedTimeEquals(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(content)), UnB64(parts[2]))) return null;
        using var json = JsonDocument.Parse(UnB64(parts[1]));
        if (json.RootElement.GetProperty("exp").GetInt64() < DateTimeOffset.UtcNow.ToUnixTimeSeconds()) return null;
        var user = await db.Users.FindAsync(json.RootElement.GetProperty("sub").GetGuid());
        if (user?.Role == "staff" && !(config["Staff:PhoneNumbers"] ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Contains(user.Phone, StringComparer.Ordinal)) user.Role = "customer";
        return user;
    }
    catch { return null; }
}

record PhoneRequest(string Phone);
record VerifyRequest(string Phone, string Code);
record ProductInput(string Name, string Category, string Description, decimal Price, string ImageUrl, string Color, int Stock, bool Featured);
record ApiError(string Error);
class StoreDb(DbContextOptions<StoreDb> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StoreUser> Users => Set<StoreUser>();
    public DbSet<VerificationChallenge> Challenges => Set<VerificationChallenge>();
    protected override void OnModelCreating(ModelBuilder model) { model.Entity<StoreUser>().HasIndex(u => u.Phone).IsUnique(); model.Entity<VerificationChallenge>().HasIndex(c => c.Phone).IsUnique(); }
}
class Product
{
    public Product() { }
    public Product(string name, string category, string description, decimal price, string imageUrl, string color, int stock, bool featured)
    { Name = name; Category = category; Description = description; Price = price; ImageUrl = imageUrl; Color = color; Stock = stock; Featured = featured; }
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = "";
    public string Color { get; set; } = "";
    public int Stock { get; set; }
    public bool Featured { get; set; }
    public bool Active { get; set; } = true;
}
class StoreUser { public Guid Id { get; set; } = Guid.NewGuid(); public string Phone { get; set; } = ""; public string Role { get; set; } = "customer"; }
class VerificationChallenge
{
    public int Id { get; set; }
    public string Phone { get; set; } = "";
    public string CodeHash { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset LastSentAt { get; set; }
    public DateTimeOffset WindowStartedAt { get; set; }
    public int SendCount { get; set; }
    public int Attempts { get; set; }
}
