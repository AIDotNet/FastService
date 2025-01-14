using FastService;

namespace Fast.Test
{
	public class AppService : FastApi
	{
		public Task<string> CreateAsync()
		{
			return Task.FromResult("asd");
		}
	}
}
