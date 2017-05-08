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

namespace OnePiece_WebRipper
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		private List<VideoInfo> _resList = new List<VideoInfo>();

		private void DoStart(Action<VideoInfo> OnSucc, int start, int end, int steps)
		{
			var le = new Action<Action<VideoInfo>, int, int, int>(DoStart);

			var max = end;
			for (int i = start; i < max; i++)
			{
				var j = i;
				for (j = i; j < i + steps; j++)
				{
					new VideoExtractor(j).DoExtractAsync(OnSucc);
				}
				i = j;
				if (i < max)
				{
					Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith(x =>
					{
						Debugger.Break();
						DoStart(OnSucc, i, max, steps);
					});

					return;
				}
			}
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

		private void Form1_Load(object sender, EventArgs e)
		{
		}

		private async void butResolve_Click(object sender, EventArgs e)
		{
			Action<VideoInfo> OnSucc = m =>
			{
				if (m == null)
				{
					Debugger.Break();
				}
				this.InvokeEx(() =>
				{
					_resList.Add(m);
					File.AppendAllText("Results.txt", Newtonsoft.Json.JsonConvert.SerializeObject(m) + Environment.NewLine);
					dgvInfo.AddRow<DataGridViewTextBoxCell>(new[] { m.StateObject.ToString(), m.VideoName, m.VideoLink, "0" });
					dgvInfo.AutoResizeColumns();
				});
			};

			var start = 651;
			var max = 700;
			var lauf = start;
			while (lauf < max)
			{
				for (int i = 0; i < 10; i++)
				{
					new VideoExtractor(lauf).DoExtractAsync(OnSucc);
					lauf++;
				}

				await Task.Delay(TimeSpan.FromSeconds(2));
			}
		}

		private async void butDownload_Click(object sender, EventArgs e)
		{
		}
	}

	public class VideoExtractor
	{
		private Regex _rgxOpTubeAniStream = new Regex(@"<iframe src=""(.*?)"".*?<\/ifram", RegexOptions.Compiled | RegexOptions.ECMAScript);
		private Regex _rgxOpMp4 = new Regex(@"file: ['""](.*?.mp4)[""']", RegexOptions.Compiled | RegexOptions.ECMAScript);
		private Regex _rgxTitle = new Regex(@"<title>(.*?)<\/title>", RegexOptions.Compiled | RegexOptions.ECMAScript);

		private string _opTubeLink;
		private object _stateObj;

		public VideoExtractor(string opTubeLink, object stateObject = null)
		{
			_opTubeLink = opTubeLink;
			_stateObj = stateObject;
		}

		public VideoExtractor(int episode, object stateObject = null)
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
	}

	public static class Extensions
	{
		public static void AddRow<T>(this DataGridView dgv, string[] input) where T : DataGridViewCell, new()
		{
			var row = new DataGridViewRow();
			var cells = input.Select(m => new T { Value = m }).Cast<DataGridViewCell>().ToArray();
			row.Cells.AddRange(cells);
			dgv.Rows.Add(row);
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
		public string VideoName { get; set; }
		public string VideoLink { get; set; }
		public Object StateObject { get; set; }
	}
}