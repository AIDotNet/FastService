using FastService;

namespace Fast.Test
{
	public class AppService : FastApi
	{
		public static Task CreateAsync()
		{
			return Task.CompletedTask;
		}
	}
}
