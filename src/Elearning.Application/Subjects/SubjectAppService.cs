using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elearning.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace Elearning.Subjects;

[Authorize(ElearningPermissions.Subjects.Default)]
public class SubjectAppService : ElearningAppService, ISubjectAppService
{
    private static readonly Regex CodeRegex = new("^[a-z0-9_-]+$", RegexOptions.Compiled);

    private readonly IDataFilter _dataFilter;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IRepository<Subject, Guid> _subjectRepository;

    public SubjectAppService(
        IRepository<Subject, Guid> subjectRepository,
        IGuidGenerator guidGenerator,
        IDataFilter dataFilter)
    {
        _subjectRepository = subjectRepository;
        _guidGenerator = guidGenerator;
        _dataFilter = dataFilter;
    }

    public async Task<PagedResultDto<SubjectDto>> GetListAsync(GetSubjectListInput input)
    {
        var query = await _subjectRepository.GetQueryableAsync();

        if (!input.Filter.IsNullOrWhiteSpace())
        {
            var filter = input.Filter!.Trim();
            query = query.Where(x =>
                x.Code.Contains(filter) ||
                x.Name.Contains(filter) ||
                (x.Description != null && x.Description.Contains(filter)));
        }

        if (input.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == input.IsActive.Value);
        }

        var totalCount = await AsyncExecuter.CountAsync(query);
        var items = await AsyncExecuter.ToListAsync(ApplySorting(query, input.Sorting)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount));

        return new PagedResultDto<SubjectDto>(
            totalCount,
            items.Select(MapToDto).ToList());
    }

    public async Task<SubjectDto> GetAsync(Guid id)
    {
        return MapToDto(await _subjectRepository.GetAsync(id));
    }

    [Authorize(ElearningPermissions.Subjects.Create)]
    public async Task<SubjectDto> CreateAsync(CreateSubjectDto input)
    {
        var code = NormalizeCode(input.Code);
        await ValidateCodeAsync(code);

        var subject = new Subject(
            _guidGenerator.Create(),
            code,
            input.Name,
            input.SortOrder,
            input.Description);

        if (!input.IsActive)
        {
            subject.Deactivate();
        }

        await _subjectRepository.InsertAsync(subject, autoSave: true);
        return MapToDto(subject);
    }

    [Authorize(ElearningPermissions.Subjects.Update)]
    public async Task<SubjectDto> UpdateAsync(Guid id, UpdateSubjectDto input)
    {
        var subject = await _subjectRepository.GetAsync(id);
        var code = NormalizeCode(input.Code);
        await ValidateCodeAsync(code, id);

        subject.UpdateDetails(
            code,
            input.Name,
            input.Description,
            input.SortOrder);

        await _subjectRepository.UpdateAsync(subject, autoSave: true);
        return MapToDto(subject);
    }

    [Authorize(ElearningPermissions.Subjects.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await _subjectRepository.DeleteAsync(id, autoSave: true);
    }

    [Authorize(ElearningPermissions.Subjects.Update)]
    public async Task ActivateAsync(Guid id)
    {
        var subject = await _subjectRepository.GetAsync(id);
        subject.Activate();
        await _subjectRepository.UpdateAsync(subject, autoSave: true);
    }

    [Authorize(ElearningPermissions.Subjects.Update)]
    public async Task DeactivateAsync(Guid id)
    {
        var subject = await _subjectRepository.GetAsync(id);
        subject.Deactivate();
        await _subjectRepository.UpdateAsync(subject, autoSave: true);
    }

    private static IQueryable<Subject> ApplySorting(IQueryable<Subject> query, string? sorting)
    {
        return sorting?.Trim().ToLowerInvariant() switch
        {
            "code" => query.OrderBy(x => x.Code),
            "code desc" => query.OrderByDescending(x => x.Code),
            "name" => query.OrderBy(x => x.Name),
            "name desc" => query.OrderByDescending(x => x.Name),
            "sortorder desc" => query.OrderByDescending(x => x.SortOrder).ThenBy(x => x.Name),
            _ => query.OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
        };
    }

    private async Task ValidateCodeAsync(string code, Guid? currentId = null)
    {
        if (!CodeRegex.IsMatch(code))
        {
            throw new UserFriendlyException(L["Subjects:CodeInvalid"]);
        }

        using (_dataFilter.Disable<ISoftDelete>())
        {
            var existing = await _subjectRepository.FindAsync(x => x.Code == code);
            if (existing != null && existing.Id != currentId)
            {
                throw new UserFriendlyException(L["Subjects:CodeAlreadyExists", code]);
            }
        }
    }

    private static string NormalizeCode(string code)
    {
        return code.Trim().ToLowerInvariant();
    }

    private static SubjectDto MapToDto(Subject subject)
    {
        return new SubjectDto
        {
            Id = subject.Id,
            Code = subject.Code,
            Name = subject.Name,
            Description = subject.Description,
            IsActive = subject.IsActive,
            SortOrder = subject.SortOrder,
            CreationTime = subject.CreationTime,
            CreatorId = subject.CreatorId,
            LastModificationTime = subject.LastModificationTime,
            LastModifierId = subject.LastModifierId,
            IsDeleted = subject.IsDeleted,
            DeleterId = subject.DeleterId,
            DeletionTime = subject.DeletionTime
        };
    }
}
