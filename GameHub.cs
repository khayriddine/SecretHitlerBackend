using Microsoft.AspNetCore.SignalR;
using SecretHitlerBackend.Models;
using SecretHitlerBackend.Services;
using System.Collections.Concurrent;

public class GameHub : Hub
{
    // Track connected players and game rooms
    private static Dictionary<string, Room> _rooms = new();
    private static readonly ConcurrentDictionary<string, string> _connections = new();
    private readonly GameService _gameService;

    public GameHub(GameService gameService)
    {
        _gameService = gameService;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var kvp = _connections.FirstOrDefault(elt => elt.Value == Context.ConnectionId);
        if (kvp.Key != null){
            await Clients.All.SendAsync("UserDisconnected", kvp.Key);
            _connections.TryRemove(kvp.Key, out _);
        }
        await base.OnDisconnectedAsync(exception);
    }
    public async Task ReconnectPlayer(string roomId, string userId)
    {
        if (_connections.TryGetValue(userId, out var oldConnectionId))
        {
            // Update to new connection ID
            _connections[userId] = Context.ConnectionId;

            if (_rooms.TryGetValue(roomId, out var room))
            {
                //room.UpdatePlayerConnection(playerId, Context.ConnectionId);
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                await Clients.Caller.SendAsync("RoomUpdated", room);
                if(_gameService.GetGame(roomId) is Game game)
                    await Clients.Caller.SendAsync("GameUpdated", game);
            }
        }
    }

    public async Task ConnectUser(string userId)
    {
        _connections.TryAdd(userId, Context.ConnectionId);
        await Clients.Caller.SendAsync("UserConnected");
    }

    // Member create a game room
    public async Task CreateRoom(string roomId, string userId, string playerName)
    {
        try
        {
            var room = new Room(roomId, playerName);
            room.Members.Add(new Member( playerName, userId));
            _rooms.Add(roomId, room);
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await Clients.Group(roomId).SendAsync("RoomUpdated", room);
        }
        catch
        {

        }
    }

    public async Task SetAvatar(string roomId, string userId, string avatar)
    {
        if (_rooms.TryGetValue(roomId, out var room))
        {
            if (_gameService.GetGame(roomId) is Game game)
            {
                game.SetAvatar(userId, avatar);
                await SendGameUpdates(roomId);
            }
        }
    }

    // Member joins a game room
    public async Task JoinRoom(string roomId, string userId,string playerName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        if (!_rooms.ContainsKey(roomId))
        {

        }

        _rooms[roomId].Members.Add(new Member(playerName, userId));

        await Clients.Group(roomId).SendAsync("RoomUpdated", _rooms[roomId]);
    }

    public async Task LeaveRoom(string roomId, string userId)
    {
        if (_rooms.TryGetValue(roomId, out var room))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            room.RemoveMember(userId);
            await Clients.Group(roomId).SendAsync("RoomUpdated", _rooms[roomId]);
        }
    }

    public async Task KickFromRoom(string roomId, string userId)
    {
        if (_rooms.TryGetValue(roomId, out var room))
        {
            if(_connections.TryGetValue(userId, out var connection))
            {
                await Groups.RemoveFromGroupAsync(connection, roomId);
            }

            room.RemoveMember(userId);
            await Clients.Group(roomId).SendAsync("RoomUpdated", _rooms[roomId]);
        }
    }

    // List All members
    public async Task GetRoomUpdates(string roomId)
    {
        if (_rooms.ContainsKey(roomId))
        {
            await Clients.Group(roomId).SendAsync("RoomUpdated", _rooms[roomId]);
        }
    }

    #region "Game"
    public async Task StartGame(string roomId)
    {
        if(_rooms.TryGetValue(roomId, out var room))
        {
            if (_gameService.CreateGame(room) is Game game)
            {
                _gameService.StartGame(roomId);
                await Clients.Group(roomId).SendAsync("GameStarted");
            }

        }
    }



    public async Task GetGameUpdates(string roomId)
    {
        await SendGameUpdates(roomId);
    }

    public async Task NominateChancellor(string roomId, string nomineeId)
    {
        _gameService.ChooseChancellor(roomId, nomineeId);
        await SendGameUpdates(roomId);
    }

    public async Task CastVote(string roomId, string playerId, bool vote)
    {
        _gameService.CastVote(roomId, playerId, vote);

        var game = _gameService.GetGame(roomId);
        if (game.IsGameOver)
        {
            await Clients.Group(roomId).SendAsync("GameOver", game.WinningTeam);
        }

        await SendGameUpdates(roomId);
    }

    public async Task PresidentDiscardsOne(string roomId, PolicyType discarded)
    {
        _gameService.PresidentDiscardsOne(roomId, discarded);

        var game = _gameService.GetGame(roomId);
        if (game.IsGameOver)
        {
            await Clients.Group(roomId).SendAsync("GameOver", game.WinningTeam);
        }

        await SendGameUpdates(roomId);
    }

    public async Task ChancellorEnactsPolicy(string roomId, PolicyType policy)
    {
        _gameService.ChancellorEnactsPolicy(roomId, policy);

        var game = _gameService.GetGame(roomId);
        if (game.IsGameOver)
        {
            await Clients.Group(roomId).SendAsync("GameOver", game.WinningTeam);
        }

        await SendGameUpdates(roomId);
    }

    public async Task ExecutePlayer(string roomId, string targetUserId)
    {
        _gameService.ExecutePlayer(roomId, targetUserId);

        var game = _gameService.GetGame(roomId);

        if (game.IsGameOver)
        {
            await Clients.Group(roomId).SendAsync("GameOver", game.WinningTeam);
        }

        await SendGameUpdates(roomId);
    }

    public async Task SetSpecialElectionPresident(string roomId, string chosenUserId)
    {
        _gameService.SetSpecialElectionPresident(roomId, chosenUserId);
        await SendGameUpdates(roomId);
    }

    public async Task CompleteExcutionAction(string roomId)
    {
        _gameService.CompleteExecutiveAction(roomId);
        await SendGameUpdates(roomId);
    }

    public async Task ProposeVeto(string roomId)
    {
        _gameService.ProposeVeto(roomId);
        await SendGameUpdates(roomId);
    }

    public async Task HandleVetoResponse(string roomId, bool approved)
    {
        _gameService.HandleVetoResponse(roomId, approved);
        await SendGameUpdates(roomId);
    }
    #endregion


    // Player submits a vote
    //public async Task SubmitVote(string roomId, string playerName, bool vote)
    //{
    //    if (_rooms.TryGetValue(roomId, out var room))
    //    {
    //        room.RecordVote(playerName, vote);
    //        await Clients.Group(roomId).SendAsync("VoteReceived", playerName, vote);
    //    }
    //}

    private async Task SendGameUpdates(string roomId)
    {
        if (_rooms.TryGetValue(roomId, out var room))
        {
            if (_gameService.GetGame(roomId) is Game game)
            {
                await Clients.Group(roomId).SendAsync("GameUpdated", game);
            }
        }
    }
}

// Game state models


