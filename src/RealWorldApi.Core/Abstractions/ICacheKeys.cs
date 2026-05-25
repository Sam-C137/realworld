namespace RealWorldApi.Core.Abstractions;

public interface ICacheKeys<in TId> where TId : notnull
{
    public static abstract string Version();
    public static abstract string Item(TId id, long version);
    public static abstract string Tag(TId id);
    public static abstract string List(long version, string fingerprint);
}

public interface ICacheKeys : ICacheKeys<Guid>;