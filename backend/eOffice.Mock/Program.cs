using System.Text;
using EOffice.Mock.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký DbContext cho EOfficeMockDb
builder.Services.AddDbContext<EOfficeMockDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("EofficeMockDb")));

// 2. Cấu hình JWT Authentication với MapInboundClaims = false
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]
    ?? throw new InvalidOperationException("Thiếu cấu hình Jwt:Key.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        // Giữ nguyên tên claim gốc ("sub", "role", "name", "dept_id")
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],

            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

            // Ánh xạ đúng claim để [Authorize(Roles = "...")] và User.Identity.Name hoạt động
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization();

// 3. Cấu hình CORS cho Angular (4200) và AI Service (8001)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontendAndAi", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:8001")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 4. Cấu hình Swagger hỗ trợ nhập Bearer Token trực tiếp
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EOffice Mock API",
        Version = "v1",
        Description = "Hệ thống giả lập nghiệp vụ eOffice & cấp phát JWT cho Phase 0"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập trực tiếp chuỗi access_token (không cần gõ chữ Bearer phía trước)."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 5. Tự động chạy Migration & Seed dữ liệu mẫu khi khởi động
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<EOfficeMockDbContext>();
    DbSeeder.Seed(dbContext);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontendAndAi");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();