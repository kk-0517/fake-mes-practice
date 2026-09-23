using FakeMes.Domain.Stations;

namespace FakeMes.Domain.Repositories;

public interface IStationRepository
{
    Task<bool> ExistsByCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<Station>> GetAllOrderedAsync(CancellationToken ct = default);
    Task AddAsync(Station station, CancellationToken ct = default);
}
