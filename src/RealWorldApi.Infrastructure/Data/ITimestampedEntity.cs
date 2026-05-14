namespace RealWorldApi.Infrastructure.Data;

public interface ITimestampedEntity: IEntity
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
    DateTime? DeletedAt { get; set; }
}