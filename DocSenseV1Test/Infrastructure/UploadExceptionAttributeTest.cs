using DocSenseV1.Dtos;
using DocSenseV1.Exceptions;
using DocSenseV1.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DocSenseV1Test.Infrastructure
{
    public class UploadExceptionAttributeTest
    {
        private ExceptionContext CreateExceptionContext(Exception exception)
        {
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(
                httpContext,
                new Microsoft.AspNetCore.Routing.RouteData(),
                new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
            );

            return new ExceptionContext(actionContext, new List<IFilterMetadata>())
            {
                Exception = exception
            };
        }

        [Fact]
        public async Task OnException_UsageLimitException_Returns403Forbidden()
        {
            // Arrange
            var stats = new UsageCheckResult
            {
                Allowed = false,
                Stats = new UsageLimitsStats
                {
                    Symbols = new SymbolsMetric
                    {
                        Used = 500,
                        Limit = 500,
                        Remaining = 0,
                        RequestedSymbols = 100,
                    }
                }
            };

            var exception = new UsageLimitException("Monthly limit exceeded.", stats.Stats);
            var context = CreateExceptionContext(exception);
            var attribute = new UploadExceptionAttribute();

            // Act
            await attribute.OnExceptionAsync(context);

            // Assert
            Assert.True(context.ExceptionHandled);
            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(403, result.StatusCode);

            var errorResponse = Assert.IsType<ErrorResponseDto>(result.Value);
            Assert.Equal("Usage Limit Reached", errorResponse.Error);
            Assert.Equal("Monthly limit exceeded.", errorResponse.Message);
            Assert.Equal(stats.Stats, errorResponse.Details);
        }

        [Fact]
        public async Task OnException_UnsupportedFileException_BadRequest()
        {
            // Assert
            var exception = new UnsupportedFileException("The provided file format is not supported");
            var context = CreateExceptionContext(exception);
            var attribute = new UploadExceptionAttribute();

            // Act
            await attribute.OnExceptionAsync(context);

            // Assert
            Assert.True(context.ExceptionHandled);
            var result = Assert.IsType<BadRequestObjectResult>(context.Result);
            var errorResponse = Assert.IsType<ErrorResponseDto>(result.Value);
            Assert.Equal("Unsupported File", errorResponse.Error);
            Assert.Equal("The provided file format is not supported", errorResponse.Message);
        }

        [Fact]
        public async Task OnException_UnsupportedProviderException_BadRequest()
        {
            // Assert
            var exception = new UnsupportedProviderException("The provided provider is not supported");
            var context = CreateExceptionContext(exception);
            var attribute = new UploadExceptionAttribute();

            // Act
            await attribute.OnExceptionAsync(context);

            // Assert
            Assert.True(context.ExceptionHandled);
            var result = Assert.IsType<BadRequestObjectResult>(context.Result);
            var errorResponse = Assert.IsType<ErrorResponseDto>(result.Value);
            Assert.Equal("Unsupported Provider", errorResponse.Error);
            Assert.Equal("The provided provider is not supported", errorResponse.Message);
        }

        [Fact]
        public async Task OnException_GeneralException_Returns500InternalServerError()
        {
            // Assert
            var exception = new Exception("Error");
            var context = CreateExceptionContext(exception);
            var attribute = new UploadExceptionAttribute();

            // Act
            await attribute.OnExceptionAsync(context);

            // Asseert
            Assert.True(context.ExceptionHandled);
            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(500, result.StatusCode);
            Assert.Equal("An error occurred while processing the file.", result.Value);
        }
    }
}
