using MediatR;
using Microsoft.EntityFrameworkCore;
using SafeBid.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SafeBid.Application;

public record CreateCategoryCommand(string Name, Guid? ParentId) : IRequest<Result<Guid>>;

public record UpdateCategoryParentCommand(Guid CategoryId, Guid? ParentId) : IRequest<Result>;

public record GetCategoriesQuery() : IRequest<Result<List<CategoryDto>>>;

public record CategoryDto(Guid Id, string Name, Guid? ParentId, List<CategoryDto> Children);

public class CategoryCommandHandlers : 
    IRequestHandler<CreateCategoryCommand, Result<Guid>>,
    IRequestHandler<UpdateCategoryParentCommand, Result>,
    IRequestHandler<GetCategoriesQuery, Result<List<CategoryDto>>>
{
    private readonly IAppDbContext _db;

    public CategoryCommandHandlers(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = Category.Create(request.Name);
        if (request.ParentId.HasValue)
        {
            var parent = await _db.Categories.FindAsync(new object[] { request.ParentId.Value }, cancellationToken);
            if (parent != null)
            {
                var setParentResult = category.SetParent(parent);
                if (!setParentResult.IsSuccess)
                    return Result<Guid>.Failure(setParentResult.Error);
            }
        }

        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(category.Id);
    }

    public async Task<Result> Handle(UpdateCategoryParentCommand request, CancellationToken cancellationToken)
    {
        var categories = await _db.Categories.ToListAsync(cancellationToken);
        
        var category = categories.FirstOrDefault(c => c.Id == request.CategoryId);
        var parent = request.ParentId.HasValue ? categories.FirstOrDefault(c => c.Id == request.ParentId.Value) : null;

        if (category == null) return Result.Failure(new Error("Category.NotFound", "Not found"));

        var result = category.SetParent(parent);
        if (result.IsSuccess)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        return result;
    }

    public async Task<Result<List<CategoryDto>>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _db.Categories.ToListAsync(cancellationToken);
        
        var dtos = categories.Select(c => new CategoryDto(c.Id, c.Name, c.ParentId, new List<CategoryDto>())).ToDictionary(c => c.Id);
        var roots = new List<CategoryDto>();

        foreach (var dto in dtos.Values)
        {
            if (dto.ParentId.HasValue && dtos.TryGetValue(dto.ParentId.Value, out var parentDto))
            {
                parentDto.Children.Add(dto);
            }
            else
            {
                roots.Add(dto);
            }
        }

        return Result<List<CategoryDto>>.Success(roots);
    }
}
