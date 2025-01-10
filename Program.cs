using System.Diagnostics;

namespace GachaSim
{
	public enum Rarity
	{
		ThreeStar,
		FourStar,
		FiveStar
	}

	public enum BannerType
	{
		Character,
		LightCone
	}

	// For 102 pulls at 100,000 simulations.
	// As structs: 2371ms, 2300ms, 2350ms. Analysis: 111ms, 103ms, 107ms. Total: 2482ms, 2403ms, 2457ms
	// As classes: 2091ms, 2233ms, 2134ms. Analysis: 125ms, 125ms, 122ms. Total: 2216ms, 2358ms, 2256ms
	// Not a massive difference, but it is present.
	// ↓
	public readonly record struct PullSessionResult(Banner Banner, int AvailablePulls, int RemainingPulls, List<PullResult> Results, Dictionary<Rarity, List<PullResult>> ResultsByRarity, int AdditionalPulls);

	public readonly record struct PullResult(CharacterOrLightCone Result, int Pity, int FourStarPity, bool? Guarentee = null)
	{
		public override readonly string ToString() => Result.ToString()
			+ $", Pity: {Pity}"
			+ (Result.Rarity == Rarity.FourStar ? $", Pity 4*: {FourStarPity}": string.Empty)
			+ (Result.Rarity != Rarity.ThreeStar ? $", Guarentee: {Guarentee}": string.Empty);
	}
	// ↑

	public readonly record struct CharacterOrLightCone(string Name, Rarity Rarity)
	{
		private readonly string GetRarityAsString() => Rarity switch
		{
			Rarity.FiveStar => "!Five Star!",
			Rarity.FourStar => "Four Star",
			_ => "Three Star"
		};

		public override readonly string ToString() => GetRarityAsString() + $": {Name}";
	}

	public class Program
	{
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

		public static int InitialPullCount
		{
			get;
			private set;
		}

		static void Main()
		{
			while (true)
			{
				Console.WriteLine("Input the number of pulls each simulation should use:");

				var input = Console.ReadLine();
				if (string.IsNullOrEmpty(input))
					continue;

				if (!int.TryParse(input, out var intInput))
					continue;

				InitialPullCount = intInput;
				break;
			}

			List<PullSessionResult>[] results = new List<PullSessionResult>[Constants.SimulationCount];
			var templateBannerAglaeaCharacter = ConstructAglaeaCharacterBanner();
			var templateBannerAglaeaLightCone = ConstructAglaeaLightConeBanner();

			// Time how long the simulations take. Basic form of benchmarking, but good enough for this program.
			var watch = new Stopwatch();
			watch.Start();

			// ↓ Timed code.
			for (int i = 0; i < Constants.SimulationCount; i++)
			{
				results[i] = [];

				var sessionResult = SimulatePullSession(InitialPullCount, AglaeaCharacterBanner);
				var sessionResult2 = SimulatePullSession(sessionResult.RemainingPulls, AglaeaLightConeBanner);
				results[i].Add(sessionResult);
				results[i].Add(sessionResult2);

				// Reset the stats by transfering the template ones to the main banners after the simulation has finished.
				// This makes the total simulation time *slightly* faster, but this is a bit inconsistent.
				TransferBannerStats(templateBannerAglaeaCharacter, AglaeaCharacterBanner);
				TransferBannerStats(templateBannerAglaeaLightCone, AglaeaLightConeBanner);
			}
			// ↑

			watch.Stop();

			AnalyzeAndDisplayResults(results, InitialPullCount, watch);
		}

		public static void TransferBannerStats(Banner previousBanner, Banner newBanner)
		{
			newBanner.Pity = previousBanner.Pity;
			newBanner.PityFourStar = previousBanner.PityFourStar;
			newBanner.GuarenteeFourStar = previousBanner.GuarenteeFourStar;
			newBanner.GuarenteeFiveStar = previousBanner.GuarenteeFiveStar;
		}

