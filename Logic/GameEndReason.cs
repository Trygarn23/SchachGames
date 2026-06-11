namespace Schach.Logic;

public enum GameEndReason
{
    None,
    Checkmate,
    Stalemate,
    FiftyMoveRule,
    ThreefoldRepetition,
    InsufficientMaterial,
    Resignation,
    DrawAgreement,
    Timeout,
    ThreeCheck,
    KingOfTheHill
}
