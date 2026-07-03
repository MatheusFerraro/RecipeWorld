namespace worldRecipeMvc.Models.ViewModels
{
    /// <summary>Drives the shared windowed pager partial (Views/Shared/_Pagination.cshtml).</summary>
    public class PaginationViewModel
    {
        public int CurrentPage { get; init; } = 1;
        public int TotalPages { get; init; }
        public string Action { get; init; } = "Index";

        /// <summary>Filter values to preserve across page links (search term, sort, ...).</summary>
        public Dictionary<string, string?> RouteValues { get; init; } = new();

        public Dictionary<string, string?> RouteValuesForPage(int page)
        {
            var values = new Dictionary<string, string?>(RouteValues)
            {
                ["pageNumber"] = page.ToString()
            };
            return values;
        }
    }
}
