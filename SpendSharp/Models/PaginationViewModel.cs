namespace SpendSharp.Models
{
    public class PaginationViewModel
    {
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public string Controller { get; set; } = "";
        public string Action { get; set; } = "";
        public int[] PageSizes { get; set; } = [5, 10, 25, 50];
    }
}
