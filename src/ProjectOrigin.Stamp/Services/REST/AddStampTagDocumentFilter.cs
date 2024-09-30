using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ProjectOrigin.Stamp.Services.REST;

public class AddStampTagDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Check if the "Contracts" tag already exists to avoid duplicates
        if (!swaggerDoc.Tags.Any(tag => tag.Name == "Stamp"))
        {
            swaggerDoc.Tags.Add(new OpenApiTag
            {
                Name = "Stamp",
                Description = "The Stamp is essential for Energy Origin yadadayadya"
            });
        }
    }
}
