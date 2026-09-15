using System;
using System.Collections.Generic;
using System.Text;

using FluentAssertions;

using ReactivePipelineEditor.Domain.Common;
using ReactivePipelineEditor.Domain.Nodes;

namespace ReactivePipelineEditor.Domain.Tests
{
    public sealed class PipelineTests
    {
        private readonly Pipeline.Pipeline _pipeline = new(PipelineId.New(), "test");

        [Fact]
        public void AddNode_succeeds_for_new_node()
        {
            var node = new CsvSourceNode(NodeId.New(), new Point2D(0, 0), "file.csv");
            var result = _pipeline.AddNode(node);

            result.IsSuccess.Should().BeTrue();
            _pipeline.Nodes.Should().ContainSingle();
        }

        [Fact]
        public void AddNode_fails_for_duplicate_id()
        {
            var id = NodeId.New();
            var node1 = new CsvSourceNode(id, new Point2D(0, 0), "file1.csv");
            var node2 = new CsvSourceNode(id, new Point2D(100, 0), "file2.csv");

            _pipeline.AddNode(node1);
            var result = _pipeline.AddNode(node2);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error!.Value.Code.Should().Be("conflict");
        }

        [Fact]
        public void RemoveNode_succeeds_for_existing()
        {
            var node = new CsvSourceNode(NodeId.New(), new Point2D(0, 0), "file.csv");
            _pipeline.AddNode(node);

            var result = _pipeline.RemoveNode(node.Id);

            result.IsSuccess.Should().BeTrue();
            _pipeline.Nodes.Should().BeEmpty();
        }

        [Fact]
        public void RemoveNode_fails_for_missing()
        {
            var result = _pipeline.RemoveNode(NodeId.New());
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error!.Value.Code.Should().Be("not_found");
        }
    }
}
