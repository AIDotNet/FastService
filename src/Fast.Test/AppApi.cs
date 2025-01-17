using FastService;
using Microsoft.AspNetCore.Authorization;

namespace Fast.Test
{
	[Authorize(Roles = "Admin")]
	public class AppService : FastApi
	{
		public Task<string> CreateAsync()
		{
			return Task.FromResult("asd");
		}
	}
}
