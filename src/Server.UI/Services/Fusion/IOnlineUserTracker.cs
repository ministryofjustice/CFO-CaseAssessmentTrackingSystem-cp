using System.Runtime.Serialization;
using ActualLab.Fusion;
using ActualLab.Fusion.Blazor;
using MemoryPack;

namespace Cfo.Cats.Server.UI.Services.Fusion;

public interface IOnlineUserTracker : IComputeService
{
    Task AddUser(string sessionId, UserInfo userInfo, CancellationToken cancellationToken = default);
    Task RemoveUser(string sessionId, CancellationToken cancellationToken = default);
    Task UpdateUser(UserInfo userInfo, CancellationToken cancellationToken = default);
    
    [ComputeMethod]
    Task<UserInfo[]> GetOnlineUsers(CancellationToken cancellationToken = default);

}

[DataContract, MemoryPackable]
[ParameterComparer(typeof(ByValueParameterComparer))]
public sealed partial record UserInfo(
    [property: DataMember] string Id,
    [property: DataMember] string Name,
    [property: DataMember] string Email,
    [property: DataMember] string DisplayName,
    [property: DataMember] string ProfilePictureDataUrl,
    [property: DataMember] string SuperiorName,
    [property: DataMember] string SuperiorId,
    [property: DataMember] string TenantId,
    [property: DataMember] string TenantName,
    [property: DataMember] string? PhoneNumber,
    [property: DataMember] string[] AssignedRoles,
    [property: DataMember] UserPresence Status
);
public enum UserPresence
{
    Available,
    Busy,
    Donotdisturb,
    Away,
    Offline,
    Statusunknown
}