using AutoMapper;
using SecretHitlerBackend.Models;

namespace SecretHitlerBackend.Services;

public class GameService
{
    private readonly Dictionary<string, Game> _games = new();
    private readonly IMapper _mapper;

    public GameService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public Game CreateGame(Models.Room room)
    {
        var game = new Game
        {
            RoomId = room.RoomId,
            Players = _mapper.Map<List<Player>>(room.Members)
        };
        _games[room.RoomId] = game;
        return game;
    }

    public Game GetGame(string roomId) => _games[roomId];

    public void StartGame(string roomId)
    {
        var game = GetGame(roomId);
        game.Initialize();
    }

    public void ChooseChancellor(string roomId, string userId)
    {
        var game = GetGame(roomId);
        if (game.Phase != GamePhase.Nomination)
            throw new InvalidOperationException("Not in nomination phase");

        game.ChancellorId = userId;
        game.Phase = GamePhase.Voting;
        game.Votes.Clear();
    }

    public void CastVote(string roomId, string userId, bool vote)
    {
        var game = GetGame(roomId);
        if (game.Phase != GamePhase.Voting)
            throw new InvalidOperationException("Not in voting phase");

        game.Votes[userId] = vote;

        if (game.Votes.Count == game.AlivePlayers.Count())
        {
            var approveCount = game.Votes.Values.Count(v => v);
            bool passed = approveCount > game.Votes.Count / 2;

            if (passed)
            {
                game.CheckWinCondition();
                if (game.IsGameOver) return;
                game.PresidentHand = game.Draw(3);
                game.Phase = GamePhase.LegislativePresident;
                game.ElectionTracker = 0;
            }
            else
            {
                game.Phase = GamePhase.Nomination;
                game.ElectionTracker++;

                if (game.ElectionTracker >= 3)
                {
                    var topPolicy = game.Draw(1).First();
                    game.EnactPolicy(topPolicy, enactedByElectionTracker: true);
                    game.ElectionTracker = 0;
                }

                game.AdvancePresident();
            }

            game.Votes.Clear();
        }
    }



    public void PresidentDiscardsOne(string roomId, PolicyType discarded)
    {
        var game = GetGame(roomId);
        if (game.Phase != GamePhase.LegislativePresident)
            throw new InvalidOperationException("Wrong phase");

        if (!game.PresidentHand.Contains(discarded))
            throw new ArgumentException("President doesn't have this card");

        game.PresidentHand.Remove(discarded);
        game.DiscardPile.Add(discarded);
        game.ChancellorHand = new List<PolicyType>(game.PresidentHand);
        game.Phase = GamePhase.LegislativeChancellor;
    }

    public void ChancellorEnactsPolicy(string roomId, PolicyType chosen)
    {
        var game = GetGame(roomId);
        if (game.Phase != GamePhase.LegislativeChancellor)
            throw new InvalidOperationException("Not in chancellor phase");

        if (!game.ChancellorHand.Contains(chosen))
            throw new ArgumentException("Invalid policy");

        // Discard the other card
        game.ChancellorHand.Remove(chosen);
        game.DiscardPile.AddRange(game.ChancellorHand);
        game.ChancellorHand.Clear();

        // Enact policy
        game.EnactPolicy(chosen);

        // ✅ Check win condition
        game.CheckWinCondition();
        if (game.IsGameOver) return;

        // ✅ Check for executive action (fascist policy only)
        if (chosen == PolicyType.Fascist)
        {
            var action = game.GetExecutiveAction(); // returns enum or null

            if (action != null)
            {
                game.CurrentExecutiveAction = action;
                game.Phase = GamePhase.Executive;
                return;
            }
        }

        // ✅ No win or executive action, continue normal flow
        game.Phase = GamePhase.Nomination;
        game.PreviousPresidentId = game.PresidentId;
        game.PreviousChancellorId = game.ChancellorId;
        game.AdvancePresident();
    }

