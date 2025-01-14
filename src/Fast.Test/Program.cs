using FastService.Extensions;
using Microsoft.AspNetCore.Builder;
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
			builder.Services.WithFast();
			// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
			builder.Services.AddOpenApi();

			var app = builder.Build();

			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				app.MapScalarApiReference(); // scalar/v1
				app.MapOpenApi();
			}

			app.MapFast();

			app.MapGet("test",
				[EndpointSummary("")]
			() =>
				{

				});

			app.Run();

		}
	}
}
