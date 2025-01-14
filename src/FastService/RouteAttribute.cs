namespace FastService
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)] 
	public class RouteAttribute(string route) : Attribute
	{
		public readonly string Route = route;
	}
}
