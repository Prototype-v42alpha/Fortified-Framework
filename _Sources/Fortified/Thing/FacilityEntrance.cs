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
	public class FacilityEntrance : MapPortal
	{
		protected override void Tick()
		{
			if (this.IsHashIntervalTick(250) && PocketMap != null)
			{
				foreach (Lord lord in Map.lordManager.lords.ToList())
				{
					if (lord.faction.IsPlayer == false && !GenHostility.AnyHostileActiveThreatTo(Map, lord.faction))
					{
						List<Pawn> list = lord.ownedPawns.ToList();
						foreach (Pawn p in list)
						{
							Job job = JobMaker.MakeJob(JobDefOf.EnterPortal, this);
							job.checkOverrideOnExpire = false;
							job.expiryInterval = 99999;
							job.expireOnEnemiesNearby = false;
							job.playerForced = true;
							p.jobs.TryTakeOrderedJob(job, JobTag.Misc);
						}
					}
				}
			}
			base.Tick();
		}
	}

	public class FacilityExit : PocketMapExit
	{
		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			if (!respawningAfterLoad && MapGenerator.mapBeingGenerated == map)
			{
				IntVec3 cell = this.OccupiedRect().ExpandedBy(1).EdgeCells.FirstOrDefault((x) => x.Standable(map));
				if(cell == null || !cell.IsValid)
				{
					return;
				}
				MapGenerator.PlayerStartSpot = cell;
			}
		}
	}
}
