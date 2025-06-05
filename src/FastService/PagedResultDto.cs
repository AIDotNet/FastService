namespace FastService;

/// <summary>
/// 分页数据封装类
/// </summary>
public class PagedResultDto<T>
{
    /// <summary>
    /// 数据列表
    /// </summary>
    public IEnumerable<T> Items { get; set; } = new List<T>();

    /// <summary>
    /// 总记录数
    /// </summary>
    public long Total { get; set; }

    /// <summary>
    /// 当前页码
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// 每页大小
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int Pages => Size > 0 ? (int)Math.Ceiling((double)Total / Size) : 0;

    /// <summary>
    /// 是否有下一页
    /// </summary>
    public bool HasNext => Page < Pages;

    /// <summary>
    /// 是否有上一页
    /// </summary>
    public bool HasPrevious => Page > 1;

    public PagedResultDto()
    {
    }

    public PagedResultDto(IEnumerable<T> items, long total, int page, int size)
    {
        Items = items;
        Total = total;
        Page = page;
        Size = size;
    }

    /// <summary>
    /// 创建分页结果
    /// </summary>
    public static PagedResultDto<T> Create(IEnumerable<T> items, long total, int page, int size)
    {
        return new PagedResultDto<T>(items, total, page, size);
    }

    /// <summary>
    /// 创建空的分页结果
    /// </summary>
    public static PagedResultDto<T> Empty(int page = 1, int size = 10)
    {
        return new PagedResultDto<T>(new List<T>(), 0, page, size);
    }
}