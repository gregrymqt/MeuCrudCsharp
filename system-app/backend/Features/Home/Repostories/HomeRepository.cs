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
        // NÃO chama SaveChangesAsync - deixa pro UnitOfWork
    }

    public Task UpdateHeroAsync(HomeHero hero)
    {
        _context.HomeHeroes.Update(hero);
        // NÃO chama SaveChangesAsync - deixa pro UnitOfWork
        return Task.CompletedTask;
    }

    public Task DeleteHeroAsync(HomeHero hero)
    {
        _context.HomeHeroes.Remove(hero);
        // NÃO chama SaveChangesAsync - deixa pro UnitOfWork
        return Task.CompletedTask;
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
        // NÃO chama SaveChangesAsync - deixa pro UnitOfWork
    }

    public Task UpdateServiceAsync(HomeService service)
    {
        _context.HomeServices.Update(service);
        // NÃO chama SaveChangesAsync - deixa pro UnitOfWork
        return Task.CompletedTask;
    }

    public Task DeleteServiceAsync(HomeService service)
    {
        _context.HomeServices.Remove(service);
        // NÃO chama SaveChangesAsync - deixa pro UnitOfWork
        return Task.CompletedTask;
    }
}
