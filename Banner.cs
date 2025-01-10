namespace GachaSim
{
	public class Banner(BannerType BannerType, CharacterOrLightCone fiveStar, List<CharacterOrLightCone> fourStars, int pity = 0, int pityFourStar = 0, bool guarenteeFiveStar = false, bool guarenteeFourStar = false)
	{
		public CharacterOrLightCone FiveStar = fiveStar;

		public static readonly CharacterOrLightCone Standard5Star = new("Standard", Rarity.FiveStar);

		public static readonly CharacterOrLightCone Standard4Star = new("Standard", Rarity.FourStar);

		public static readonly CharacterOrLightCone ThreeStarLightCone = new("Generic LightCone", Rarity.ThreeStar);

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
					result = new(Standard5Star, Pity, PityFourStar, GuarenteeFiveStar);
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

				if (characterOrLightCone.Name == "Standard")
					return 8;

				CharacterEidolonCatalogue.OwnedCharacters.Add(characterOrLightCone.Name, 1);
				return 0;
			}

			return Constants.Stardust.StardustFromDuplicate4StarLightCone;
		}
	}
}
