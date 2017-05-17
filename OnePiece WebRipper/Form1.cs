using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
namespace OnePiece_WebRipper
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		private List<VideoInfo> _resList = new List<VideoInfo>();

		private void Form1_Load(object sender, EventArgs e)
		{
			if (File.Exists("Results.txt"))
			{
				var lines = File.ReadAllLines("Results.txt");
				var rsLst = new List<VideoInfo[]>();
				foreach (var item in lines)
				{
					rsLst.Add(JsonConvert.DeserializeObject<VideoInfo[]>(item));
				}
				_resList.AddRange(rsLst.SelectMany(m => m).Distinct().OrderBy(m => m.VideoName));
			}
			AddRange(_resList);
		}

		private async void butResolve_Click(object sender, EventArgs e)
		{
			var start = 651;
			var max = 653;

			for (int i = start; i < max; i++)
			{
				if (_resList.Any(m => m.Folge == i))
				{
					continue;
				}
				var erg = (await VideoExtractor.DoExtractAsync2($"http://onepiece-tube.com/folge/{i}")).ToArray();
				foreach (var item in erg)
				{
					item.Folge = i;
					AddRow(item);
				}
				_resList.AddRange(erg);
				File.AppendAllText("Results.txt", Newtonsoft.Json.JsonConvert.SerializeObject(erg) + Environment.NewLine);
				dgvInfo.AutoResizeColumns();
			}
		}

		private static Regex _fileNameRex = new Regex(@"(\d+).*?\| (.*)", RegexOptions.ECMAScript | RegexOptions.Compiled);
		private void butDownload_Click(object sender, EventArgs e)
		{
			var firstOne = _resList.OrderBy(m => m.Folge).FirstOrDefault(m => m.Percentage == 0);
			if (firstOne == null)
			{
				MessageBox.Show("No Download Left!");
				return;
			}

			if (_fileNameRex.IsMatch(firstOne.VideoName))
			{
				var VidNameMatch = _fileNameRex.Match(firstOne.VideoName);
				if (VidNameMatch.Groups.Count < 2)
				{
					MessageBox.Show("Video title doesn't match");
					return;
				}
				var fileName = $"{VidNameMatch.Groups[1].Value} - {VidNameMatch.Groups[2].Value}.mp4";
				var wv = new CustWebclient() { Proxy = null, Encoding = Encoding.UTF8, StateObject = firstOne };

				wv.DownloadFileCompleted += (a, g) =>
				{
					var wbc = a as CustWebclient;
					var statObj = wbc.StateObject as VideoInfo;
					if (statObj == null)
						return;

					this.InvokeEx(m =>
					{
						
						var SelRow = m.dgvInfo.Rows.Cast<DataGridViewRow>().FirstOrDefault(x => Convert.ToInt32(x.Cells[0].Value) == statObj.Folge);
						if (SelRow != null)
						{
							SelRow.Cells[3].Value = "100 %";
						}
						MessageBox.Show("File Download Completed");
					});
				};
				wv.DownloadProgressChanged += (a, g) =>
				{
					var wbc = a as CustWebclient;
					var statObj = wbc.StateObject as VideoInfo;
					if (statObj != null)
					{
						this.InvokeEx(m =>
						{
							var SelRow = m.dgvInfo.Rows.Cast<DataGridViewRow>().FirstOrDefault(x => Convert.ToInt32(x.Cells[0].Value) == statObj.Folge);
							if (SelRow != null)
							{
								SelRow.Cells[3].Value = $"{g.ProgressPercentage} %";
							}
						});
					}
				};

				wv.DownloadFileAsync(new Uri(firstOne.VideoLink), fileName);
				Debugger.Break();
			}
			MessageBox.Show("Video title doesn't match");
		}

		private void AddRange(IEnumerable<VideoInfo> input)
		{
			var sel = input.Select(m => new[] { m.Folge.ToString(), m.VideoName, m.VideoLink, m.Percentage.ToString() });
			dgvInfo.AddRows<DataGridViewTextBoxCell>(sel);
		}

		private void AddRow(VideoInfo input)
		{
			dgvInfo.AddRow<DataGridViewTextBoxCell>(new[] { input.Folge.ToString(), input.VideoName, input.VideoLink, input.Percentage.ToString() });
		}
	}

	public class VideoExtractor
	{
		private static Regex _rgxOpTubeAniStream = new Regex(@"<iframe src=""(.*?)"".*?<\/ifram", RegexOptions.Compiled | RegexOptions.ECMAScript);
		private static Regex _rgxOpMp4 = new Regex(@"file: ['""](.*?.mp4)[""']", RegexOptions.Compiled | RegexOptions.ECMAScript);
		private static Regex _rgxTitle = new Regex(@"<title>(.*?)<\/title>", RegexOptions.Compiled | RegexOptions.ECMAScript);

		public static async Task<IEnumerable<VideoInfo>> DoExtractAsync2(string opTubeLink)
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

	public static class Extensions
	{
		public static void AddRow<T>(this DataGridView dgv, string[] input) where T : DataGridViewCell, new()
		{
			dgv.Rows.Add(GetRow<T>(input));
		}

		public static void AddRows<T>(this DataGridView dgv, IEnumerable<string[]> input) where T : DataGridViewCell, new()
		{
			var resRows = input.Select(m => GetRow<T>(m)).ToArray();
			dgv.Rows.AddRange(resRows);
		}
		public static DataGridViewRow GetRow<T>(string[] input) where T : DataGridViewCell, new()
		{
			var row = new DataGridViewRow();
			var cells = input.Select(m => new T { Value = m }).Cast<DataGridViewCell>().ToArray();
			row.Cells.AddRange(cells);
			return row;
		}

		public static void DownloadStringAsync(this WebClient srcWeb, string uri) => srcWeb.DownloadStringAsync(new Uri(uri));

		public static IEnumerable<Match> MatchesMinGroups(this Regex input, string source, int minCount)
			=> input.Matches(source).MinGroups(minCount);

		/// <summary>
		/// Returns all Matches which Groups atleast contains <see cref="minCount"/> Values
		/// </summary>
		/// <param name="input"></param>
		/// <param name="minCount"></param>
		/// <returns></returns>
		public static IEnumerable<Match> MinGroups(this MatchCollection input, int minCount)
			=> input.Cast<Match>().Where(m => m.Groups.Count >= minCount);
	}

	public static class InvokeExtentions
	{
		public static TResult InvokeEx<TControl, TResult>(this TControl control, Func<TControl, TResult> func) where TControl : Control
			=> control.InvokeRequired ? (TResult)control.Invoke(func, control) : func(control);

		public static void InvokeEx<TControl>(this TControl control, Action<TControl> func) where TControl : Control
			=> control.InvokeEx(c => { func(c); return c; });

		public static void InvokeEx<TControl>(this TControl control, Action action) where TControl : Control
			=> control.InvokeEx(c => action());
	}

	public class VideoInfo
	{
		public int Folge { get; set; }
		public string VideoName { get; set; }
		public string VideoLink { get; set; }
		public int Percentage { get; set; }
		public Object StateObject { get; set; }
	}

	public class CustWebclient : WebClient
	{
		public object StateObject { get; set; }
	}

	public class VideoExtractorOld
	{
		private static Regex _rgxOpTubeAniStream = new Regex(@"<iframe src=""(.*?)"".*?<\/ifram", RegexOptions.Compiled | RegexOptions.ECMAScript);
		private static Regex _rgxOpMp4 = new Regex(@"file: ['""](.*?.mp4)[""']", RegexOptions.Compiled | RegexOptions.ECMAScript);
		private static Regex _rgxTitle = new Regex(@"<title>(.*?)<\/title>", RegexOptions.Compiled | RegexOptions.ECMAScript);

		private string _opTubeLink;
		private object _stateObj;

		public VideoExtractorOld(string opTubeLink, object stateObject = null)
		{
			_opTubeLink = opTubeLink;
			_stateObj = stateObject;
		}

		public VideoExtractorOld(int episode, object stateObject = null)
		{
			_opTubeLink = $"http://onepiece-tube.com/folge/{episode}";
			_stateObj = stateObject ?? episode;
		}

		public VideoInfo[] DoExtract()
		{
			string source = new WebClient { Proxy = null, Encoding = Encoding.UTF8 }.DownloadString(_opTubeLink);
			var value = _rgxOpTubeAniStream.MatchesMinGroups(source, 1).Select(m => m.Groups[1].Value);
			var videoName = _rgxTitle.MatchesMinGroups(source, 1).FirstOrDefault()?.Groups[1].Value;

			var mTasks = value.Select(val => Task.Run(() => new WebClient() { Proxy = null, Encoding = Encoding.UTF8 }.DownloadString(val)).ContinueWith(task => (task.Status == TaskStatus.RanToCompletion) ? _rgxOpMp4.MatchesMinGroups(task.Result, 1).FirstOrDefault()?.Groups[1].Value ?? string.Empty : string.Empty)).ToArray();

			Task.WaitAll(mTasks);

			return mTasks.Where(m => m.IsFaulted == false && m.Result != string.Empty).Select(m => new VideoInfo()
			{
				VideoLink = m.Result,
				VideoName = videoName,
				StateObject = _stateObj
			}).ToArray();
		}



		public void DoExtractAsync(Action<VideoInfo> asyncCbx)
		{
			Task.Run(() => new WebClient() { Proxy = null, Encoding = Encoding.UTF8 }.DownloadString(_opTubeLink)).ContinueWith(
					m =>
					{
						if (m.IsFaulted) asyncCbx(null);
						var source = m.Result;
						var prVal = _rgxOpTubeAniStream.MatchesMinGroups(source, 1);
						var value = prVal.Select(a => a.Groups[1].Value).LastOrDefault(x => x.Contains("ani-stream.com"));
						var videoName = _rgxTitle.MatchesMinGroups(source, 1).FirstOrDefault()?.Groups[1].Value;
						var erg = Task.Run(() => new WebClient() { Proxy = null, Encoding = Encoding.UTF8 }.DownloadString(value) + _opTubeLink + "\n" + value);
						erg.ContinueWith(task =>
						{
							if (task.Status == TaskStatus.RanToCompletion)
							{
								var res = _rgxOpMp4.MatchesMinGroups(task.Result, 1).FirstOrDefault();
								if (res != null)
								{
									return res.Groups[1].Value;
								}
								return string.Empty;
							}
							return string.Empty;
						}).ContinueWith(t =>
						{
							if (!t.IsFaulted && t.Result != string.Empty && asyncCbx != null)
							{
								asyncCbx(new VideoInfo()
								{
									VideoLink = t.Result,
									VideoName = videoName,
									StateObject = _stateObj
								});
							}
							else
							{
								Debugger.Break();
							}
						});
					});
		}

		//	return;
		//		Action<VideoInfo> OnSucc = m =>
		//		{
		//			if (m == null)
		//			{
		//				Debugger.Break();
		//			}
		//			this.InvokeEx(() =>
		//			{
		//				_resList.Add(m);
		//				File.AppendAllText("Results.txt", Newtonsoft.Json.JsonConvert.SerializeObject(m) + Environment.NewLine);
		//				dgvInfo.AddRow<DataGridViewTextBoxCell>(new[] { m.StateObject.ToString(), m.VideoName, m.VideoLink, "0" });
		//				dgvInfo.AutoResizeColumns();
		//			});
		//		};

		//	start = 651;
		//		 max = 700;
		//		var lauf = start;
		//		while (lauf<max)
		//		{
		//			for (int i = 0; i< 10; i++)
		//			{
		//				new VideoExtractor(lauf).DoExtractAsync(OnSucc);
		//	lauf++;
		//			}

		//await Task.Delay(TimeSpan.FromSeconds(2));
		//		}
		private void DoStart(Action<VideoInfo> OnSucc, int start, int end, int steps)
		{
			//var le = new Action<Action<VideoInfo>, int, int, int>(DoStart);

			//var max = end;
			//for (int i = start; i < max; i++)
			//{
			//	var j = i;
			//	for (j = i; j < i + steps; j++)
			//	{
			//		new VideoExtractor(j).DoExtractAsync(OnSucc);
			//	}
			//	i = j;
			//	if (i < max)
			//	{
			//		Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith(x =>
			//		{
			//			Debugger.Break();
			//			DoStart(OnSucc, i, max, steps);
			//		});

			//		return;
			//	}
			//}

		}
		private string extract_videosrc(string uri)
		{
			WebClient wc = new WebClient { Proxy = null };
			string tmp = wc.DownloadString(uri);
			string value = (Regex.Match(tmp, @"ani-stream.com/(.*?).html").Groups[1].Value);
			string dsttmp = wc.DownloadString("http://www.ani-stream.com/" + value + ".html");

			string value2 = (Regex.Match(dsttmp, @"file: ['""](.*?.mp4)[""']").Groups[1].Value);
			return value2;
		}

	}
}