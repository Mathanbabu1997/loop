using LOOP.API.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace LOOP.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // =====================================================
            // Database
            // =====================================================

            builder.Services.AddDbContext<LoopDbContext>(options =>
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("DefaultConnection")
                ));


            // =====================================================
            // Controllers
            // =====================================================

            builder.Services.AddControllers();


            // =====================================================
            // CORS
            // =====================================================

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Frontend", policy =>
                {
                    policy
                        .WithOrigins(
                            "http://localhost:3000",
                            "http://127.0.0.1:3000"
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });


            // =====================================================
            // JWT Authentication
            // =====================================================

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,

                            ValidIssuer =
                                builder.Configuration["Jwt:Issuer"],

                            ValidAudience =
                                builder.Configuration["Jwt:Audience"],

                            IssuerSigningKey =
                                new SymmetricSecurityKey(
                                    Encoding.UTF8.GetBytes(
                                        builder.Configuration["Jwt:Key"]!
                                    )
                                )
                        };
                });


            // =====================================================
            // Authorization
            // =====================================================

            builder.Services.AddAuthorization();


            // =====================================================
            // Swagger + JWT Bearer
            // =====================================================

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition(
                    "Bearer",
                    new Microsoft.OpenApi.OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        In = Microsoft.OpenApi.ParameterLocation.Header,
                        Description = "Enter your JWT token."
                    });

                options.AddSecurityRequirement(document =>
                    new Microsoft.OpenApi.OpenApiSecurityRequirement
                    {
                        {
                            new Microsoft.OpenApi.OpenApiSecuritySchemeReference(
                                "Bearer",
                                document
                            ),
                            new List<string>()
                        }
                    });
            });


            // =====================================================
            // Build Application
            // =====================================================

            var app = builder.Build();


            // =====================================================
            // Seed Database
            // =====================================================

            using (var scope = app.Services.CreateScope())
            {
                var context =
                    scope.ServiceProvider
                        .GetRequiredService<LoopDbContext>();

                DbSeeder.SeedAsync(context)
                    .GetAwaiter()
                    .GetResult();
            }


            // =====================================================
            // Swagger
            // =====================================================
            // Enabled in both Development and Production
            // so Swagger works on Render.

            app.UseSwagger();
            app.UseSwaggerUI();


            // =====================================================
            // HTTPS
            // =====================================================

            app.UseHttpsRedirection();


            // =====================================================
            // CORS
            // =====================================================

            app.UseCors("Frontend");


            // =====================================================
            // Authentication
            // =====================================================

            app.UseAuthentication();


            // =====================================================
            // Authorization
            // =====================================================

            app.UseAuthorization();


            // =====================================================
            // Controllers
            // =====================================================

            app.MapControllers();


            // =====================================================
            // Run Application
            // =====================================================

            app.Run();
        }
    }
}