using System;
using System.Collections.Generic;

namespace FastService
{
    /// <summary>
    /// 通用API响应结果封装类
    /// </summary>
    public class ResultDto
    {
        /// <summary>
        /// 响应状态码
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// 响应消息
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// 响应数据
        /// </summary>
        public object? Data { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public long Timestamp { get; set; }

        public ResultDto(int code, string? message, object? data = null)
        {
            Code = code;
            Message = message;
            Data = data;
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// 创建成功响应
        /// </summary>
        public static ResultDto Success(object? data = null, string? message = "操作成功")
        {
            return new ResultDto(200, message, data);
        }

        /// <summary>
        /// 创建失败响应
        /// </summary>
        public static ResultDto Error(string? message = "操作失败", int code = 500, object? data = null)
        {
            return new ResultDto(code, message, data);
        }

        /// <summary>
        /// 创建参数错误响应
        /// </summary>
        public static ResultDto BadRequest(string? message = "参数错误", object? data = null)
        {
            return new ResultDto(400, message, data);
        }

        /// <summary>
        /// 创建未授权响应
        /// </summary>
        public static ResultDto Unauthorized(string? message = "未授权", object? data = null)
        {
            return new ResultDto(401, message, data);
        }

        /// <summary>
        /// 创建禁止访问响应
        /// </summary>
        public static ResultDto Forbidden(string? message = "禁止访问", object? data = null)
        {
            return new ResultDto(403, message, data);
        }

        /// <summary>
        /// 创建未找到响应
        /// </summary>
        public static ResultDto NotFound(string? message = "资源未找到", object? data = null)
        {
            return new ResultDto(404, message, data);
        }
    }

    /// <summary>
    /// 泛型API响应结果封装类
    /// </summary>
    public class ResultDto<T> : ResultDto
    {
        /// <summary>
        /// 强类型响应数据
        /// </summary>
        public new T? Data { get; set; }

        public ResultDto(int code, string? message, T? data = default) : base(code, message, data)
        {
            Data = data;
        }

        /// <summary>
        /// 创建成功响应
        /// </summary>
        public static ResultDto<T> Success(T? data = default, string? message = "操作成功")
        {
            return new ResultDto<T>(200, message, data);
        }

        /// <summary>
        /// 创建失败响应
        /// </summary>
        public static ResultDto<T> Error(string? message = "操作失败", int code = 500, T? data = default)
        {
            return new ResultDto<T>(code, message, data);
        }
    }
} 