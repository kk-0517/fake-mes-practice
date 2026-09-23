using FakeMes.Domain.Repositories;
using FakeMes.Domain.Stations;
using Microsoft.EntityFrameworkCore;

namespace FakeMes.EntityFrameworkCore.Repositories;

public class EfStationRepository(FakeMesDbContext db) : IStationRepository
{
    public Task<bool> ExistsByCodeAsync(string code, CancellationToken ct = default)
        => db.Stations.AnyAsync(x => x.Code == code, ct);

    public async Task<IReadOnlyList<Station>> GetAllOrderedAsync(CancellationToken ct = default)
        => await db.Stations
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .ToListAsync(ct);

    public Task AddAsync(Station station, CancellationToken ct = default)
    {
        db.Stations.Add(station);
        return Task.CompletedTask;
    }
}
