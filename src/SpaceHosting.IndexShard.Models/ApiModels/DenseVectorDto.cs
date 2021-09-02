namespace SpaceHosting.IndexShard.Models.ApiModels
{
    public record DenseVectorDto(double[] Coordinates)
        : VectorDto(IsSparse: false, Coordinates);
}
