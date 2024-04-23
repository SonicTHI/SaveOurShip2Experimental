
namespace SaveOurShip2
{
	using Verse;

	public class HediffCompProps_Bubble : HediffCompProperties
	{
		public ThingDef customMote;
		public float scale = 1.0f;

		public HediffCompProps_Bubble()
		{
			compClass = typeof(HediffComp_Bubble);
		}
	}
}
