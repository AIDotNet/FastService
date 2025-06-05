using FastService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fast.Test
{
	[Tags("test")]
	[Authorize(Roles = "Admin")]
	public class AppService : FastApi
	{
		public Task<string> CreateAsync()
		{
			return Task.FromResult("asd");
		}
		
		public Task<string> CreateDataAsync()
		{
			return Task.FromResult("asd");
		}
	}
}
