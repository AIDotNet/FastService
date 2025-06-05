using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Scalar.AspNetCore;

namespace Fast.Test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();
            
            // 注册 FastService APIs
            builder.Services.AddFastApis();
            builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = "yourIssuer",
                        ValidAudience = "yourAudience",
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("yourSecretKey"))
                    };
                });

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapScalarApiReference(); // scalar/v1
                app.MapOpenApi();
            }

            app.UseAuthentication();
            app.UseAuthorization();

            // 测试配置选项
            app.MapFastApis(options =>
            {
                options.Prefix = "api";
                options.Version = "v1";
                options.PluralizeServiceName = true;
                options.AutoAppendId = true;
                options.DisableTrimMethodPrefix = false;
                options.EnableProperty = false;
            });

            app.MapGet("test",(string value) => new
            {
                value = value,
                name = "ces"
			});

			app.Run();
        }
    }
}