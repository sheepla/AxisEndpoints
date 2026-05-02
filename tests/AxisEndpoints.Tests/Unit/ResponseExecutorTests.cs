using System.Net;
using System.Reflection;
using AxisEndpoints.Internal;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AxisEndpoints.Tests.Unit;

public class ResponseExecutorTests
{
    [Fact]
    public void ToResult_WithBody_ReturnsJsonResult()
    {
        var dto = new TestDto("hello");
        var response = new Response<TestDto> { Body = dto };

        var result = ResponseExecutor.ToResult(response);

        result.Should().BeAssignableTo<JsonHttpResult<TestDto>>();
        var json = (JsonHttpResult<TestDto>)result;
        json.StatusCode.Should().Be(200);
        json.Value.Should().Be(dto);
    }

    [Fact]
    public void ToResult_WithCreatedStatus_ReturnsCorrectStatusCode()
    {
        var dto = new TestDto("created");
        var response = new Response<TestDto> { StatusCode = HttpStatusCode.Created, Body = dto };

        var result = ResponseExecutor.ToResult(response);

        result.Should().BeAssignableTo<JsonHttpResult<TestDto>>();
        var json = (JsonHttpResult<TestDto>)result;
        json.StatusCode.Should().Be(201);
    }

    [Fact]
    public void ToResult_WithEmptyResponse_ReturnsStatusCodeResult()
    {
        var response = new Response<EmptyResponse> { Body = EmptyResponse.Instance };

        var result = ResponseExecutor.ToResult(response);

        result.Should().BeAssignableTo<StatusCodeHttpResult>();
        var statusCode = (StatusCodeHttpResult)result;
        statusCode.StatusCode.Should().Be(200);
    }

    [Fact]
    public void ToResult_WithHeaders_ReturnsHeadersResult()
    {
        var dto = new TestDto("with-headers");
        var response = new Response<TestDto> { Body = dto, Headers = [("X-Custom", "value")] };

        var result = ResponseExecutor.ToResult(response);

        result.Should().BeOfType<HeadersResult>();
        var headersResult = (HeadersResult)result;

        var headersField = typeof(HeadersResult).GetField(
            "_headers",
            BindingFlags.NonPublic | BindingFlags.Instance
        )!;
        var headers =
            (IReadOnlyList<(string Name, string Value)>)headersField.GetValue(headersResult)!;
        headers.Should().ContainSingle().Which.Should().Be(("X-Custom", "value"));
    }

    [Fact]
    public void ToResult_NoContent_ReturnsStatusCode204()
    {
        var response = Response.NoContent;

        var result = ResponseExecutor.ToResult(response);

        result.Should().BeAssignableTo<StatusCodeHttpResult>();
        var statusCode = (StatusCodeHttpResult)result;
        statusCode.StatusCode.Should().Be(204);
    }

    public record TestDto(string Value);
}
