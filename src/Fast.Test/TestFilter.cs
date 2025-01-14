
namespace Fast.Test
{
	public class TestFilter : IEndpointFilter
	{
		public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
		{
			return await next(context);
		}
	}
}
