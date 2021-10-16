using System;
using System.Collections.Generic;
using System.Linq;
using Vektonn.Contracts.Sharding.Index;
using Vektonn.Index;

namespace Vektonn.Contracts
{
    public record IndexMeta(
        DataSourceMeta DataSourceMeta,
        string IndexAlgorithm,
        HashSet<string> SplitAttributes,
        IndexShardsMapMeta IndexShardsMap)
    {
        public int VectorDimension => DataSourceMeta.VectorDimension;

        public HashSet<string> IdAttributes => DataSourceMeta.IdAttributes;
        public HashSet<string> ShardAttributes => IndexShardsMap.ShardAttributes;
        public HashSet<string> PermanentAttributes => IdAttributes.Union(SplitAttributes).Union(ShardAttributes).ToHashSet();
        public HashSet<string> DataAttributes => DataSourceMeta.AttributeValueTypes.Keys.Except(PermanentAttributes).ToHashSet();

        public HashSet<string> IndexIdAttributes => IdAttributes.Except(SplitAttributes).ToHashSet();
        public HashSet<string> IndexPayloadAttributes => DataAttributes.Union(ShardAttributes).Except(IdAttributes).Except(SplitAttributes).ToHashSet();

        public bool HasSplits => SplitAttributes.Any();
        public bool HasPayload => IndexPayloadAttributes.Any();

        public void ValidateConsistency()
        {
            var untypedAttributes = PermanentAttributes
                .Union(DataAttributes)
                .Union(DataSourceMeta.DataSourceShardingMeta.ShardAttributes)
                .Except(DataSourceMeta.AttributeValueTypes.Keys)
                .ToArray();
            if (untypedAttributes.Any())
                throw new InvalidOperationException($"There are attributes with unspecified value type ({string.Join(", ", untypedAttributes)}) for: {this}");

            var nonIdDataSourceShardingAttributes = DataSourceMeta.DataSourceShardingMeta.ShardAttributes.Except(DataSourceMeta.IdAttributes).ToArray();
            if (nonIdDataSourceShardingAttributes.Any())
                throw new InvalidOperationException($"There are non-id data source sharding attributes ({string.Join(", ", nonIdDataSourceShardingAttributes)}) for: {this}");

            var vectorsAreSparse = AlgorithmTraits.VectorsAreSparse(IndexAlgorithm);
            if (vectorsAreSparse ^ DataSourceMeta.VectorsAreSparse)
                throw new InvalidOperationException($"IndexMeta.VectorsAreSparse ({vectorsAreSparse}) and DataSourceMeta.VectorsAreSparse ({DataSourceMeta.VectorsAreSparse}) are inconsistent");
        }

        public override string ToString()
        {
            return $"{nameof(IndexAlgorithm)} = {IndexAlgorithm}, " +
                   $"{nameof(SplitAttributes)} = {string.Join(";", SplitAttributes)}, " +
                   $"{nameof(ShardAttributes)} = {string.Join(";", ShardAttributes)}, " +
                   $"{nameof(DataAttributes)} = {string.Join(";", DataAttributes)}, " +
                   $"{nameof(DataSourceMeta)} = {DataSourceMeta}";
        }
    }
}
