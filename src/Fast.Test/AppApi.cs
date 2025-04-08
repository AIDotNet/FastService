using FastService;
using Microsoft.AspNetCore.Authorization;

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
	}
}
