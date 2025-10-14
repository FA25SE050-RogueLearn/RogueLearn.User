using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BuildingBlocks.Shared.Common;

public abstract class BaseEntity : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
    }

    protected BaseEntity(Guid id)
    {
        Id = id;
    }
}