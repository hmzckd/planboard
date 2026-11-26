using Planboard.Api.Data;

namespace Planboard.Api.GraphQL;

[ObjectType<Label>]
public static partial class LabelNode
{
    static partial void Configure(IObjectTypeDescriptor<Label> descriptor)
    {
        descriptor.Ignore(l => l.TenantId);
    }
}
