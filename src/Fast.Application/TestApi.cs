using Fast.Test;
using FastService;

namespace Fast.Application
{
	[Filter(typeof(TestFilter))]
	[Tags("测试组")]
	public class TestApi : FastService.FastApi
	{

		[EndpointSummary("获取测试接口")]
		public Task GetAsync()
		{
			return Task.FromResult("test");
		}

		/// <summary>
		/// 测试注释
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		[EndpointSummary("测试")]
		public Task CreateAsync(TestInput input,string name)
		{
			return Task.FromResult("test");
		}
	}

	public class TestInput
	{
		public string Name { get; set; }
	}
}
