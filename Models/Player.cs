namespace SecretHitlerBackend.Models;

public class Player
{
    public string UserId { get; set; }
    public string Name { get; set; }
    public Role Role { get; set; }
    public bool IsAlive { get; set; } = true;
    public bool IsConnected { get; set; } = true;
    public bool IsPresident  { get;set; }
    public bool IsChancellor { get;set;}
}
