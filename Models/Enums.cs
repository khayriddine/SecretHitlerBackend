namespace SecretHitlerBackend.Models;

public enum Role
{
    Liberal,
    Fascist,
    Hitler
}

public enum PolicyType
{
    Liberal,
    Fascist
}

public enum GamePhase
{
    Setup,
    Nomination,
    Voting,
    LegislativePresident,
    LegislativeChancellor,
    VetoPending,
    Executive,
    ExecutingAction,
    GameOver,
}

public enum ExecutiveAction
{
    InvestigateLoyalty,
    PolicyPeek,
    Execution,
    SpecialElection
}