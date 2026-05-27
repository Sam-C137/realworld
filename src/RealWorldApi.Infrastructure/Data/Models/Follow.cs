using Microsoft.EntityFrameworkCore;

namespace RealWorldApi.Infrastructure.Data.Models;

[Index(nameof(FollowerId), nameof(FolloweeId), IsUnique = true)]
[Index(nameof(FollowerId))]
[Index(nameof(FolloweeId))]
public class Follow: ITimestampedEntity
{
    public Guid Id { get; set; }
    public Guid FollowerId { get; set; }
    public Guid FolloweeId { get; set; }

    public Profile Follower { get; set; } = null!;
    public Profile Followee { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
