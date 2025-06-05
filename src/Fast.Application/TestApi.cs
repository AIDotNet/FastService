using Fast.Test;
using FastService;
using Microsoft.AspNetCore.Http;

namespace Fast.Application
{
	[Filter(typeof(TestFilter))]
	[Route("/api/test1")]
	[Tags("测试组")]
	public class TestApi : FastApi
	{

		[EndpointSummary("获取测试接口")]
		public Task GetAsync(TestInput input)
		{
			return Task.FromResult("test");
		}

		/// <summary>
		/// 测试注释
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		[EndpointSummary("测试")]
		public Task CreateDataAsync(TestInput input,string name)
		{
			return Task.FromResult("test");
		}
	}

	public class TestInput
	{
		public string Name { get; set; }
	}
}
