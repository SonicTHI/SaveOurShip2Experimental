
namespace SaveOurShip2
{
	using Verse;

	public class HediffCompProperties_Bubble : HediffCompProperties
	{
		public ThingDef customMote;
		public float scale = 1.0f;

		public HediffCompProperties_Bubble()
		{
			compClass = typeof(HediffComp_Bubble);
		}
	}
}
