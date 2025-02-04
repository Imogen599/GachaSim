using System.Diagnostics;
using static GachaSim.Banners;

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

	public delegate bool ShouldEndPullingDelegate(PullResult result, BannerType bannerType);

	public class Program
	{
		public static int InitialPullCount
		{
			get;
			private set;
		}

		public static readonly ShouldEndPullingDelegate EndPullingIfNonStandard5Star = (result, bannerType) => result.Result.Rarity == Rarity.FiveStar && !Banner.IsStandard5Star(result.Result.Name, bannerType);

		public static readonly ShouldEndPullingDelegate EndPullingIfAny5Star = (result, _) => result.Result.Rarity == Rarity.FiveStar;

		public static readonly ShouldEndPullingDelegate DoNotEnd = (_, _) => false;

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
			var templateTribbieBanner = ConstructTribbieCharacterBanner();
			var templateCastoriceBanner = ConstructCastoriceCharacterBanner();
			var templateCastoriceLCBanner = ConstructCastoriceLightConeBanner();

			// Time how long the simulations take. Basic form of benchmarking, but good enough for this program.
			var watch = new Stopwatch();
			watch.Start();

			// ↓ Timed code.
			for (int i = 0; i < Constants.SimulationCount; i++)
			{
				results[i] = [];

				var sessionResult = SimulatePullSession(22, TribbieCharacterBanner, DoNotEnd);
				results[i].Add(sessionResult);
				TransferBannerStats(TribbieCharacterBanner, CastoriceCharacterBanner);
				var sessionResult2 = SimulatePullSession(InitialPullCount - 22, CastoriceCharacterBanner, EndPullingIfNonStandard5Star);
				results[i].Add(sessionResult2);
				var sessionResult3 = SimulatePullSession(sessionResult2.RemainingPulls, CastoriceLightConeBanner, EndPullingIfNonStandard5Star);
				results[i].Add(sessionResult3);

				// Reset the stats by transfering the template ones to the main banners after the simulation has finished.
				// This makes the total simulation time *slightly* faster, but this is a bit inconsistent.
				TransferBannerStats(templateTribbieBanner, TribbieCharacterBanner);
				TransferBannerStats(templateCastoriceBanner, CastoriceCharacterBanner);
				TransferBannerStats(templateCastoriceLCBanner, CastoriceLightConeBanner);
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

		public static PullSessionResult SimulatePullSession(int initialPullCount, Banner banner, ShouldEndPullingDelegate shouldEndPulling)
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

				if (shouldEndPulling(result, banner.BannerType))
					break;
			}

			return new(banner, initialPullCount, pulls, results, resultsByRarity, additionalPulls);
		}

		// For speed, the analysis should be done *during* the result collection, instead of afterwards to remove duplicate looping, but I'm kinda lazy to change it again, and its only like 100ms with
		// the above test conditions (102 pulls, 100,000 simulations) compared to the ~2300ms that the simulations take to run.
		public static void AnalyzeAndDisplayResults(List<PullSessionResult>[] simulationResults, int maxNumberOfPulls, Stopwatch watch)
		{
			var simulationTime = watch.ElapsedMilliseconds;
			watch.Restart();
			int bannersPulledOn = simulationResults[0].Count;
			Analysis[] analysises =
				[
				new Successful5StarPullsAnalysis(bannersPulledOn),
				new BannerWasSuccessfulAnalysis(bannersPulledOn, 1),
				new Unique5StarPullsAnalysis(bannersPulledOn),
				new AdditionalPullsAnalysis(bannersPulledOn),
				new RemainingPullsAnalysis(bannersPulledOn),
				new MostFiveStarsPulledAnalysis(bannersPulledOn)
				];

			// ↓ Timed code.
			for (int i = 0; i < Constants.SimulationCount; i++)
            {
                for (int j = 0; j < simulationResults[i].Count; j++)
				{
					var bannerResult = simulationResults[i][j];

					for (int k = 0; k < analysises.Length; k++)
						analysises[k].Analyse(i, ref bannerResult, j);
				}
			}
			// ↑

			watch.Stop();

			Console.WriteLine("-----------------------------------------------------------------------------------------------------------------------------------------------------");
			Console.WriteLine("Analysis:");
			Console.WriteLine();

			// Basic stats about the simulations.
			Console.WriteLine($"A total of \"{Constants.SimulationCount}\" simulations were ran in \"{simulationTime}ms\". Analysis took \"{watch.ElapsedMilliseconds}ms\".");
			Console.WriteLine($"There were \"{maxNumberOfPulls}\" max pulls available per simulation.");
			Console.WriteLine();

			for (int i = 0; i < analysises.Length; i++)
			{
				var lines = analysises[i].SummarizeAnalysis();
				foreach (var line in lines)
					Console.WriteLine(line);
				Console.WriteLine();
			}


			while (true)
			{
				Console.WriteLine("-----------------------------------------------------------------------------------------------------------------------------------------------------");
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
				Console.WriteLine();
				Console.WriteLine("-----------------------------------------------------------------------------------------------------------------------------------------------------");
				foreach (var banner in selectedSim)
					DisplayResults(banner);
				Console.WriteLine();
			}
		}

		public static void DisplayResults(PullSessionResult sessionResult)
		{
			Console.WriteLine();
			if (sessionResult.ResultsByRarity[Rarity.FiveStar].Count != 0)
			{
				foreach (var fiveStar in sessionResult.ResultsByRarity[Rarity.FiveStar])
					Console.WriteLine($"5* in {fiveStar.Pity} pulls" + (sessionResult.Banner.IsStandard5Star(fiveStar.Result.Name) ? "." : $" with {sessionResult.RemainingPulls} pulls remaining."));
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
