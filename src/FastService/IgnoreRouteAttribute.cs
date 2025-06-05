using System;

namespace FastService
{
	/// <summary>
	/// 指示方法应被忽略，不生成API路由的特性
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class IgnoreRouteAttribute : Attribute
	{

	}
}
