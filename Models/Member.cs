namespace SecretHitlerBackend.Models;

public class Member
{
    public string Name { get; }
    public string ConnectionId { get; }
    public string UserId { get; set; }


    public Member(string name, string userId)
        => (Name, UserId) = (name, userId);
}