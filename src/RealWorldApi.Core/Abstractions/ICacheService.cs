namespace RealWorldApi.Core.Abstractions;

public interface ICacheService
{
    public Task<long> GetVersionAsync();
    public Task<long> BumpVersionAsync();
}