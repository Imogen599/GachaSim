namespace GachaSim
{
	public static class CharacterEidolonCatalogue
	{
		// Manually populated, used to determine stardust amounts.
		/// <summary>
		/// Layout: (Name, AmountOwned).
		/// </summary>
		public static readonly Dictionary<string, int> OwnedCharacters = new()
		{ 
			{"Tingyun", 2},
			{"Hanya", 1},
			{"Sushang", 0}
		};
	}
}