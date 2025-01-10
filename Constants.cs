namespace GachaSim
{
	public static class Constants
	{
		public static class CharacterBanner
		{
			public const int MaxPity5StarCharacter = 90;

			public const float CharacterFiftyFifty = 0.5f;

			public const float Average4StarDropRate = 0.051f;

			public const float Average5StarDropRate = 0.006f;

			public const int SoftPityStart = 74;

			// Hand calculated, using public genshin data accounting for the lowest increase roughly that aligns with the user submitted data.
			// https://www.reddit.com/r/Genshin_Impact/comments/o9v0c0/soft_and_hard_pity_explained_based_on_24m_wishes/#lightbox
			public const float SoftPityIncrease = 0.06f;
		}

		public static class LightConeBanner
		{
			public const int MaxPity5StarLightCone = 80;

			public const float LightConeFiftyFifty = 0.75f;

			public const float Average4StarDropRate = 0.066f;

			public const float Average5StarDropRate = 0.008f;

			public const int SoftPityStart = 63;

			// Hand calculated, using genshin data accounting for the lowest increase roughly that aligns with the user submitted data.
			// https://www.reddit.com/r/Genshin_Impact/comments/o9v0c0/soft_and_hard_pity_explained_based_on_24m_wishes/#lightbox
			public const float SoftPityIncrease = 0.067f;
		}

		public static class Stardust
		{
			public const int StardustForPull = 20;

			public const int StardustFromDuplicate4StarIncomplete = 8;

			public const int StardustFromDuplicate4StarComplete = 20;

			public const int StardustFromDuplicate4StarLightCone = 8;
		}

		public const int MaxPity4Star = 10;

		public const int SimulationCount = 100000;
	}
}
