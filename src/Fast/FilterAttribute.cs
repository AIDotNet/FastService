namespace Fast
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
	public class FilterAttribute : Attribute
	{
		public Type[] Types { get; set; }

		public FilterAttribute(params Type[] types)
		{
			Types = types;
		}
	}
}
