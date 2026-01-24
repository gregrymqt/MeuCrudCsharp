using System;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.About.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.About.Repositories;

public class AboutRepository : IAboutRepository
{
    private readonly ApiDbContext _context;

    public AboutRepository(ApiDbContext context)
    {
        _context = context;
    }

    // ==========================================
    // GENERIC SECTIONS
    // ==========================================
    public async Task<List<AboutSection>> GetAllSectionsAsync()
    {
        return await _context
            .AboutSections.OrderBy(s => s.OrderIndex) // Ordenação sugerida no seu model [cite: 14]
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<AboutSection?> GetSectionByIdAsync(int id)
    {
        return await _context.AboutSections.FindAsync(id);
    }

    public async Task AddSectionAsync(AboutSection section)
    {
        await _context.AboutSections.AddAsync(section);
        // NÃO chama SaveChangesAsync - deixa pro UnitOfWork
    }

    public Task UpdateSectionAsync(AboutSection section)
    {
        _context.AboutSections.Update(section);
        // NÃO chama SaveChangesAsync - deixa pro UnitOfWork
        return Task.CompletedTask;
    }

    public Task DeleteSectionAsync(AboutSection section)
    {
        _context.AboutSections.Remove(section);
        // NÃO chama SaveChangesAsync - deixa pro UnitOfWork
        return Task.CompletedTask;
    }

    // ==========================================
    // TEAM MEMBERS
    // ==========================================
    public async Task<List<TeamMember>> GetAllTeamMembersAsync()
    {
        return await _context.TeamMembers.AsNoTracking().ToListAsync();
    }

    public async Task<TeamMember?> GetTeamMemberByIdAsync(int id)
    {
        return await _context.TeamMembers.FindAsync(id);
    }

    public async Task AddTeamMemberAsync(TeamMember member)
    {
        await _context.TeamMembers.AddAsync(member);
        // NÃO chama SaveChangesAsync - deixa pro UnitOfWork
    }

    public Task UpdateTeamMemberAsync(TeamMember member)
    {
        _context.TeamMembers.Update(member);
        // NÃO chama SaveChangesAsync - deixa pro UnitOfWork
        return Task.CompletedTask;
    }

    public Task DeleteTeamMemberAsync(TeamMember member)
    {
        _context.TeamMembers.Remove(member);
        // NÃO chama SaveChangesAsync - deixa pro UnitOfWork
        return Task.CompletedTask;
    }
}
