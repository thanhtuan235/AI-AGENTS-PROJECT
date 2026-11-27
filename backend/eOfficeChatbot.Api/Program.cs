using eOfficeChatbot.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Thêm cấu hình DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Cấu hình CORS (Cho phép Angular cổng 4200 và FastAPI gọi tới)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 3. Sử dụng Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll"); // Áp dụng CORS
app.UseAuthorization();
app.MapControllers();

// Tự động áp dụng migration khi khởi động
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
}

// 4. API "Hello World" / Health Check cho Người B và C test
app.MapGet("/api/health", () => new { 
    Status = "OK", 
    Message = "eOffice .NET 10 API is running!", 
    Timestamp = DateTime.UtcNow 
});

app.Run();