using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentAssertions;
using FluentAssertions.Equivalency;
using FluentAssertions.Execution;
using NUnit.Framework;
using Vektonn.Hosting;
using Vektonn.SharedImpl.Configuration;
using Vektonn.SharedImpl.Contracts;
using Vektonn.SharedImpl.Contracts.Sharding.Index;

namespace Vektonn.Tests.SharedImpl.Configuration
{
    public class IndexMetaProviderTests
    {
        private readonly IndexMetaProvider sut = new(configBaseDirectory: FileSystemHelpers.PatchDirectoryName("tests/config"));

        [Test]
        public void TryGetDataSourceMeta()
        {
            var dataSourceMeta = sut.TryGetDataSourceMeta(new DataSourceId(Name: "Samples.SparseVectors", Version: "0.1"));

            using (new AssertionScope())
            {
                dataSourceMeta.Should().NotBeNull();
                dataSourceMeta!.VectorsAreSparse.Should().BeTrue();
                dataSourceMeta.ShardAttributes.Should().BeEquivalentTo(new[] {"ShardId"}.ToHashSet());
            }
        }

        [Test]
        public void TryGetIndexMeta()
        {
            var indexMetaWithShardEndpoints = sut.TryGetIndexMeta(new IndexId(Name: "Samples.SparseVectors", Version: "0.1"));
            indexMetaWithShardEndpoints.Should().NotBeNull();

            using (new AssertionScope())
            {
                var indexMeta = indexMetaWithShardEndpoints!.IndexMeta;

                indexMeta.HasSplits.Should().BeTrue();
                indexMeta.HasPayload.Should().BeTrue();
                indexMeta.ShardAttributes.Should().BeEquivalentTo(new[] {"ShardId"}.ToHashSet());
                indexMeta.IndexIdAttributes.Should().BeEquivalentTo(new[] {"Id", "ShardId"}.ToHashSet());
                indexMeta.IndexPayloadAttributes.Should().BeEquivalentTo(new[] {"Payload"}.ToHashSet());
                indexMeta.IndexShardsMap
                    .ShardsById["ShardA"]
                    .ShardsByAttributeKey["ShardId"]
                    .Should()
                    .BeEquivalentTo(
                        new IndexAttributeValueShard<ushort>(
                            shardingRule: IndexShardingRule.BelongToSet,
                            shardValues: new ushort[] {0, 2, 4}.ToHashSet(),
                            attributeValueProjector: null!),
                        ComparingWithRespectToRuntimeTypes);

                indexMetaWithShardEndpoints.EndpointsByShardId
                    .Should()
                    .BeEquivalentTo(
                        new Dictionary<string, DnsEndPoint>
                        {
                            ["ShardA"] = new DnsEndPoint("localhost", 8082),
                            ["ShardB"] = new DnsEndPoint("localhost", 8083),
                        });
            }
        }

        private static EquivalencyAssertionOptions<T> ComparingWithRespectToRuntimeTypes<T>(EquivalencyAssertionOptions<T> options)
        {
            return options.RespectingRuntimeTypes();
        }
    }
}