		public static PullSessionResult SimulatePullSession(int initialPullCount, Banner banner)
		{
			int pulls = initialPullCount;
			int stardust = 16;
			int additionalPulls = 0;

			List<PullResult> results = [];
			Dictionary<Rarity, List<PullResult>> resultsByRarity = new()
			{
				{ Rarity.ThreeStar, new() },
				{ Rarity.FourStar, new() },
				{ Rarity.FiveStar, new() }
			};

			while (pulls > 0)
			{
				var result = banner.Pull(out var obtainedStardust);
				stardust += obtainedStardust;
				results.Add(result);
				resultsByRarity[result.Result.Rarity].Add(result);

				pulls--;

				// Check to see if a pass can be bought with stardust, if so, add another pull and track its addition.
				if (stardust >= 20)
				{
					stardust -= 20;
					pulls++;
					additionalPulls++;
				}

				var pulledRememberanceCone = false;//result.Result.Name == "4* Remember";
				if ((result.Result.Rarity == Rarity.FiveStar && result.Result.Name != "Standard") || pulledRememberanceCone)
					break;
			}

			return new(banner, initialPullCount, pulls, results, resultsByRarity, additionalPulls);
		}

		// For speed, the analysis should be done *during* the result collection, instead of afterwards to remove duplicate looping, but I'm kinda lazy to change it again, and its only like 100ms with
		// the above test conditions (102 pulls, 100,000 simulations) compared to the ~2300ms that the simulations take to run.
		public static void AnalyzeAndDisplayResults(List<PullSessionResult>[] simulationResults, int maxNumberOfPulls, Stopwatch watch)
		{
			int[] wonBanners = new int[Constants.SimulationCount];
			List<int> additionalPulls = [];
			List<int> remainingPulls = [];
			Dictionary<string, List<int>> fiveStarPullDetails = [];
			bool[] successfulSims = new bool[Constants.SimulationCount];
			(int Simulation, int Pulls) mostPullsRemaining = (0, 0);
			(int Simulation, int AmountPulled) mostFiveStarsPulled = (0, 0);

			var simulationTime = watch.ElapsedMilliseconds;
			watch.Restart();

			// ↓ Timed code.
			for (int i = 0; i < Constants.SimulationCount; i++)
            {
				int fiveStarCount = 0;
                foreach (var bannerResult in simulationResults[i])
				{
					if (bannerResult.ResultsByRarity[Rarity.FiveStar].Count != 0)
					{
						// If banner pulled any non standard 5 star.
						if (bannerResult.ResultsByRarity[Rarity.FiveStar].Any(result => result.Result.Name != "Standard"))
							wonBanners[i]++;

						// Store the pity for each 5 star.
						foreach (var fiveStarResult in bannerResult.ResultsByRarity[Rarity.FiveStar])
						{
							fiveStarCount++;
							if (fiveStarPullDetails.TryGetValue(fiveStarResult.Result.Name, out List<int>? value))
								value.Add(fiveStarResult.Pity);
							else
								fiveStarPullDetails.Add(fiveStarResult.Result.Name, [fiveStarResult.Pity]);
						}
					}

					additionalPulls.Add(bannerResult.AdditionalPulls);
				}

				var finalBannerRemainingPulls = simulationResults[i].Last().RemainingPulls;
				remainingPulls.Add(finalBannerRemainingPulls);

				if (finalBannerRemainingPulls > mostPullsRemaining.Pulls)
					mostPullsRemaining = (i, finalBannerRemainingPulls);

				if (fiveStarCount > mostFiveStarsPulled.AmountPulled)
					mostFiveStarsPulled = (i, fiveStarCount);

				// Simulations that still have pulls left must have pulled the limited 5* on all of their banners, and are thus successfull.
				if (simulationResults[i][^1].RemainingPulls != 0)
					successfulSims[i] = true;
			}
			// ↑

			watch.Stop();

			// Basic stats about the simulations.
            Console.WriteLine($"A total of \"{Constants.SimulationCount}\" simulations were ran in \"{simulationTime}ms\". Analysis took \"{watch.ElapsedMilliseconds}ms\".");
			Console.WriteLine($"There were \"{maxNumberOfPulls}\" max pulls available per simulation.");
			Console.WriteLine();

			// Calculate the probability of being successful based on the simulations. The most important number.
			int successfulSimCount = successfulSims.Where(successful => successful).Count();
			Console.WriteLine($"There were \"{successfulSimCount}\" successful simulations out of \"{Constants.SimulationCount}\" simulations, for a success rate of \"{(float)successfulSimCount / Constants.SimulationCount * 100f}%\".");
			Console.WriteLine();

			// Log each number of unique limited 5 stars pulled.
			foreach (var pair in fiveStarPullDetails)
			{
				if (pair.Value.Count == 0)
					continue;

				Console.WriteLine($"There were a total of \"{pair.Value.Count}\" of \"{pair.Key}\", with an average of \"{Math.Round(pair.Value.Average(), MidpointRounding.ToEven)}\" pity, with a total average of \"{(float)pair.Value.Count / Constants.SimulationCount * 100f}%\". The minimum pulls was \"{pair.Value.Min()}\" and the maximum was \"{pair.Value.Max()}\"");
			}

			// Log the average additional and remaining pulls.
			Console.WriteLine();
			Console.WriteLine($"There was an average of \"{Math.Round(additionalPulls.Average(), MidpointRounding.ToEven)}\" additional pulls with a minimum of \"{additionalPulls.Min()}\" and a maximum of \"{additionalPulls.Max()}\"");
			Console.WriteLine($"There was an average of \"{Math.Round(remainingPulls.Average(), MidpointRounding.ToEven)}\" pulls remaining with a minimum of \"{remainingPulls.Min()}\" and a maximum of \"{remainingPulls.Max()}\" (simulation \"{mostPullsRemaining.Simulation}\").");
			Console.WriteLine($"The most 5 stars pulled was \"{mostFiveStarsPulled.AmountPulled}\" (simulation \"{mostFiveStarsPulled.Simulation}\")");
			while (true)
			{
				Console.WriteLine();
				Console.WriteLine($"Input a number from 1-{Constants.SimulationCount} (inclusive) to view the pulling data for the related simulation. Input anything else to exit.");
				string? input = Console.ReadLine();

				if (string.IsNullOrEmpty(input))
					break;

				if (!int.TryParse(input, out var intInput))
					break;

				if (intInput < 1 && intInput > Constants.SimulationCount)
					break;

				var selectedSim = simulationResults[intInput];

				Console.WriteLine("-----------------------------------------------------------------------------------------------------------------------------------------------------");
				foreach (var banner in selectedSim)
					DisplayResults(banner);
			}
		}

		public static void DisplayResults(PullSessionResult sessionResult)
		{
			Console.WriteLine();
			if (sessionResult.ResultsByRarity[Rarity.FiveStar].Count != 0)
			{
				foreach (var fiveStar in sessionResult.ResultsByRarity[Rarity.FiveStar])
					Console.WriteLine($"5* in {fiveStar.Pity} pulls" + (fiveStar.Result.Name == "Standard" ? "." : $" with {sessionResult.RemainingPulls} pulls remaining."));
			}
			else
				Console.WriteLine("No 5* pulled.");

			Console.WriteLine($"There were \"{sessionResult.RemainingPulls}\" pulls remaining.");
			Console.WriteLine($"There were \"{sessionResult.AdditionalPulls}\" additional pulls");

			Console.WriteLine();

			foreach (var result in sessionResult.Results)
			{
				if (result.Result.Rarity == Rarity.ThreeStar)
					continue;

				Console.WriteLine(result.ToString());
			}
		}
	}
}
