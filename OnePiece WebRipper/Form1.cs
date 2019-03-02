using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using RipLib;

namespace OnePiece_WebRipper
{
    /// <summary>
    /// VEERRY UGLY CODE;
    /// DONT DO THAT AT HOME BOYYYS AND GUURRRLS
    ///
    /// (srsly; i cleaned up a bit...next time its gonna be a wpf tool using mvvm...promised)
    /// </summary>
    public partial class Form1 : Form
    {
        private readonly List<VideoInfo> _videoInfoList = new List<VideoInfo>();

        public int CurMaxEpisode = -1;
        private HttpClient _httpClient;

        public Form1()
        {
            InitializeComponent();
            _httpClient = new HttpClient();

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
                _videoInfoList.AddRange(rsLst.SelectMany(m => m).Distinct().Select(m =>
                {
                    m.IsDownloading = false;
                    return m;
                }).OrderBy(m => m.VideoName));
            }

            AddRange(_videoInfoList);
        }

        private async void butResolve_Click(object sender, EventArgs e)
        {
            var start = 1;
            var max = 874;

            for (var i = start; i < max; i++)
            {
                await ResolveId(i);
            }
        }

        private async Task ResolveId(int episode)
        {
            if (_videoInfoList.Any(m => m.Episode == episode)) return;
            var erg = (await VideoExtractor.DoExtractAsync($"http://onepiece-tube.com/folge/{episode}")).ToArray();
            foreach (var item in erg)
            {
                item.Episode = episode;
                AddRow(item);
            }

            _videoInfoList.AddRange(erg);
            File.AppendAllText("Results.txt", JsonConvert.SerializeObject(erg) + Environment.NewLine);
            dgvInfo.AutoResizeColumns();
        }

        private async void butDownload_ClickAsync(object sender, EventArgs e)
        {
            butDownload.Enabled = false;
            try
            {
                var taskList = new List<Task>();

                var groupByHost = _videoInfoList.GroupBy(x => new Uri(x.VideoLink).Host).ToList();
                while (_videoInfoList.Any(x => !x.IsDownloading))
                {
                    foreach (var videoInfoList in groupByHost)
                    {
                        var videoInfo = videoInfoList.FirstOrDefault(x => !x.IsDownloading);
                        if (videoInfo == null)
                        {
                            continue;
                        }

                        videoInfo.IsDownloading = true;
                        var fSelRow = dgvInfo.Rows.Cast<DataGridViewRow>().FirstOrDefault(x => Convert.ToInt32(x.Cells[0].Value) == videoInfo.Episode)?.Cells[3];
                        taskList.Add(DownloadFileWithStatusUpdateAsync(videoInfo, fSelRow));
                    }

                    this.Text = $"Downloading {taskList.Count} episodes";
                    while (taskList.Count > 20)
                    {
                        var resultTask = await Task.WhenAny(taskList);
                        await resultTask;
                        taskList.Remove(resultTask);
                    }
                }
                while (taskList.Count != 0)
                {
                    var resultTask = await Task.WhenAny(taskList);
                    await resultTask;
                    taskList.Remove(resultTask);
                }

                MessageBox.Show("Finished downloading!");
            }
            finally
            {
                butDownload.Enabled = true;
            }
        }

        private async Task DownloadFileWithStatusUpdateAsync(VideoInfo videoInfo, DataGridViewCell statusCell, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var lastValue = "";

                using (var response = await _httpClient.GetAsync(videoInfo.VideoLink, HttpCompletionOption.ResponseHeadersRead))
                using (var inputStream = await response.Content.ReadAsStreamAsync())
                {
                    var contentLength = response.Content.Headers.ContentLength;

                    if (File.Exists(videoInfo.FileName))
                    {
                        if (new FileInfo(videoInfo.FileName).Length >= contentLength && statusCell != null)
                        {
                            videoInfo.Percentage = 100;
                            statusCell.Value = $"{videoInfo.Percentage} %";
                            return;
                        }

                        File.Delete(videoInfo.FileName);
                    }

                    using (var outputStream = File.OpenWrite(videoInfo.FileName))
                    {
                        int bytesRead;
                        long totalBytesRead = 0;

                        byte[] buffer = new byte[8 * 1024 * 1024];
                        while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
                        {
                            outputStream.Write(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;
                            if (contentLength.HasValue && statusCell != null)
                            {
                                var percent = ((decimal)(totalBytesRead * 100)) / (decimal)contentLength;
                                var nextValue = $"{percent:F1}";
                                if (lastValue != nextValue)
                                {
                                    statusCell.Value = nextValue;
                                    lastValue = nextValue;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                videoInfo.IsDownloading = false;
                if (File.Exists(videoInfo.FileName))
                {
                    File.Delete(videoInfo.FileName);
                }

                if (statusCell != null)
                {
                    statusCell.Value = $"{videoInfo.Percentage} %";
                }
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