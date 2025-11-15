using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BeC.OpenId.Connect.Infrastructure.Swagger;

/// <summary>
/// Operation filter to handle file upload parameters in Swagger
/// </summary>
public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var formFileParameters = context.ApiDescription.ParameterDescriptions
            .Where(p => p.Type == typeof(IFormFile) ||
                       p.Type == typeof(IEnumerable<IFormFile>) ||
                       p.Type == typeof(IFormFileCollection))
            .ToList();

        if (!formFileParameters.Any())
            return;

        // Ensure the operation consumes multipart/form-data
        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema>(),
                        Required = new HashSet<string>()
                    }
                }
            }
        };

        var formDataSchema = operation.RequestBody.Content["multipart/form-data"].Schema;

        // Add all form parameters to the schema
        foreach (var parameter in context.ApiDescription.ParameterDescriptions)
        {
            if (parameter.Source?.Id == "Form" || parameter.Source?.Id == "FormFile")
            {
                var parameterName = parameter.Name;

                if (parameter.Type == typeof(IFormFile))
                {
                    formDataSchema.Properties[parameterName] = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary"
                    };
                }
                else if (parameter.Type == typeof(IEnumerable<IFormFile>) ||
                         parameter.Type == typeof(IFormFileCollection))
                {
                    formDataSchema.Properties[parameterName] = new OpenApiSchema
                    {
                        Type = "array",
                        Items = new OpenApiSchema
                        {
                            Type = "string",
                            Format = "binary"
                        }
                    };
                }
                else
                {
                    // Handle other form parameters (like string, DateTime, etc.)
                    formDataSchema.Properties[parameterName] = new OpenApiSchema
                    {
                        Type = GetOpenApiType(parameter.Type)
                    };
                }

                // Mark as required if not nullable
                if (!IsNullable(parameter.Type) && parameter.IsRequired)
                {
                    formDataSchema.Required.Add(parameterName);
                }
            }
        }

        // Remove the parameters from the operation parameters list
        // since they're now in the request body
        var parametersToRemove = operation.Parameters
            .Where(p => context.ApiDescription.ParameterDescriptions
                .Any(pd => pd.Name == p.Name &&
                          (pd.Source?.Id == "Form" || pd.Source?.Id == "FormFile")))
            .ToList();

        foreach (var parameter in parametersToRemove)
        {
            operation.Parameters.Remove(parameter);
        }
    }

    private static string GetOpenApiType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType == typeof(int) || underlyingType == typeof(long))
            return "integer";
        if (underlyingType == typeof(float) || underlyingType == typeof(double) || underlyingType == typeof(decimal))
            return "number";
        if (underlyingType == typeof(bool))
            return "boolean";
        if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset))
            return "string"; // with format: date-time

        return "string";
    }

    private static bool IsNullable(Type type)
    {
        return Nullable.GetUnderlyingType(type) != null ||
               !type.IsValueType ||
               type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }
}
