using ApiGateway.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Mapear variáveis de ambiente para o formato esperado pela configuração
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Jwt:Secret"] = Environment.GetEnvironmentVariable("JWT_SECRET") ?? builder.Configuration["Jwt:Secret"],
    ["Jwt:Issuer"] = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration["Jwt:Issuer"],
    ["Jwt:Audience"] = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? builder.Configuration["Jwt:Audience"],
    ["Services:UserService:Url"] = Environment.GetEnvironmentVariable("USER_SERVICE_URL") ?? builder.Configuration["Services:UserService:Url"],
    ["Services:AuthService:Url"] = Environment.GetEnvironmentVariable("AUTH_SERVICE_URL")
        ?? Environment.GetEnvironmentVariable("AUTH_LAMBDA_URL")
        ?? builder.Configuration["Services:AuthService:Url"]
        ?? builder.Configuration["Services:AuthLambda:Url"],
    ["Services:VideoProcessingService:Url"] = Environment.GetEnvironmentVariable("VIDEO_PROCESSING_SERVICE_URL") ?? builder.Configuration["Services:VideoProcessingService:Url"],
    ["Services:NotificationService:Url"] = Environment.GetEnvironmentVariable("NOTIFICATION_SERVICE_URL") ?? builder.Configuration["Services:NotificationService:Url"]
});

// Autenticação JWT
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorizationPolicies();

builder.Services.AddGatewayReverseProxy(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin() //NOSONAR
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }));

app.MapReverseProxy();

await app.RunAsync();

// Make Program class visible for integration tests
public static partial class Program { }