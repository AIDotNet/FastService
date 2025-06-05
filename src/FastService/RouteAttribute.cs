using System;

namespace FastService
{
	/// <summary>
	/// 指定API路由的特性
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
	public sealed class RouteAttribute : Attribute
	{
		/// <summary>
		/// 获取路由模板
		/// </summary>
		public string Route { get; }

		/// <summary>
		/// 初始化RouteAttribute的新实例
		/// </summary>
		/// <param name="route">路由模板</param>
		/// <exception cref="ArgumentNullException">当route为null时抛出</exception>
		public RouteAttribute(string route)
		{
			Route = route ?? throw new ArgumentNullException(nameof(route));
		}
	}
}
