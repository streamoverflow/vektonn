using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using Vektonn.Contracts;
using Vektonn.Contracts.ApiModels;
using Vektonn.Contracts.ApiModels.Validation;
using Vektonn.Index;
using Vostok.Logging.Abstractions;

namespace Vektonn.IndexShard
{
    public class IndexShardHolder<TVector> : IDisposable, IIndexShardUpdater<TVector>, ISearchQueryExecutor
        where TVector : IVector
    {
        private readonly ILog log;
        private readonly SearchQueryValidator searchQueryValidator;
        private readonly IIndexShard<TVector> indexShard;

        public IndexShardHolder(ILog log, IndexMeta indexMeta)
        {
            this.log = log.ForContext("VektonnIndexShard");

            IndexMeta = indexMeta;

            searchQueryValidator = new SearchQueryValidator(indexMeta);

            var indexStoreFactory = new IndexStoreFactory<byte[], byte[]>(this.log);

            indexShard = indexMeta.HasSplits
                ? new SplitIndexShard<TVector>(this.log, indexMeta, indexStoreFactory)
                : new WholeIndexShard<TVector>(this.log, indexMeta, indexStoreFactory);
        }

        public IndexMeta IndexMeta { get; }

        public long DataPointsCount => indexShard.DataPointsCount;

        public void Dispose()
        {
            indexShard.Dispose();
        }

        public void UpdateIndexShard(IReadOnlyList<DataPointOrTombstone<TVector>> dataPointOrTombstones)
        {
            indexShard.UpdateIndex(dataPointOrTombstones);
        }

        public ValidationResult ValidateSearchQuery(SearchQueryDto query)
        {
            return searchQueryValidator.Validate(query);
        }

        public SearchResultDto[] ExecuteSearchQuery(SearchQueryDto query)
        {
            var searchQuery = new SearchQuery<TVector>(
                query.SplitFilter?.ToDictionary(x => x.Key, x => x.Value),
                query.QueryVectors.Select(x => (TVector)x.ToVector(IndexMeta.VectorDimension)).ToArray(),
                query.K);

            log.Info($"Executing search query: {searchQuery}");
            var searchResults = indexShard.FindNearest(searchQuery);

            return searchResults.Select(
                    x => new SearchResultDto(
                        x.QueryVector.ToVectorDto(),
                        x.NearestDataPoints.Select(
                                p => new FoundDataPointDto(
                                    p.Vector.ToVectorDto(),
                                    p.Attributes.Select(t => new AttributeDto(t.Key, t.Value)).ToArray(),
                                    p.Distance)
                            )
                            .ToArray())
                )
                .ToArray();
        }
    }
}
