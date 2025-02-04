namespace GachaSim
{
	public abstract class Analysis(int bannersPulledOn)
	{
		public readonly int BannersPulledOn = bannersPulledOn;

		public abstract void Analyse(int simulationNumber, ref PullSessionResult pullSessionResult, int currentBanner);

		public abstract IEnumerable<string> SummarizeAnalysis();
	}

	public sealed class Successful5StarPullsAnalysis(int bannersPulledOn) : Analysis(bannersPulledOn)
	{
		private readonly Dictionary<int, List<bool>> SuccessfulBannersPerSim = [];

		public override void Analyse(int simulationNumber, ref PullSessionResult pullSessionResult, int currentBanner)
		{
			// Initialize if first banner in the simulation.
			if (currentBanner == 0)
				SuccessfulBannersPerSim[simulationNumber] = new List<bool>(BannersPulledOn);

			if (pullSessionResult.ResultsByRarity.TryGetValue(Rarity.FiveStar, out var fiveStars))
			{
				var banner = pullSessionResult.Banner;
				if (fiveStars.Any(fiveStar => !banner.IsStandard5Star(fiveStar.Result.Name)))
					SuccessfulBannersPerSim[simulationNumber].Add(true);
				else
					SuccessfulBannersPerSim[simulationNumber].Add(false);
			}
			else
				SuccessfulBannersPerSim[simulationNumber].Add(false);
		}

		public override IEnumerable<string> SummarizeAnalysis()
		{
			// Calculate the probability of being successful based on the simulations. The most important number.
			List<string> lines = [];
			int allBannersSuccessfulCount = SuccessfulBannersPerSim.Where(banners => banners.Value.All(successful => successful)).Count();
			int someBannersSuccessfullCount = SuccessfulBannersPerSim.Where(banners => banners.Value.Any(successful => successful)).Count();

			lines.Add($"There were \"{allBannersSuccessfulCount}\" simulations where all banners were successful out of \"{Constants.SimulationCount}\" simulations, for a success rate of \"{(float)allBannersSuccessfulCount / Constants.SimulationCount * 100f}%\".");
			if (BannersPulledOn > 1)
				lines.Add($"There were \"{someBannersSuccessfullCount}\" simulations where at least one banner was successful out of \"{Constants.SimulationCount}\" simulations, for a success rate of \"{(float)someBannersSuccessfullCount / Constants.SimulationCount * 100f}%\".");
			
			return lines;
		}
	}

	public sealed class BannerWasSuccessfulAnalysis(int bannersPulledOn, int bannerToAnalyse) : Analysis(bannersPulledOn)
	{
		private readonly bool[] SuccessfulFinalBanners = new bool[Constants.SimulationCount];

		public override void Analyse(int simulationNumber, ref PullSessionResult pullSessionResult, int currentBanner)
		{
			if (currentBanner != bannerToAnalyse - 1)
				return;

			if (pullSessionResult.ResultsByRarity.TryGetValue(Rarity.FiveStar, out var fiveStars))
			{
				var banner = pullSessionResult.Banner;
				if (fiveStars.Any(fiveStar => !banner.IsStandard5Star(fiveStar.Result.Name)))
				{
					SuccessfulFinalBanners[simulationNumber] = true;
					return;
				}
			}
			SuccessfulFinalBanners[simulationNumber] = false;
		}

		public override IEnumerable<string> SummarizeAnalysis()
		{
			int successCount = SuccessfulFinalBanners.Where(success => success).Count();
			return [$"There were \"{successCount}\" simulations where banner {bannerToAnalyse} was successful out of \"{Constants.SimulationCount}\" simulations, for a success rate of \"{(float)successCount / Constants.SimulationCount * 100f}%\"."];
		}
	}

	public sealed class Unique5StarPullsAnalysis(int bannersPulledOn) : Analysis(bannersPulledOn)
	{
		private readonly Dictionary<string, List<int>> FiveStarPullDetails = [];

		public override void Analyse(int simulationNumber, ref PullSessionResult pullSessionResult, int currentBanner)
		{
			if (pullSessionResult.ResultsByRarity[Rarity.FiveStar].Count != 0)
			{
				// Store the pity for each 5 star.
				foreach (var fiveStarResult in pullSessionResult.ResultsByRarity[Rarity.FiveStar])
				{
					if (FiveStarPullDetails.TryGetValue(fiveStarResult.Result.Name, out List<int>? value))
						value.Add(fiveStarResult.Pity);
					else
						FiveStarPullDetails.Add(fiveStarResult.Result.Name, [fiveStarResult.Pity]);
				}
			}
		}

		public override IEnumerable<string> SummarizeAnalysis()
		{
			List<string> returnValue = [];
			foreach (var pair in FiveStarPullDetails)
			{
				if (pair.Value.Count == 0)
					continue;

				returnValue.Add($"There were a total of \"{pair.Value.Count}\" of \"{pair.Key}\", with an average of \"{Math.Round(pair.Value.Average(), MidpointRounding.ToEven)}\" pity, with a total average of \"{(float)pair.Value.Count / Constants.SimulationCount * 100f}%\". The minimum pulls was \"{pair.Value.Min()}\" and the maximum was \"{pair.Value.Max()}\"");
			}

			return returnValue;
		}
	}

	public sealed class AdditionalPullsAnalysis(int bannersPulledOn) : Analysis(bannersPulledOn)
	{
		private readonly List<int> AdditionalPulls = [];

		public override void Analyse(int simulationNumber, ref PullSessionResult pullSessionResult, int currentBanner) => AdditionalPulls.Add(pullSessionResult.AdditionalPulls);

		public override IEnumerable<string> SummarizeAnalysis() => [$"There was an average of \"{Math.Round(AdditionalPulls.Average(), MidpointRounding.ToEven)}\" additional pulls with a minimum of \"{AdditionalPulls.Min()}\" and a maximum of \"{AdditionalPulls.Max()}\""];
	}


	public sealed class RemainingPullsAnalysis(int bannersPulledOn) : Analysis(bannersPulledOn)
	{
		private readonly List<int> RemainingPulls = [];

		private (int Simulation, int Pulls) MostPullsRemaining = (0, 0);

		public override void Analyse(int simulationNumber, ref PullSessionResult pullSessionResult, int currentBanner)
		{
			RemainingPulls.Add(pullSessionResult.RemainingPulls);

			if (currentBanner != BannersPulledOn - 1)
				return;

			if (pullSessionResult.RemainingPulls > MostPullsRemaining.Pulls)
				MostPullsRemaining = (simulationNumber, pullSessionResult.RemainingPulls);
		}

		public override IEnumerable<string> SummarizeAnalysis() => [$"There was an average of \"{Math.Round(RemainingPulls.Average(), MidpointRounding.ToEven)}\" pulls remaining with a minimum of \"{RemainingPulls.Min()}\" and a maximum of \"{RemainingPulls.Max()}\" (simulation \"{MostPullsRemaining.Simulation}\")."];
	}

	public sealed class MostFiveStarsPulledAnalysis(int bannersPulledOn) : Analysis(bannersPulledOn)
	{
		private (int Simulation, int AmountPulled) MostFiveStarsPulled = (0, 0);

		private int FiveStarCount;

		public override void Analyse(int simulationNumber, ref PullSessionResult pullSessionResult, int currentBanner)
		{
			// Reset this when looking at the first banner on the simulation.
			if (currentBanner == 0)
				FiveStarCount = 0;

			if (pullSessionResult.ResultsByRarity[Rarity.FiveStar].Count != 0)
				FiveStarCount += pullSessionResult.ResultsByRarity[Rarity.FiveStar].Count;

			if (currentBanner != BannersPulledOn - 1)
				return;

			if (FiveStarCount > MostFiveStarsPulled.AmountPulled)
				MostFiveStarsPulled = (simulationNumber, FiveStarCount);
		}

		public override IEnumerable<string> SummarizeAnalysis() => [$"The most 5 stars pulled was \"{MostFiveStarsPulled.AmountPulled}\" (Simulation \"{MostFiveStarsPulled.Simulation}\")"];
	}
}
