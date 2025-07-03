using System.Collections.Generic;
using System.Numerics;

namespace SecretHitlerBackend.Models;

public class Game
{
    public string RoomId { get; set; }
    public List<Player> Players { get; set; } = new();
    public IEnumerable<Player> AlivePlayers { get { return Players.Where(p => p.IsAlive); } }
    public List<PolicyType> DrawPile { get; set; } = new();
    public List<PolicyType> DiscardPile { get; set; } = new();

    public int EnactedFascistPolicies { get; set; }
    public int EnactedLiberalPolicies { get; set; }

    public string PresidentId { get; set; }
    public string ChancellorId { get; set; }
    public string PreviousPresidentId { get; set; }
    public string PreviousChancellorId { get; set; }

    public GamePhase Phase { get; set; } = GamePhase.Setup;
    public int ElectionTracker { get; set; } = 0;

    public bool VetoUnlocked => EnactedFascistPolicies >= 5;
    public bool IsStarted { get; set; } = false;
    public Dictionary<string, bool> Votes { get; set; } = new();
    public List<PolicyType> PresidentHand { get; set; } = new();
    public List<PolicyType> ChancellorHand { get; set; } = new();
    public bool IsGameOver { get; set; } = false;
    public string? WinningTeam { get; set; }
    public ExecutiveAction? CurrentExecutiveAction { get; set; }
    public bool VetoProposed { get; set; } = false;

    public void Initialize()
    {
        ShufflePlayers();
        AssignRoles();
        ShufflePolicies();
        PresidentId = Players[0].UserId;
        IsStarted = true;
        Phase = GamePhase.Nomination;
    }

    public void AssignRoles()
    {
        var count = Players.Count;
        var roles = GetRoleDistributions(count);
        var shuffled = Players.OrderBy(_ => Guid.NewGuid()).ToList();

        for (int i = 0; i < count; i++)
        {
            shuffled[i].Role = roles[i];
        }
    }

    public List<Role> GetRoleDistributions(int count)
    {
        if (count < 5 || count > 10)
            throw new ArgumentException("Player count must be between 5 and 10.");

        int fascists, liberals;

        switch (count)
        {
            case 5: liberals = 3; fascists = 1; break;
            case 6: liberals = 4; fascists = 1; break;
            case 7: liberals = 4; fascists = 2; break;
            case 8: liberals = 5; fascists = 2; break;
            case 9: liberals = 5; fascists = 3; break;
            case 10: liberals = 6; fascists = 3; break;
            default: throw new ArgumentOutOfRangeException();
        }

        List<Role> roles = [Role.Hitler];
        roles.AddRange(Enumerable.Repeat(Role.Fascist, fascists));
        roles.AddRange(Enumerable.Repeat(Role.Liberal, liberals));

        return roles.OrderBy(_ => Guid.NewGuid()).ToList();
    }

    public void ShufflePolicies()
    {
        DrawPile = Enumerable
            .Repeat(PolicyType.Liberal, 6)
            .Concat(Enumerable.Repeat(PolicyType.Fascist, 11))
            .OrderBy(_ => Guid.NewGuid())
            .ToList();
    }

    public void ShufflePlayers()
    {
        var rng = new Random();
        int n = Players.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (Players[k], Players[n]) = (Players[n], Players[k]);
        }
    }

    public void EnactPolicy(PolicyType policy, bool enactedByElectionTracker = false)
    {
        if (policy == PolicyType.Fascist)
            EnactedFascistPolicies++;
        else
            EnactedLiberalPolicies++;

        if (!enactedByElectionTracker)
            CheckWinCondition();
    }

    public void AdvancePresident()
    {
        var alive = AlivePlayers.ToList();
        var currentIndex = alive.FindIndex(p => p.UserId == PresidentId);
        PresidentId = alive[(currentIndex + 1) % alive.Count].UserId;
    }

    public void CheckWinCondition()
    {
        if (IsGameOver) return;

        if (EnactedLiberalPolicies >= 5)
        {
            EndGame("Liberal");
            return;
        }

        if (EnactedFascistPolicies >= 6)
        {
            EndGame("Fascist");
            return;
        }

        // Hitler elected chancellor after 3 fascist policies
        if (EnactedFascistPolicies >= 3 &&
            ChancellorId != null &&
            Players.FirstOrDefault(p => p.UserId == ChancellorId)?.Role == Role.Hitler &&
            Phase == GamePhase.Voting)
        {
            EndGame("Fascist");
            return;
        }
    }

    public void EndGame(string winningTeam)
    {
        IsGameOver = true;
        WinningTeam = winningTeam;
        Phase = GamePhase.GameOver;
    }

    public ExecutiveAction? GetExecutiveAction()
    {
        int fascistCount = EnactedFascistPolicies;
        int playerCount = Players.Count;

        return (fascistCount, playerCount) switch
        {
            // 5-6 players
            (3, 5 or 6) => ExecutiveAction.InvestigateLoyalty,
            (4 or 5, 5 or 6) => ExecutiveAction.Execution,

            // 7-8 players
            (2, 7 or 8) => ExecutiveAction.InvestigateLoyalty,
            (3, 7 or 8) => ExecutiveAction.SpecialElection,
            (4 or 5, 7 or 8) => ExecutiveAction.Execution,

            // 9-10 players
            (1 or 2, 9 or 10) => ExecutiveAction.InvestigateLoyalty,
            (3, 9 or 10) => ExecutiveAction.SpecialElection,
            (4 or 5, 9 or 10) => ExecutiveAction.Execution,

            _ => null
        };
    }



    public List<PolicyType> Draw(int count)
    {
        if (DrawPile.Count < count)
        {
            DrawPile.AddRange(DiscardPile);
            DiscardPile.Clear();
            DrawPile = DrawPile.OrderBy(_ => Guid.NewGuid()).ToList();
        }

        var drawn = DrawPile.Take(count).ToList();
        DrawPile.RemoveRange(0, drawn.Count);
        return drawn;
    }



    // Other actions: nominate, vote, pass policy, veto, etc...
}

