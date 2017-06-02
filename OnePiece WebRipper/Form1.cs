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
using RipLib;

namespace OnePiece_WebRipper
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();

			VideoExtractor.GetCurEpisode().ContinueWith(m => this.InvokeEx(x =>
			{
				x.CurMaxEpisode = m.Result;
				x.Text = $"Video extractor, {m.Result} videos avaiable.";
			}));
		}

		public int CurMaxEpisode = -1;
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
				_resList.AddRange(rsLst.SelectMany(m => m).Distinct().Select(m => { m.IsDownloading = false; return m; }).OrderBy(m => m.VideoName));
			}
			AddRange(_resList);
		}

		private async void butResolve_Click(object sender, EventArgs e)
		{
			var start = 651;
			var max = 653;

			for (int i = start; i < max; i++)
			{
				await ResolveID(i);
			}
		}

		private async Task ResolveID(int episode)
		{
			if (_resList.Any(m => m.Episode == episode))
			{
				return;
			}
			var erg = (await VideoExtractor.DoExtractAsync($"http://onepiece-tube.com/folge/{episode}")).ToArray();
			foreach (var item in erg)
			{
				item.Episode = episode;
				AddRow(item);
			}
			_resList.AddRange(erg);
			File.AppendAllText("Results.txt", JsonConvert.SerializeObject(erg) + Environment.NewLine);
			dgvInfo.AutoResizeColumns();
		}

		private static Regex _fileNameRex = new Regex(@"(\d+).*?\| (.*)", RegexOptions.ECMAScript | RegexOptions.Compiled);

		private async void butDownload_ClickAsync(object sender, EventArgs e)
		{
			if (!_resList.Any())
				await ResolveID(733);

			var firstOne = _resList.OrderBy(m => m.Episode).FirstOrDefault(m => m.Percentage == 0 && !m.IsDownloading);
			if (firstOne == null)
			{
				var nextEpisode = _resList.Max(m => m.Episode) + 1;
				if (CurMaxEpisode != -1)
				{
					if (nextEpisode > CurMaxEpisode)
					{
						MessageBox.Show("No Download Left!");
						return;
					}
					else
					{
						await ResolveID(nextEpisode);
						firstOne = _resList.OrderBy(m => m.Episode).FirstOrDefault(m => m.Percentage == 0);
						if (firstOne == null)
						{
							MessageBox.Show("Error!");
							return;
						}
					}
				}
				else
				{
					MessageBox.Show("No Download Left!");
					return;
				}
			}
			firstOne.IsDownloading = true;
			if (_fileNameRex.IsMatch(firstOne.VideoName))
			{
				var VidNameMatch = _fileNameRex.Match(firstOne.VideoName);
				if (VidNameMatch.Groups.Count < 2)
				{
					MessageBox.Show("Video title doesn't match");
					return;
				}
				var fileName = $"{VidNameMatch.Groups[1].Value} - {VidNameMatch.Groups[2].Value}.mp4";
				if (File.Exists(fileName))
				{
					firstOne.Percentage = 100;
					var fSelRow = dgvInfo.Rows.Cast<DataGridViewRow>().FirstOrDefault(x => Convert.ToInt32(x.Cells[0].Value) == firstOne.Episode);
					fSelRow.Cells[3].Value = $"{firstOne.Percentage} %";
					butDownload.PerformClick();
					return;
				}
				var wv = new CustWebclient() { Proxy = null, Encoding = Encoding.UTF8, StateObject = firstOne };

				wv.DownloadFileCompleted += (a, g) =>
				{
					var wbc = a as CustWebclient;
					var statObj = wbc.StateObject as VideoInfo;
					if (statObj == null)
						return;
					//new Action(() => { MessageBox.Show($"File \"{statObj.VideoName}\" Download Completed"); }).BeginInvoke(null, this);
					this.InvokeEx(m =>
					{
						statObj.Percentage = 100;
						var SelRow = m.dgvInfo.Rows.Cast<DataGridViewRow>().FirstOrDefault(x => Convert.ToInt32(x.Cells[0].Value) == statObj.Episode);
						if (SelRow != null)
							SelRow.Cells[3].Value = $"{statObj.Percentage} %";

						butDownload.PerformClick();
					});
				};

				wv.DownloadProgressChanged += (a, g) =>
				{
					var wbc = a as CustWebclient;
					var statObj = wbc.StateObject as VideoInfo;
					if (statObj != null)
					{
						statObj.Percentage = g.ProgressPercentage;
						this.InvokeEx(m =>
						{
							var SelRow = m.dgvInfo.Rows.Cast<DataGridViewRow>().FirstOrDefault(x => Convert.ToInt32(x.Cells[0].Value) == statObj.Episode);
							if (SelRow != null)
								SelRow.Cells[3].Value = $"{statObj.Percentage} %";
						});
					}
				};

				wv.DownloadFileAsync(new Uri(firstOne.VideoLink), fileName);
				//Debugger.Break();
			}
			else
			{
				MessageBox.Show("Video title doesn't match, no suiting file found");
			}
		}

		private void AddRange(IEnumerable<VideoInfo> input)
		{
			var sel = input.Select(m => new[] { m.Episode.ToString(), m.VideoName, m.VideoLink, m.Percentage.ToString() });
			dgvInfo.AddRows<DataGridViewTextBoxCell>(sel);
		}

		private void AddRow(VideoInfo input)
		{
			dgvInfo.AddRow<DataGridViewTextBoxCell>(new[] { input.Episode.ToString(), input.VideoName, input.VideoLink, input.Percentage.ToString() });
		}
	}
}