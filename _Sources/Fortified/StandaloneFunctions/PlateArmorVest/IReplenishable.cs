using Verse;

namespace Fortified;

public interface IReplenishable
{
    // How many materials are required to fully refill given current state
    int GetMaterialCostForRefill();

    // How much each material restores (units meaningful to the implementor)
    float DurabilityRestorePerMaterial { get; }

    // Perform the replenish action: actor performed by pawn, using materialCount items
    void Replenish(Pawn actor, int materialCount);
}