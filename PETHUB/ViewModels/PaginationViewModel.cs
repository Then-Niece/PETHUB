namespace PETHUB.ViewModels
{
    public class PaginationViewModel
    {
        public int CurrentPage { get; set; }

        public int PageSize { get; set; }

        public int TotalItems { get; set; }

        public int TotalPages =>
            (int)Math.Ceiling(
                TotalItems / (double)PageSize
            );

        public bool HasPreviousPage =>
            CurrentPage > 1;

        public bool HasNextPage =>
            CurrentPage < TotalPages;
    }


    public class PaginationViewModel<T> : PaginationViewModel
    {
        public IEnumerable<T> Items { get; set; }
            = Enumerable.Empty<T>();
    }
}