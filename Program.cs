using System.Text;
using G2CCRMPortal.Data;
using G2CCRMPortal.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace SamsyaSetuV2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ── Database ──────────────────────────────────────────────────
            builder.Services.AddDbContext<G2CCrmDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // ── JWT Authentication ────────────────────────────────────────
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
                    };

                    // Read JWT from cookie if no Authorization header
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            if (context.Request.Cookies.ContainsKey("jwt"))
                                context.Token = context.Request.Cookies["jwt"];
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddAuthorization();

            // ── Services ──────────────────────────────────────────────────
            builder.Services.AddScoped<IJwtService, JwtService>();
            builder.Services.AddScoped<IEmailService, EmailService>();

            // ── CORS (React frontend) ────────────────────────────────────
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReactApp", policy =>
                {
                    policy.WithOrigins(builder.Configuration["App:ClientUrl"]!)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // ── Middleware pipeline ───────────────────────────────────────
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            if (!app.Environment.IsDevelopment())
                app.UseHttpsRedirection();

            app.UseCors("AllowReactApp");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
