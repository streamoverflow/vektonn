namespace SpaceHosting.ApiModels
{
    public class SearchResultDto
    {
        public double Distance { get; init; }
        public VectorDto Vector { get; init; } = null!;
        public object? Data { get; init; }
    }
}
