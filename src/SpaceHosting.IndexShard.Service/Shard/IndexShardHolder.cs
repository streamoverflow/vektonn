using System;
using System.Linq;
using FluentValidation.Results;
using SpaceHosting.Index;
using SpaceHosting.IndexShard.Models;
using SpaceHosting.IndexShard.Models.ApiModels;
using SpaceHosting.IndexShard.Models.ApiModels.Validation;
using Vostok.Logging.Abstractions;

namespace SpaceHosting.IndexShard.Service.Shard
{
    public class IndexShardHolder<TVector> : IDisposable, IIndexShardUpdater<TVector>, ISearchQueryExecutor
        where TVector : IVector
    {
        private readonly ILog log;
        private readonly IndexMeta indexMeta;
        private readonly SearchQueryValidator searchQueryValidator;
        private readonly IIndexShard<TVector> indexShard;

        public IndexShardHolder(ILog log, IndexMeta indexMeta)
        {
            this.log = log.ForContext("SpaceHostingIndexShard");
            this.indexMeta = indexMeta;

            searchQueryValidator = new SearchQueryValidator(indexMeta);

            var indexStoreFactory = new IndexStoreFactory<byte[], byte[]>(this.log);

            indexShard = indexMeta.HasSplits
                ? new SplitIndexShard<TVector>(this.log, indexMeta, indexStoreFactory)
                : new IndexShard<TVector>(this.log, indexMeta, indexStoreFactory);
        }

        public void Dispose()
        {
            indexShard.Dispose();
        }

        public void UpdateIndexShard(DataPointOrTombstone<TVector>[] batch)
        {
            indexShard.UpdateIndex(batch);
        }

        public ValidationResult ValidateSearchQuery(SearchQueryDto query)
        {
            return searchQueryValidator.Validate(query);
        }

        public SearchResultDto[] ExecuteSearchQuery(SearchQueryDto query)
        {
            var searchQuery = new SearchQuery<TVector>(
                query.SplitFilter?.ToDictionary(x => x.Key, x => x.Value),
                query.QueryVectors.Select(x => (TVector)x.ToVector(indexMeta.VectorDimension)).ToArray(),
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
