using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Fortified
{
	public class LordToil_DefendRoom : LordToil
	{
		public override IntVec3 FlagLoc => Data.stagingPoint;

		private LordToilData_Stage Data => (LordToilData_Stage)data;

		public override bool ForceHighStoryDanger => true;

		public LordToil_DefendRoom(IntVec3 stagingLoc)
		{
			data = new LordToilData_Stage();
			Data.stagingPoint = stagingLoc;
		}

		public override void UpdateAllDuties()
		{
			LordToilData_Stage lordToilData_Stage = Data;
			for (int i = 0; i < lord.ownedPawns.Count; i++)
			{
				lord.ownedPawns[i].mindState.duty = new PawnDuty(FFF_DefOf.FFF_DefendRoom, lordToilData_Stage.stagingPoint);
				lord.ownedPawns[i].mindState.duty.radius = 28f;
			}
		}
	}

	public class LordJob_DefendRoom : LordJob
	{
		public IntVec3 position;

		public string attackSignal = "";

		public int ticksTillFallback = 2500;

		public int ticksTillBackToWork = 5000;

		public LordJob_DefendRoom()
		{
		}

		public LordJob_DefendRoom(IntVec3 position, string attackSignal = "", int ticksTillFallback = 2500, int ticksTillBackToWork = 5000)
		{
			this.position = position;
			this.attackSignal = attackSignal;
			this.ticksTillFallback = ticksTillFallback;
			this.ticksTillBackToWork = ticksTillBackToWork;
		}

		public override StateGraph CreateGraph()
		{
			StateGraph stateGraph = new StateGraph();
			LordToil_DefendRoom lordToil_Stage = (LordToil_DefendRoom)(stateGraph.StartingToil = new LordToil_DefendRoom(position));
			LordToil_AssaultColony lordToil_AssaultColony = new LordToil_AssaultColony();
			stateGraph.AddToil(lordToil_AssaultColony);
			Transition transition1 = new Transition(lordToil_Stage, lordToil_AssaultColony);
			transition1.AddTrigger(new Trigger_PawnHarmed(1f, requireInstigatorWithFaction: false));
			transition1.AddTrigger(new Trigger_Custom((TriggerSignal signal) => ((signal.type == TriggerSignalType.BuildingDamaged || signal.type == TriggerSignalType.BuildingLost) && signal.thing is Building_Turret) || (!attackSignal.NullOrEmpty() && signal.signal.tag == attackSignal)));
			transition1.AddPostAction(new TransitionAction_Custom(delegate (Transition t)
			{
				foreach (Lord lord in t.Map.lordManager.lords)
				{
					lord.Notify_SignalReceived(new Signal(attackSignal));
				}
			}));
			stateGraph.AddTransition(transition1);

			Transition transition2 = new Transition(lordToil_AssaultColony, lordToil_Stage);
			transition2.AddTrigger(new Trigger_TicksPassedWithoutHarm(ticksTillFallback));
			stateGraph.AddTransition(transition2);

			return stateGraph;
		}

		private bool AnyAsleep()
		{
			for (int i = 0; i < lord.ownedPawns.Count; i++)
			{
				if (lord.ownedPawns[i].Spawned && !lord.ownedPawns[i].Dead && !lord.ownedPawns[i].Awake())
				{
					return true;
				}
			}
			return false;
		}

		public override void ExposeData()
		{
			Scribe_Values.Look(ref position, "position");
			Scribe_Values.Look(ref ticksTillFallback, "ticksTillFallback");
			Scribe_Values.Look(ref ticksTillBackToWork, "ticksTillBackToWork");
			Scribe_Values.Look(ref attackSignal, "attackSignal");
		}
	}
}