    public void ExecutePlayer(string roomId, string targetUserId)
    {
        var game = GetGame(roomId);

        if (game.Phase != GamePhase.Executive)
            throw new InvalidOperationException("Not in an executive action phase.");

        if (game.CurrentExecutiveAction != ExecutiveAction.Execution)
            throw new InvalidOperationException("Current action is not execution.");

        var target = game.Players.FirstOrDefault(p => p.UserId == targetUserId);
        if (target == null || !target.IsAlive)
            throw new ArgumentException("Invalid or already dead player.");

        // Perform execution
        target.IsAlive = false;

        if (target.Role == Role.Hitler)
        {
            game.EndGame("Liberal");
            return;
        }

        // Reset state and proceed to next round
        game.CurrentExecutiveAction = null;
        game.Phase = GamePhase.Nomination;

        game.PreviousPresidentId = game.PresidentId;
        game.PreviousChancellorId = game.ChancellorId;

        game.AdvancePresident();
    }

    public string InvestigateLoyalty(string roomId, string targetUserId)
    {
        var game = GetGame(roomId);

        if (game.Phase != GamePhase.Executive)
            throw new InvalidOperationException("Not in executive phase.");

        if (game.CurrentExecutiveAction != ExecutiveAction.InvestigateLoyalty)
            throw new InvalidOperationException("Not investigate phase.");

        var target = game.Players.FirstOrDefault(p => p.UserId == targetUserId);
        if (target == null)
            throw new ArgumentException("Invalid target");

        // Determine party affiliation (not exact role)
        string party = target.Role == Role.Liberal ? "Liberal" : "Fascist";

        // Clear executive action and advance phase
        game.CurrentExecutiveAction = null;
        game.Phase = GamePhase.ExecutingAction;

        return party;
    }

    public void SetSpecialElectionPresident(string roomId, string chosenUserId)
    {
        var game = GetGame(roomId);

        if (game.Phase != GamePhase.ExecutingAction)
            throw new InvalidOperationException("Not in executive phase.");

        if (game.CurrentExecutiveAction != ExecutiveAction.SpecialElection)
            throw new InvalidOperationException("Not special election phase.");

        var candidate = game.Players.FirstOrDefault(p => p.UserId == chosenUserId && p.IsAlive);
        if (candidate == null)
            throw new ArgumentException("Invalid or dead player.");



        // ✅ complete executive action
        game.CurrentExecutiveAction = null;
        game.Phase = GamePhase.Nomination;
        game.PreviousPresidentId = game.PresidentId;
        game.PreviousChancellorId = game.ChancellorId;
        game.PresidentId = chosenUserId;
    }

    public void CompleteExecutiveAction(string roomId)
    {
        var game = GetGame(roomId);
        game.CurrentExecutiveAction = null;
        game.Phase = GamePhase.Nomination;
        game.PreviousPresidentId = game.PresidentId;
        game.PreviousChancellorId = game.ChancellorId;
        game.AdvancePresident();
    }

    public void ProposeVeto(string roomId)
    {
        var game = GetGame(roomId);

        if (game.Phase != GamePhase.LegislativeChancellor)
            throw new InvalidOperationException("Not in chancellor phase.");

        if (game.EnactedFascistPolicies < 5)
            throw new InvalidOperationException("Veto is not yet unlocked.");

        game.VetoProposed = true;
        game.Phase = GamePhase.VetoPending;
    }

    public void HandleVetoResponse(string roomId, bool approved)
    {
        var game = GetGame(roomId);

        if (game.Phase != GamePhase.VetoPending || !game.VetoProposed)
            throw new InvalidOperationException("No veto is pending.");

        if (approved)
        {
            // Discard both policies
            game.DiscardPile.AddRange(game.ChancellorHand);
            game.ChancellorHand.Clear();

            game.ElectionTracker++;

            if (game.ElectionTracker >= 3)
            {
                var top = game.Draw(1).First();
                game.EnactPolicy(top, enactedByElectionTracker: true);
                game.ElectionTracker = 0;
            }

            game.Phase = GamePhase.Nomination;
            game.PreviousPresidentId = game.PresidentId;
            game.PreviousChancellorId = game.ChancellorId;
            game.AdvancePresident();
        }
        else
        {
            // President rejects veto — back to chancellor's decision
            game.Phase = GamePhase.LegislativeChancellor;
        }

        game.VetoProposed = false;
    }


}
