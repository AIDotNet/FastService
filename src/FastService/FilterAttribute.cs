using System;

namespace FastService
{
	/// <summary>
	/// 指定端点过滤器的特性
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
	public sealed class FilterAttribute : Attribute
	{
		/// <summary>
		/// 获取过滤器类型数组
		/// </summary>
		public Type[] Types { get; }

		/// <summary>
		/// 初始化FilterAttribute的新实例
		/// </summary>
		/// <param name="types">过滤器类型数组</param>
		/// <exception cref="ArgumentNullException">当types为null时抛出</exception>
		public FilterAttribute(params Type[] types)
		{
			Types = types ?? throw new ArgumentNullException(nameof(types));
		}
	}
}
