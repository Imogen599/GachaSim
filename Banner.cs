namespace GachaSim
{
	public static class Banners
	{
		#region Banners
		public static Banner AglaeaCharacterBanner
		{
			get;
			private set;
		} = ConstructAglaeaCharacterBanner();

		public static Banner AglaeaLightConeBanner
		{
			get;
			private set;
		} = ConstructAglaeaLightConeBanner();

		public static Banner TribbieCharacterBanner
		{
			get;
			private set;
		} = ConstructTribbieCharacterBanner();

		public static Banner CastoriceCharacterBanner
		{
			get;
			private set;
		} = ConstructCastoriceCharacterBanner();

		public static Banner CastoriceLightConeBanner
		{
			get;
			private set;
		} = ConstructCastoriceLightConeBanner();
		#endregion

		#region Construct Banners
		public static Banner ConstructAglaeaCharacterBanner()
			=> new(BannerType.Character, new("Aglaea", Rarity.FiveStar),
			[new("Tingyun", Rarity.FourStar), new("Hanya", Rarity.FourStar), new("Sushang", Rarity.FourStar)],
			pity: 42,
			pityFourStar: 2,
			guarenteeFiveStar: true,
			guarenteeFourStar: false);

		public static Banner ConstructAglaeaLightConeBanner()
			=> new(BannerType.LightCone, new("Time Woven Into Gold", Rarity.FiveStar),
			[new("4* Remember", Rarity.FourStar), new("Subscribe for More!", Rarity.FourStar), new("Dance! Dance! Dance!", Rarity.FourStar)],
			pity: 21,
			pityFourStar: 2,
			guarenteeFiveStar: false,
			guarenteeFourStar: false);

		public static Banner ConstructTribbieCharacterBanner()
			=> new(BannerType.Character, new("Tribbie", Rarity.FiveStar),
			[new("Unknown1", Rarity.FourStar), new("Unknown2", Rarity.FourStar), new("Unknown3", Rarity.FourStar)],
			pity: 35,
			pityFourStar: 4,
			guarenteeFiveStar: false,
			guarenteeFourStar: false);

		public static Banner ConstructCastoriceCharacterBanner()
			=> new(BannerType.Character, new("Castorice", Rarity.FiveStar),
			[new("Unknown4", Rarity.FourStar), new("Unknown5", Rarity.FourStar), new("Unknown6", Rarity.FourStar)],
			pity: 35,
			pityFourStar: 4,
			guarenteeFiveStar: false,
			guarenteeFourStar: false);

		public static Banner ConstructCastoriceLightConeBanner()
			=> new(BannerType.LightCone, new("Castorice Light Cone", Rarity.FiveStar),
			[new("Unknown 7", Rarity.FourStar), new("Uknown 8", Rarity.FourStar), new("Unknown 9", Rarity.FourStar)],
			pity: 3,
			pityFourStar: 1,
			guarenteeFiveStar: false,
			guarenteeFourStar: false);
		#endregion
	}

	public class Banner(BannerType bannerType, CharacterOrLightCone fiveStar, List<CharacterOrLightCone> fourStars, int pity = 0, int pityFourStar = 0, bool guarenteeFiveStar = false, bool guarenteeFourStar = false)
	{
		public CharacterOrLightCone FiveStar = fiveStar;

		public readonly BannerType BannerType = bannerType;

		public static readonly CharacterOrLightCone Standard5StarCharacter = new(Standard5StarCharacterName, Rarity.FiveStar);

		public static readonly CharacterOrLightCone Standard5StarLightCone = new(Standard5StarLightConeName, Rarity.FiveStar);

		public static readonly CharacterOrLightCone Standard4Star = new(Standard4StarName, Rarity.FourStar);

		public static readonly CharacterOrLightCone ThreeStarLightCone = new("Generic LightCone", Rarity.ThreeStar);

		public const string Standard5StarCharacterName = "Standard Character";

		public const string Standard5StarLightConeName = "Standard Light Cone";

		public const string Standard4StarName = "Standard";

		public List<CharacterOrLightCone> FourStarCharacters = fourStars;

		public int Pity = pity;

		public int PityFourStar = pityFourStar;

		public bool GuarenteeFourStar = guarenteeFourStar;

		public bool GuarenteeFiveStar = guarenteeFiveStar;

		private static readonly Random Random = new(DateTime.Now.Microsecond);

		public PullResult Pull(out int stardust)
		{
			stardust = 0;
			PullResult result;
			PityFourStar++;
			Pity++;

			// Slowest part, can't really speed it up much :/.
			float randomNumber = Random.NextSingle();

			bool isCharacterBanner = BannerType is BannerType.Character;

			float initialCheck = isCharacterBanner ? Constants.CharacterBanner.Average5StarDropRate : Constants.LightConeBanner.Average5StarDropRate;
			
			// Soft pity, calcuated by user inputted pull data.
			if (isCharacterBanner && Pity >= Constants.CharacterBanner.SoftPityStart)
			{
				int difference = Pity - Constants.CharacterBanner.SoftPityStart + 1;
				initialCheck += difference * Constants.CharacterBanner.SoftPityIncrease;
			}
			else if (Pity >= Constants.LightConeBanner.SoftPityStart)
			{
				int difference = Pity - Constants.LightConeBanner.SoftPityStart + 1;
				initialCheck += difference * Constants.LightConeBanner.SoftPityIncrease;
			}

			// You pulled a 5star!
			if (Pity == (isCharacterBanner ? Constants.CharacterBanner.MaxPity5StarCharacter : Constants.LightConeBanner.MaxPity5StarLightCone) || randomNumber <= initialCheck)
			{
				// Check for 50/50.
				float fiftyCheck = isCharacterBanner ? Constants.CharacterBanner.CharacterFiftyFifty : Constants.LightConeBanner.LightConeFiftyFifty;
				float randomNumber2 = Random.NextSingle();

				if (GuarenteeFiveStar || randomNumber2 <= fiftyCheck)
				{
					result = new(FiveStar, Pity, PityFourStar, GuarenteeFiveStar);
					GuarenteeFiveStar = false;
					Pity = 0;
					return result;
				}
				else
				{
					result = new(isCharacterBanner ? Standard5StarCharacter : Standard5StarLightCone, Pity, PityFourStar, GuarenteeFiveStar);
					GuarenteeFiveStar = true;
					Pity = 0;
					return result;
				}
			}

			// Add on the 4star range.
			float secondCheck = initialCheck + (isCharacterBanner ? Constants.CharacterBanner.Average4StarDropRate : Constants.LightConeBanner.Average4StarDropRate);

			// You pulled a 4star!
			if (PityFourStar == Constants.MaxPity4Star || (randomNumber > initialCheck  && randomNumber <= secondCheck))
			{

				float fiftyCheck = isCharacterBanner ? Constants.CharacterBanner.CharacterFiftyFifty : Constants.LightConeBanner.LightConeFiftyFifty;
				float randomNumber2 = Random.NextSingle();

				if (GuarenteeFourStar || randomNumber2 <= fiftyCheck)
				{
					result = new(FourStarCharacters[Random.Next(0, FourStarCharacters.Count)], Pity, PityFourStar, GuarenteeFourStar);
					stardust = CheckForStardustAmount(result.Result);
					GuarenteeFourStar = false;
					PityFourStar = 0;
					return result;
				}
				else
				{
					result = new(Standard4Star, Pity, PityFourStar, GuarenteeFourStar);
					GuarenteeFourStar = true;
					PityFourStar = 0;
					return result;
				}
			}

			// Last, return a generic 3star.
			result = new(ThreeStarLightCone, Pity, PityFourStar);

			return result;
		}

		// Could potentially need to be used for 5*, if simulating multiple copies of a unique one, however that is not the case, so this is only
		// used for pulling 4*.
		public int CheckForStardustAmount(CharacterOrLightCone characterOrLightCone)
		{
			if (BannerType is BannerType.Character)
			{
				if (CharacterEidolonCatalogue.OwnedCharacters.TryGetValue(characterOrLightCone.Name, out var existingCopies))
				{
					CharacterEidolonCatalogue.OwnedCharacters[characterOrLightCone.Name]++;
					if (existingCopies is > 0 and < 6)
						return Constants.Stardust.StardustFromDuplicate4StarIncomplete;
					else if (existingCopies is 0)
						return 0;
					else
						return Constants.Stardust.StardustFromDuplicate4StarComplete;
				}

				if (IsStandard4Star(characterOrLightCone.Name))
					return 8;

				CharacterEidolonCatalogue.OwnedCharacters.Add(characterOrLightCone.Name, 1);
				return 0;
			}

			return Constants.Stardust.StardustFromDuplicate4StarLightCone;
		}

		public bool IsStandard5Star(string name) => IsStandard5Star(name, BannerType);

		public static bool IsStandard5Star(string name, BannerType bannerType) => name == (bannerType is BannerType.Character ? Standard5StarCharacterName : Standard5StarLightConeName);


		public static bool IsStandard4Star(string name) => name == Standard4StarName;
	}
}
