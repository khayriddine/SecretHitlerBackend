namespace SecretHitlerBackend.Models;

public class Room
{
    public string RoomId { get; }
    public List<Member> Members { get; } = new();
    public string CreatedBy { get; set; }
    public string Status { get { return Members.Count >= 5 ? "Ready" : "Not Ready"; } }
    public int Fascists
    {
        get
        {
            var playerCount = Members?.Count;

            if (playerCount < 5) return 0;

            if (playerCount == 5 || playerCount == 6) return 2;
            if (playerCount == 7 || playerCount == 8) return 3;
            return 4;
        }
    }
    public int Liberals
    {
        get
        {
            var playerCount = Members?.Count ?? 0;

            if (playerCount < 5) return 0;
            return playerCount - Fascists;
        }
    }

    public Room(string roomId, string playerName)
    {

        CreatedBy = playerName;
        RoomId = roomId;
    }

    public bool RemoveMember(string userId)
    {
        var m = Members.FirstOrDefault(x => x.UserId == userId);
        if (m != null)
            return Members.Remove(m);
        return false;
    }
}
