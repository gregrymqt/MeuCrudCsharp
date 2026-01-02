using System;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Home.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Home.Repostories;

public class HomeRepository : IHomeRepository
{
    private readonly ApiDbContext _context;

    public HomeRepository(ApiDbContext context)
    {
        _context = context;
    }

    // --- HERO ---
    public async Task<List<HomeHero>> GetAllHeroesAsync()
    {
        // Retorna a lista do DbSet HomeHeroes [cite: 1]
        return await _context.HomeHeroes.AsNoTracking().ToListAsync();
    }

    public async Task<HomeHero?> GetHeroByIdAsync(int id)
    {
        return await _context.HomeHeroes.FindAsync(id);
    }

    public async Task AddHeroAsync(HomeHero hero)
    {
        await _context.HomeHeroes.AddAsync(hero);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateHeroAsync(HomeHero hero)
    {
        _context.HomeHeroes.Update(hero);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteHeroAsync(HomeHero hero)
    {
        _context.HomeHeroes.Remove(hero);
        await _context.SaveChangesAsync();
    }

    // --- SERVICES ---
    public async Task<List<HomeService>> GetAllServicesAsync()
    {
        // Retorna a lista do DbSet HomeServices [cite: 1]
        return await _context.HomeServices.AsNoTracking().ToListAsync();
    }

    public async Task<HomeService?> GetServiceByIdAsync(int id)
    {
        return await _context.HomeServices.FindAsync(id);
    }

    public async Task AddServiceAsync(HomeService service)
    {
        await _context.HomeServices.AddAsync(service);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateServiceAsync(HomeService service)
    {
        _context.HomeServices.Update(service);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteServiceAsync(HomeService service)
    {
        _context.HomeServices.Remove(service);
        await _context.SaveChangesAsync();
    }
}
