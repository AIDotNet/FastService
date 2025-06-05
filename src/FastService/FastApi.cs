namespace FastService
{
	/// <summary>
	/// FastService API的基类，所有服务类都应继承此类
	/// </summary>
	public abstract class FastApi
	{
		/// <summary>
		/// 返回成功结果
		/// </summary>
		/// <param name="data">数据</param>
		/// <param name="message">消息</param>
		/// <returns></returns>
		protected ResultDto Ok(object? data = null, string? message = "操作成功")
		{
			return ResultDto.Success(data, message);
		}

		/// <summary>
		/// 返回成功结果（泛型）
		/// </summary>
		/// <typeparam name="T">数据类型</typeparam>
		/// <param name="data">数据</param>
		/// <param name="message">消息</param>
		/// <returns></returns>
		protected ResultDto<T> Ok<T>(T? data = default, string? message = "操作成功")
		{
			return ResultDto<T>.Success(data, message);
		}

		/// <summary>
		/// 返回错误结果
		/// </summary>
		/// <param name="message">错误消息</param>
		/// <param name="code">错误码</param>
		/// <param name="data">数据</param>
		/// <returns></returns>
		protected ResultDto Fail(string? message = "操作失败", int code = 500, object? data = null)
		{
			return ResultDto.Error(message, code, data);
		}

		/// <summary>
		/// 返回错误结果（泛型）
		/// </summary>
		/// <typeparam name="T">数据类型</typeparam>
		/// <param name="message">错误消息</param>
		/// <param name="code">错误码</param>
		/// <param name="data">数据</param>
		/// <returns></returns>
		protected ResultDto<T> Fail<T>(string? message = "操作失败", int code = 500, T? data = default)
		{
			return ResultDto<T>.Error(message, code, data);
		}

		/// <summary>
		/// 返回参数错误结果
		/// </summary>
		/// <param name="message">错误消息</param>
		/// <param name="data">数据</param>
		/// <returns></returns>
		protected ResultDto BadRequest(string? message = "参数错误", object? data = null)
		{
			return ResultDto.BadRequest(message, data);
		}

		/// <summary>
		/// 返回未授权结果
		/// </summary>
		/// <param name="message">错误消息</param>
		/// <param name="data">数据</param>
		/// <returns></returns>
		protected ResultDto Unauthorized(string? message = "未授权", object? data = null)
		{
			return ResultDto.Unauthorized(message, data);
		}

		/// <summary>
		/// 返回禁止访问结果
		/// </summary>
		/// <param name="message">错误消息</param>
		/// <param name="data">数据</param>
		/// <returns></returns>
		protected ResultDto Forbidden(string? message = "禁止访问", object? data = null)
		{
			return ResultDto.Forbidden(message, data);
		}

		/// <summary>
		/// 返回未找到结果
		/// </summary>
		/// <param name="message">错误消息</param>
		/// <param name="data">数据</param>
		/// <returns></returns>
		protected ResultDto NotFound(string? message = "资源未找到", object? data = null)
		{
			return ResultDto.NotFound(message, data);
		}

		/// <summary>
		/// 返回分页结果
		/// </summary>
		/// <typeparam name="T">数据类型</typeparam>
		/// <param name="pagedData">分页数据</param>
		/// <param name="message">消息</param>
		/// <returns></returns>
		protected ResultDto<PagedResultDto<T>> PagedOk<T>(PagedResultDto<T> pagedData, string? message = "操作成功")
		{
			return ResultDto<PagedResultDto<T>>.Success(pagedData, message);
		}
	}
}
