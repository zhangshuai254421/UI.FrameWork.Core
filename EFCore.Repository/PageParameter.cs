using System.Linq.Expressions;

namespace EFCore.Repository
{
    public class PageParameter
    {
        public int PageNum { get; set; } = 1;

        public int PageSize { get; set; } = 15;
    }
    public class PageParameter<TFilter> : PageParameter
    {
        public PageParameter(PageParameter page, TFilter filter)
        {
            PageNum = page.PageNum;

            PageSize = page.PageSize;

            Filter = filter;
        }

        public TFilter Filter { get; }
    }
}
