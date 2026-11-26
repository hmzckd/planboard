using Planboard.Api.Data;

namespace Planboard.Api.GraphQL;

[ObjectType<Project>]
public static partial class ProjectNode
{
    static partial void Configure(IObjectTypeDescriptor<Project> descriptor)
    {
        descriptor.Ignore(p => p.TenantId);
    }
}
