using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private static readonly Regex _fileNameRex =
            new Regex(@"(\d+).*?\| (.*)", RegexOptions.ECMAScript | RegexOptions.Compiled);

        private readonly List<VideoInfo> _resList = new List<VideoInfo>();

        public int CurMaxEpisode = -1;

        public Form1()
        {
            InitializeComponent();

            VideoExtractor.GetCurEpisode().ContinueWith(m => this.InvokeEx(x =>
            {
                x.CurMaxEpisode = m.Result;
                x.Text = $"Video extractor, {m.Result} videos avaiable.";
            }));
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists("Results.txt"))
            {
                var lines = File.ReadAllLines("Results.txt");
                var rsLst = lines.Select(JsonConvert.DeserializeObject<VideoInfo[]>).ToList();
                _resList.AddRange(rsLst.SelectMany(m => m).Distinct().Select(m =>
                {
                    m.IsDownloading = false;
                    return m;
                }).OrderBy(m => m.VideoName));
            }

            AddRange(_resList);
        }

        private async void butResolve_Click(object sender, EventArgs e)
        {
            var start = 1;
            var max = 874;

            for (var i = start; i < max; i++)
            {
                await ResolveID(i);
            }
        }

        private async Task ResolveID(int episode)
        {
            if (_resList.Any(m => m.Episode == episode)) return;
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

        private async void butDownload_ClickAsync(object sender, EventArgs e)
        {
            if (!_resList.Any())
                await ResolveID(1);

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

                    await ResolveID(nextEpisode);
                    firstOne = _resList.OrderBy(m => m.Episode).FirstOrDefault(m => m.Percentage == 0);
                    if (firstOne == null)
                    {
                        MessageBox.Show("Error!");
                        return;
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
                    var fSelRow = dgvInfo.Rows.Cast<DataGridViewRow>()
                        .FirstOrDefault(x => Convert.ToInt32(x.Cells[0].Value) == firstOne.Episode);
                    fSelRow.Cells[3].Value = $"{firstOne.Percentage} %";
                    butDownload.PerformClick();
                    return;
                }

                var wv = new CustWebclient { Proxy = null, Encoding = Encoding.UTF8, StateObject = firstOne };

                wv.DownloadFileCompleted += (a, g) =>
                {
                    var wbc = a as CustWebclient;
                    var statObj = wbc.StateObject as VideoInfo;
                    if (statObj == null)
                        return;

                    this.InvokeEx(m =>
                    {
                        statObj.Percentage = 100;
                        var SelRow = m.dgvInfo.Rows.Cast<DataGridViewRow>()
                            .FirstOrDefault(x => Convert.ToInt32(x.Cells[0].Value) == statObj.Episode);
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
                            var SelRow = m.dgvInfo.Rows.Cast<DataGridViewRow>()
                                .FirstOrDefault(x => Convert.ToInt32(x.Cells[0].Value) == statObj.Episode);
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
            var sel = input.Select(m => new[]
                {m.Episode.ToString(), m.VideoName, m.VideoLink, m.Percentage.ToString()});
            dgvInfo.AddRows<DataGridViewTextBoxCell>(sel);
        }

        private void AddRow(VideoInfo input)
        {
            dgvInfo.AddRow<DataGridViewTextBoxCell>(new[]
                {input.Episode.ToString(), input.VideoName, input.VideoLink, input.Percentage.ToString()});
        }
    }
}