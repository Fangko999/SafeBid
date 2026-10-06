using System;
using System.Collections.Generic;

namespace SafeBid.Domain;

public class Category
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Guid? ParentId { get; private set; }
    
    public Category? Parent { get; private set; }
    public List<Category> Children { get; private set; } = new();

    private Category() { }

    public static Category Create(string name)
    {
        return new Category { Id = Guid.NewGuid(), Name = name };
    }

    public Result SetParent(Category? parent)
    {
        if (parent == null)
        {
            ParentId = null;
            Parent = null;
            return Result.Success();
        }

        var current = parent;
        while (current != null)
        {
            if (current.Id == this.Id)
            {
                return Result.Failure(new Error("Category.CircularReference", "Cannot set parent as it creates a circular reference."));
            }
            current = current.Parent;
        }

        ParentId = parent.Id;
        Parent = parent;
        return Result.Success();
    }
}
