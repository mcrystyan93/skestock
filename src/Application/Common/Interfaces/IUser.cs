namespace skestock.Application.Common.Interfaces;

public interface IUser
{
    int? Id { get; }
    List<string>? Roles { get; }

}
