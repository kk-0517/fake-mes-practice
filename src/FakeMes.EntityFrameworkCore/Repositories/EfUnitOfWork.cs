using FakeMes.Domain.Repositories;

namespace FakeMes.EntityFrameworkCore.Repositories;

public class EfUnitOfWork(FakeMesDbContext db) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
