using DocSenseV1.Dtos;
using DocSenseV1.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DocSenseV1.Infrastructure
{
    public class UploadExceptionAttribute : ExceptionFilterAttribute
    {
        public override Task OnExceptionAsync(ExceptionContext context)
        {
            switch(context.Exception)
            {
                case UsageLimitException usageEx:
                    context.Result = new ObjectResult(new ErrorResponseDto
                    {
                        Error = "Usage Limit Reached",
                        Message = usageEx.Message,
                        Details = usageEx.Stats
                    })
                    {
                        StatusCode = 403
                    };
                    context.ExceptionHandled = true; 
                    break;
                case UnsupportedFileException notSupportedException:
                    var fileError = new ErrorResponseDto
                    {
                        Error = "Unsupported File",
                        Message = notSupportedException.Message,
                    };
                    context.Result = new BadRequestObjectResult(fileError);
                    context.ExceptionHandled = true;
                    break;
                case UnsupportedProviderException notSupportedException:
                    var providerError = new ErrorResponseDto
                    {
                        Error = "Unsupported Provider",
                        Message = notSupportedException.Message,
                    };
                    context.Result = new BadRequestObjectResult(providerError);
                    context.ExceptionHandled = true;
                    break;
                default:
                    context.Result = new ObjectResult("An error occurred while processing the file.")
                    {
                        StatusCode = 500
                    };
                    context.ExceptionHandled = true;
                    break;

            }

            return Task.CompletedTask;
        }
    }
}
