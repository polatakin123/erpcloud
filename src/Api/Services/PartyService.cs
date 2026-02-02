using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public interface IPartyService
{
    Task<PartyDto> CreateAsync(CreatePartyDto dto);
    Task<PaginatedResponse<PartyDto>> GetAllAsync(int page, int size, string? q, string? type);
    Task<PartyDto?> GetByIdAsync(Guid id);
    Task<PartyDto?> UpdateAsync(Guid id, UpdatePartyDto dto);
    Task<bool> DeleteAsync(Guid id);
}

public class PartyService : IPartyService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;

    public PartyService(ErpDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<PartyDto> CreateAsync(CreatePartyDto dto)
    {
        var normalizedCode = dto.Code.Trim().ToUpper();
        var normalizedType = dto.Type.Trim().ToUpper();
        var tenantId = _tenantContext.TenantId;

        // Check uniqueness
        var exists = await _context.Parties
            .AnyAsync(p => p.TenantId == tenantId && p.Code == normalizedCode);

        if (exists)
        {
            throw new InvalidOperationException($"Party with code '{normalizedCode}' already exists.");
        }

        var party = new Party
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = normalizedCode,
            Name = dto.Name.Trim(),
            Type = normalizedType,
            TaxNumber = dto.TaxNumber?.Trim(),
            Email = dto.Email?.Trim(),
            Phone = dto.Phone?.Trim(),
            Address = dto.Address?.Trim(),
            CreditLimit = dto.CreditLimit,
            PaymentTermDays = dto.PaymentTermDays,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.UserId ?? Guid.Empty
        };

        _context.Parties.Add(party);
        await _context.SaveChangesAsync();

        return MapToDto(party);
    }

    public async Task<PaginatedResponse<PartyDto>> GetAllAsync(int page, int size, string? q, string? type)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _context.Parties.Where(p => p.TenantId == tenantId);

        // Filter by type
        if (!string.IsNullOrWhiteSpace(type))
        {
            var normalizedType = type.Trim().ToUpper();
            // BOTH type matches all queries
            query = query.Where(p => p.Type == normalizedType || p.Type == "BOTH");
        }

        // Search by code or name
        if (!string.IsNullOrWhiteSpace(q))
        {
            var searchTerm = $"%{q.Trim()}%";
            query = query.Where(p => EF.Functions.ILike(p.Code, searchTerm) || EF.Functions.ILike(p.Name, searchTerm));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(p => p.Code)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(p => MapToDto(p))
            .ToListAsync();

        return new PaginatedResponse<PartyDto>(page, size, total, items);
    }

    public async Task<PartyDto?> GetByIdAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;
        var party = await _context.Parties
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

        return party == null ? null : MapToDto(party);
    }

    public async Task<PartyDto?> UpdateAsync(Guid id, UpdatePartyDto dto)
    {
        var tenantId = _tenantContext.TenantId;
        var party = await _context.Parties
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

        if (party == null) return null;

        var normalizedCode = dto.Code.Trim().ToUpper();
        var normalizedType = dto.Type.Trim().ToUpper();

        // Check uniqueness (excluding current)
        var exists = await _context.Parties
            .AnyAsync(p => p.TenantId == tenantId && p.Code == normalizedCode && p.Id != id);

        if (exists)
        {
            throw new InvalidOperationException($"Party with code '{normalizedCode}' already exists.");
        }

        party.Code = normalizedCode;
        party.Name = dto.Name.Trim();
        party.Type = normalizedType;
        party.TaxNumber = dto.TaxNumber?.Trim();
        party.Email = dto.Email?.Trim();
        party.Phone = dto.Phone?.Trim();
        party.Address = dto.Address?.Trim();
        party.CreditLimit = dto.CreditLimit;
        party.PaymentTermDays = dto.PaymentTermDays;
        party.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        return MapToDto(party);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;
        var party = await _context.Parties
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

        if (party == null) return false;

        _context.Parties.Remove(party);
        await _context.SaveChangesAsync();

        return true;
    }

    private static PartyDto MapToDto(Party party)
    {
        return new PartyDto(
            party.Id,
            party.Code,
            party.Name,
            party.Type,
            party.TaxNumber,
            party.Email,
            party.Phone,
            party.Address,
            party.CreditLimit,
            party.PaymentTermDays,
            party.IsActive,
            party.CreatedAt,
            party.CreatedBy
        );
    }
}
