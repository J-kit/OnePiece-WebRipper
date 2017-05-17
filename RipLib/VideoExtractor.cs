using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RipLib
{
	public class VideoExtractor
	{
		private static Regex _rgxOpTubeAniStream = new Regex(@"<iframe src=""(.*?)"".*?<\/ifram", RegexOptions.Compiled | RegexOptions.ECMAScript);
		private static Regex _rgxOpMp4 = new Regex(@"file: ['""](.*?.mp4)[""']", RegexOptions.Compiled | RegexOptions.ECMAScript);
		private static Regex _rgxTitle = new Regex(@"<title>(.*?)<\/title>", RegexOptions.Compiled | RegexOptions.ECMAScript);
		private static Regex _rgxEpisodeMax = new Regex(@"<b>Anime Folge (\d+)", RegexOptions.Compiled | RegexOptions.ECMAScript);

		public static async Task<int> GetCurEpisode()
		{
			var wsString = await (new WebClient() { Proxy = null, Encoding = Encoding.UTF8 }).DownloadStringTaskAsync("http://onepiece-tube.com/");
			var epiMax = _rgxEpisodeMax.Match(wsString)?.Groups[1].Value;
			return Convert.ToInt32(epiMax);
		}

		public static async Task<IEnumerable<VideoInfo>> DoExtractAsync(string opTubeLink)
		{
			var wsString = await (new WebClient() { Proxy = null, Encoding = Encoding.UTF8 }).DownloadStringTaskAsync(opTubeLink);
			var prVal = _rgxOpTubeAniStream.MatchesMinGroups(wsString, 1);
			var value = prVal.Select(a => a.Groups[1].Value).LastOrDefault(x => x.Contains("ani-stream.com"));
			var videoName = _rgxTitle.MatchesMinGroups(wsString, 1).FirstOrDefault()?.Groups[1].Value;

			var videoString = await (new WebClient() { Proxy = null, Encoding = Encoding.UTF8 }).DownloadStringTaskAsync(value);
			var res = _rgxOpMp4.MatchesMinGroups(videoString, 1);

			return res.Select(m => new VideoInfo()
			{
				VideoLink = m.Groups[1].Value,
				VideoName = videoName,
			});
		}
	}

	public class VideoInfo
	{
		public bool IsDownloading { get; set; }
		public int Episode { get; set; }
		public string VideoName { get; set; }
		public string VideoLink { get; set; }
		public int Percentage { get; set; }
		public Object StateObject { get; set; }
	}
}